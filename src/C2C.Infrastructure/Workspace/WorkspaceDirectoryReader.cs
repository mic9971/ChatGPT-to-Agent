using System.Text;
using System.Text.Json;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements safe workspace directory enumeration adhering to UC-WS-03, BR-APP-003, and BR-APP-005.
/// </summary>
public sealed class WorkspaceDirectoryReader : IWorkspaceDirectoryReader
{
    private readonly IWorkspaceAccessPolicy _accessPolicy;
    private readonly WorkspaceDirectoryReaderOptions _options;

    public WorkspaceDirectoryReader(
        IWorkspaceAccessPolicy accessPolicy,
        WorkspaceDirectoryReaderOptions? options = null)
    {
        _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        _options = options ?? new WorkspaceDirectoryReaderOptions();
    }

    public Task<OperationResult<PageResult<WorkspaceDirectoryEntry>>> ListAsync(
        IWorkspaceContext context,
        string? relativePath,
        PageRequest? pageRequest = null,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Workspace context cannot be null.",
                nameof(context)));
        }

        string effectivePath = string.IsNullOrWhiteSpace(relativePath) || relativePath.Trim() == "/"
            ? "."
            : relativePath;

        // 1. Resolve and authorize requested directory through single access policy gate
        OperationResult<ResolvedWorkspacePath> evalResult = _accessPolicy.EvaluatePath(context, effectivePath);
        if (evalResult.IsFailure)
        {
            return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(evalResult.Error!));
        }

        ResolvedWorkspacePath resolved = evalResult.Value!;
        if (!resolved.Exists)
        {
            return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(
                CommonErrorCodes.WorkspaceItemNotFound,
                $"Directory not found: '{relativePath}'.",
                relativePath));
        }

        if (!resolved.IsDirectory)
        {
            return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Path '{relativePath}' is a regular file, not a directory.",
                relativePath));
        }

        // 2. Decode cursor if provided
        int offset = 0;
        if (!string.IsNullOrWhiteSpace(pageRequest?.Cursor))
        {
            OperationResult<int> decodeResult = DecodeCursor(pageRequest.Cursor);
            if (decodeResult.IsFailure)
            {
                return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(decodeResult.Error!));
            }

            offset = decodeResult.Value;
        }

        // 3. Apply limit constraints
        int limit = pageRequest?.Limit ?? _options.DefaultLimit;
        if (limit <= 0)
        {
            limit = _options.DefaultLimit;
        }
        else if (limit > _options.MaxLimit)
        {
            limit = _options.MaxLimit;
        }

        // 4. Enumerate direct children only
        DirectoryInfo dirInfo = new(resolved.CanonicalFullPath);
        IEnumerable<FileSystemInfo> entries;
        try
        {
            entries = dirInfo.EnumerateFileSystemInfos();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Failed to enumerate directory entries: {ex.Message}",
                relativePath));
        }

        List<WorkspaceDirectoryEntry> visibleEntries = [];
        foreach (FileSystemInfo entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string childRelative = Path.GetRelativePath(context.CanonicalRoot, entry.FullName).Replace('\\', '/');

                // 5. Pre-disclosure filtering: verify child passes access policy before inclusion
                OperationResult<ResolvedWorkspacePath> childEval = _accessPolicy.EvaluatePath(context, childRelative);
                if (childEval.IsFailure || !childEval.Value!.Exists)
                {
                    // Silently filter out denied/sensitive/ignored/escaping entries
                    continue;
                }

                bool isChildDir = childEval.Value.IsDirectory;
                long? sizeBytes = null;
                if (!isChildDir && entry is FileInfo fileInfo)
                {
                    sizeBytes = fileInfo.Length;
                }

                visibleEntries.Add(new WorkspaceDirectoryEntry
                {
                    Name = entry.Name,
                    RelativePath = childRelative,
                    IsDirectory = isChildDir,
                    SizeBytes = sizeBytes,
                    LastModifiedUtc = entry.LastWriteTimeUtc
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // TOCTOU safety: file deleted or inaccessible during enumeration
                continue;
            }
        }

        // 6. Stable deterministic sorting
        visibleEntries.Sort((a, b) =>
        {
            int cmp = string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal);
        });

        // 7. Cursor pagination
        List<WorkspaceDirectoryEntry> pagedItems;
        string? nextCursor = null;

        if (offset >= visibleEntries.Count)
        {
            pagedItems = [];
        }
        else
        {
            pagedItems = visibleEntries.Skip(offset).Take(limit).ToList();
            if (offset + limit < visibleEntries.Count)
            {
                nextCursor = EncodeCursor(offset + limit);
            }
        }

        return Task.FromResult(OperationResult<PageResult<WorkspaceDirectoryEntry>>.Success(
            new PageResult<WorkspaceDirectoryEntry>(pagedItems, nextCursor)));
    }

    private static string EncodeCursor(int offset)
    {
        string json = $"{{\"offset\":{offset}}}";
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"cur_{base64}";
    }

    private static OperationResult<int> DecodeCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor) || !cursor.StartsWith("cur_", StringComparison.Ordinal))
        {
            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Invalid cursor format: cursor must start with 'cur_'.",
                nameof(cursor));
        }

        try
        {
            string payload = cursor[4..]
                .Replace('-', '+')
                .Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("offset", out JsonElement offsetProp) &&
                offsetProp.TryGetInt32(out int offset) &&
                offset >= 0)
            {
                return OperationResult<int>.Success(offset);
            }

            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Malformed cursor payload: non-negative integer 'offset' expected.",
                nameof(cursor));
        }
        catch
        {
            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Malformed or corrupted cursor string.",
                nameof(cursor));
        }
    }
}
