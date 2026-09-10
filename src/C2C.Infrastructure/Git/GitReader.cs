using System.Text;

using C2C.Core.Common;
using C2C.Core.Git;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Git;

/// <summary>
/// Implements Git evidence use cases (UC-GIT-01, UC-GIT-02) applying workspace visibility policy
/// before returning any path or diff body.
/// </summary>
public sealed class GitReader : IGitReader
{
    private readonly IGitProcess _gitProcess;
    private readonly IWorkspaceAccessPolicy _accessPolicy;
    private readonly GitOptions _options;

    public GitReader(
        IGitProcess gitProcess,
        IWorkspaceAccessPolicy accessPolicy,
        GitOptions? options = null)
    {
        _gitProcess = gitProcess ?? throw new ArgumentNullException(nameof(gitProcess));
        _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        _options = options ?? new GitOptions();
    }

    // ── UC-GIT-01 ──────────────────────────────────────────────────────────────

    public async Task<OperationResult<GitStatusResult>> GetStatusAsync(
        IWorkspaceContext context,
        GitStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // 1. Run git status --porcelain=v1 -z in workspace root
        var processResult = await _gitProcess.RunAsync(
            context.CanonicalRoot,
            ["status", "--porcelain=v1", "-z"],
            cancellationToken);

        if (processResult.IsFailure)
        {
            return OperationResult<GitStatusResult>.Failure(processResult.Error!);
        }

        // 2. Parse porcelain output
        var allEntries = GitStatusParser.Parse(processResult.Value!);

        // 3. Canonicalize + visibility-check each path; filter denied entries
        var visibleEntries = new List<GitStatusEntry>();
        int deniedCount = 0;
        bool truncated = false;

        foreach (var entry in allEntries)
        {
            if (visibleEntries.Count >= _options.MaxStatusEntries)
            {
                truncated = true;
                break;
            }

            // Evaluate the primary path
            var pathResult = _accessPolicy.EvaluatePath(context, entry.RelativePath);
            if (pathResult.IsFailure)
            {
                deniedCount++;
                continue;
            }

            // For renames, also check original path — if denied, deny the whole entry
            if (entry.OriginalPath is not null)
            {
                var origResult = _accessPolicy.EvaluatePath(context, entry.OriginalPath);
                if (origResult.IsFailure)
                {
                    deniedCount++;
                    continue;
                }
            }

            visibleEntries.Add(entry);
        }

        // 4. Sort for stable ordering (BR-APP-005)
        visibleEntries.Sort(static (a, b) =>
            string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));

        return OperationResult<GitStatusResult>.Success(new GitStatusResult
        {
            Entries = visibleEntries,
            Truncated = truncated,
            DeniedCount = deniedCount
        });
    }

    // ── UC-GIT-02 ──────────────────────────────────────────────────────────────

    public async Task<OperationResult<GitDiffResult>> GetDiffAsync(
        IWorkspaceContext context,
        GitDiffRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Stage 1: Get candidate paths from git status (reuse UC-GIT-01 logic)
        var statusResult = await GetStatusAsync(context, new GitStatusRequest(), cancellationToken);
        if (statusResult.IsFailure)
        {
            return OperationResult<GitDiffResult>.Failure(statusResult.Error!);
        }

        var allowedPaths = statusResult.Value!.Entries
            .Select(e => e.RelativePath)
            .ToList();

        if (allowedPaths.Count == 0)
        {
            return OperationResult<GitDiffResult>.Success(new GitDiffResult
            {
                Entries = [],
                NextCursor = null,
                DeniedCount = statusResult.Value.DeniedCount
            });
        }

        // Stage 2: Parse cursor-based pagination
        int pageSize = Math.Min(
            request.Limit ?? _options.DefaultDiffPageSize,
            _options.MaxDiffRecords);
        int pageOffset = ParseCursor(request.Cursor);

        var pagedPaths = allowedPaths
            .Skip(pageOffset)
            .Take(pageSize)
            .ToList();

        if (pagedPaths.Count == 0)
        {
            return OperationResult<GitDiffResult>.Success(new GitDiffResult
            {
                Entries = [],
                NextCursor = null,
                DeniedCount = statusResult.Value.DeniedCount
            });
        }

        // Stage 3: Build git diff command with EXPLICIT allowed pathspecs only
        // Never run a broad diff and filter later (UC-GIT-02 critical security rule)
        var diffArgs = BuildDiffArgs(request.Base, pagedPaths);
        var diffResult = await _gitProcess.RunAsync(context.CanonicalRoot, diffArgs, cancellationToken);

        if (diffResult.IsFailure)
        {
            return OperationResult<GitDiffResult>.Failure(diffResult.Error!);
        }

        // Stage 4: Parse per-file diff records and bound output
        var diffEntries = ParseDiffOutput(diffResult.Value!, pagedPaths);

        // Stage 5: Build next-page cursor
        int nextOffset = pageOffset + pagedPaths.Count;
        string? nextCursor = nextOffset < allowedPaths.Count
            ? BuildCursor(nextOffset)
            : null;

        return OperationResult<GitDiffResult>.Success(new GitDiffResult
        {
            Entries = diffEntries,
            NextCursor = nextCursor,
            DeniedCount = statusResult.Value.DeniedCount
        });
    }

    private static string[] BuildDiffArgs(string? baseRef, IReadOnlyList<string> allowedPaths)
    {
        var args = new List<string> { "diff", "--unified=3" };

        if (!string.IsNullOrWhiteSpace(baseRef))
        {
            args.Add(baseRef);
        }

        // Separator between options and pathspecs
        args.Add("--");

        foreach (string path in allowedPaths)
        {
            args.Add(path);
        }

        return args.ToArray();
    }

    private List<GitDiffEntry> ParseDiffOutput(string rawDiff, IReadOnlyList<string> pagedPaths)
    {
        // Split the unified diff into per-file sections by "diff --git" header
        var entries = new List<GitDiffEntry>();
        int totalBytes = 0;

        string[] sections = rawDiff.Split(
            "\ndiff --git ",
            StringSplitOptions.RemoveEmptyEntries);

        foreach (string section in sections)
        {
            if (entries.Count >= _options.MaxDiffRecords)
            {
                break;
            }

            // Extract the path from the "a/path b/path" header line
            string headerLine = section.Split('\n', 2)[0].TrimStart();
            string? relativePath = ExtractPathFromDiffHeader(headerLine, pagedPaths);

            if (relativePath is null)
            {
                continue;
            }

            // Bound diff body by bytes
            bool truncated = false;
            string diffBody;

            int remainingBytes = _options.MaxDiffBytes - totalBytes;
            string sectionContent = section.StartsWith("diff --git ", StringComparison.Ordinal)
                ? section
                : "diff --git " + section;

            if (Encoding.UTF8.GetByteCount(sectionContent) > remainingBytes)
            {
                // Truncate to remaining byte budget using UTF-8 safe truncation
                diffBody = TruncateToBytes(sectionContent, remainingBytes);
                truncated = true;
            }
            else
            {
                diffBody = sectionContent;
            }

            totalBytes += Encoding.UTF8.GetByteCount(diffBody);

            int linesAdded = 0;
            int linesRemoved = 0;
            foreach (string line in diffBody.Split('\n'))
            {
                if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal))
                {
                    linesAdded++;
                }
                else if (line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal))
                {
                    linesRemoved++;
                }
            }

            entries.Add(new GitDiffEntry
            {
                RelativePath = relativePath,
                DiffBody = diffBody,
                Truncated = truncated,
                LinesAdded = linesAdded,
                LinesRemoved = linesRemoved
            });

            if (truncated)
            {
                break;
            }
        }

        return entries;
    }

    private static string? ExtractPathFromDiffHeader(string header, IReadOnlyList<string> allowedPaths)
    {
        // Header format: "diff --git a/path b/path" or just "a/path b/path" after split
        // Match against known allowed paths to avoid leaking unknown paths
        foreach (string path in allowedPaths)
        {
            if (header.Contains($"b/{path}", StringComparison.Ordinal) ||
                header.Contains($"b/{path.Replace('\\', '/')}", StringComparison.Ordinal))
            {
                return path;
            }
        }

        return null;
    }

    private static string TruncateToBytes(string text, int maxBytes)
    {
        if (maxBytes <= 0)
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length <= maxBytes)
        {
            return text;
        }

        // Truncate to maxBytes boundary, ensuring valid UTF-8
        return Encoding.UTF8.GetString(bytes, 0, maxBytes);
    }

    private static int ParseCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return 0;
        }

        // Simple deterministic cursor: base64-encoded decimal offset
        try
        {
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(decoded, out int offset) ? Math.Max(0, offset) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string BuildCursor(int offset)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString()));
    }
}
