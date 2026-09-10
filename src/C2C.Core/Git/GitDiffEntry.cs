namespace C2C.Core.Git;

/// <summary>
/// A single per-file diff record for an allowed, workspace-visible path.
/// Per UC-GIT-02: diff bodies are only fetched for allowed paths, never captured broadly.
/// </summary>
public sealed class GitDiffEntry
{
    /// <summary>Workspace-relative path of the changed file.</summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Original path before rename. Null for non-rename entries.
    /// </summary>
    public string? OriginalPath { get; init; }

    /// <summary>Unified diff body for this file, bounded by <see cref="GitOptions.MaxDiffBytes"/>.</summary>
    public required string DiffBody { get; init; }

    /// <summary>True when the diff body was truncated due to the server byte limit.</summary>
    public bool Truncated { get; init; }

    /// <summary>Number of lines added.</summary>
    public int LinesAdded { get; init; }

    /// <summary>Number of lines removed.</summary>
    public int LinesRemoved { get; init; }
}
