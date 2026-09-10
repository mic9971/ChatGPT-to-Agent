namespace C2C.Core.Git;

/// <summary>
/// Paginated result of UC-GIT-02 git diff — only allowed, workspace-visible diff bodies.
/// </summary>
public sealed class GitDiffResult
{
    public const int SchemaVersion = 1;

    /// <summary>Schema version per BR-COM-010.</summary>
    public int Version { get; init; } = SchemaVersion;

    /// <summary>Diff entries for allowed paths on this page.</summary>
    public required IReadOnlyList<GitDiffEntry> Entries { get; init; }

    /// <summary>Opaque cursor for the next page. Null when no more records exist.</summary>
    public string? NextCursor { get; init; }

    /// <summary>Number of candidate paths denied by policy (count only, no names).</summary>
    public int DeniedCount { get; init; }

    /// <summary>True when more pages are available beyond this response.</summary>
    public bool HasMore => NextCursor is not null;
}
