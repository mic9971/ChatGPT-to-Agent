using System.Text;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Managed text search backend adhering to UC-WS-05, BR-SEC-003, and BR-SEC-004.
/// Traverses only allowed workspace paths, detects binary content, and produces bounded snippets.
/// </summary>
public sealed class ManagedSearchBackend : ISearchBackend
{
    private const int BinaryProbeBufferSize = 8192;
    private readonly IWorkspaceAccessPolicy _accessPolicy;

    public ManagedSearchBackend(IWorkspaceAccessPolicy accessPolicy)
    {
        _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
    }

    public async Task<IReadOnlyList<WorkspaceSearchMatch>> SearchAsync(
        IWorkspaceContext context,
        ResolvedWorkspacePath scopePath,
        string query,
        WorkspaceSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (scopePath == null)
        {
            throw new ArgumentNullException(nameof(scopePath));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Search query cannot be empty.", nameof(query));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        List<(string RelativePath, string FullPath)> candidateFiles = [];

        if (scopePath.IsDirectory)
        {
            // Traverse directory tree securely using access policy at each step
            Queue<string> dirQueue = new();
            dirQueue.Enqueue(scopePath.CanonicalFullPath);

            while (dirQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string currentDir = dirQueue.Dequeue();

                DirectoryInfo dirInfo = new(currentDir);
                IEnumerable<FileSystemInfo> entries;
                try
                {
                    entries = dirInfo.EnumerateFileSystemInfos();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (FileSystemInfo entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string childRelative = Path.GetRelativePath(context.CanonicalRoot, entry.FullName).Replace('\\', '/');

                        // Pre-disclosure filtering: every path must be authorized by shared access policy
                        var evalResult = _accessPolicy.EvaluatePath(context, childRelative);
                        if (evalResult.IsFailure || !evalResult.Value!.Exists)
                        {
                            // Denied, sensitive, ignored, or escaping symlink -> do not traverse or inspect
                            continue;
                        }

                        if (evalResult.Value.IsDirectory)
                        {
                            dirQueue.Enqueue(evalResult.Value.CanonicalFullPath);
                        }
                        else
                        {
                            candidateFiles.Add((childRelative, evalResult.Value.CanonicalFullPath));
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        continue;
                    }
                }
            }
        }
        else
        {
            candidateFiles.Add((scopePath.RelativePath, scopePath.CanonicalFullPath));
        }

        List<WorkspaceSearchMatch> matches = [];

        foreach (var (relPath, fullPath) in candidateFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                FileInfo fileInfo = new(fullPath);
                if (!fileInfo.Exists || fileInfo.Length > options.MaxFileSizeBytes)
                {
                    continue;
                }

                if (await IsBinaryFileAsync(fullPath, cancellationToken))
                {
                    continue;
                }

                using var stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4096,
                    useAsync: true);

                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                int lineNumber = 0;
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    lineNumber++;
                    int index = line.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        string trimmed = line.Trim();
                        string snippet = trimmed.Length <= options.MaxSnippetLength
                            ? trimmed
                            : trimmed[..options.MaxSnippetLength];

                        matches.Add(new WorkspaceSearchMatch
                        {
                            RelativePath = relPath,
                            LineNumber = lineNumber,
                            Snippet = snippet
                        });
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Safe handling of TOCTOU concurrency during search
                continue;
            }
        }

        // Stable deterministic sorting by relative path and line number
        matches.Sort((a, b) =>
        {
            int pathCmp = string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase);
            if (pathCmp != 0)
            {
                return pathCmp;
            }

            pathCmp = string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal);
            if (pathCmp != 0)
            {
                return pathCmp;
            }

            return a.LineNumber.CompareTo(b.LineNumber);
        });

        return matches;
    }

    private static async Task<bool> IsBinaryFileAsync(string filePath, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[BinaryProbeBufferSize];
        int bytesRead;

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: BinaryProbeBufferSize,
                useAsync: true);

            bytesRead = await stream.ReadAsync(buffer.AsMemory(0, BinaryProbeBufferSize), cancellationToken);
        }
        catch
        {
            return true; // Treat unreadable files as binary/unsafe to read
        }

        for (int i = 0; i < bytesRead; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }
}
