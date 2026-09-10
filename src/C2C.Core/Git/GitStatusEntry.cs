namespace C2C.Core.Git;

/// <summary>
/// Represents a single changed-file entry from git status output.
/// All paths are workspace-relative and have passed visibility policy.
/// </summary>
public sealed class GitStatusEntry
{
    /// <summary>Index status character from porcelain v1 format (e.g. 'M', 'A', 'D', '?', 'R').</summary>
    public required char IndexStatus { get; init; }

    /// <summary>Working-tree status character from porcelain v1 format.</summary>
    public required char WorktreeStatus { get; init; }

    /// <summary>Workspace-relative path of the changed file.</summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Original path before rename, if this is a rename entry.
    /// Null for non-rename entries.
    /// </summary>
    public string? OriginalPath { get; init; }

    /// <summary>Human-readable combined status label (e.g. "modified", "added", "renamed").</summary>
    public required string StatusLabel { get; init; }
}
