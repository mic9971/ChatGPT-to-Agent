namespace C2C.Core.Git;

/// <summary>
/// Result of UC-GIT-01 git status, containing only workspace-visible entries.
/// Denied paths are silently excluded per BR-SEC-003 and BR-SEC-004.
/// </summary>
public sealed class GitStatusResult
{
    public const int SchemaVersion = 1;

    /// <summary>Schema version for forward-compatibility per BR-COM-010.</summary>
    public int Version { get; init; } = SchemaVersion;

    /// <summary>
    /// Changed entries that passed workspace visibility policy.
    /// Sensitive and ignored paths are excluded without naming them.
    /// </summary>
    public required IReadOnlyList<GitStatusEntry> Entries { get; init; }

    /// <summary>
    /// True when the result was truncated to the server-enforced entry limit.
    /// </summary>
    public bool Truncated { get; init; }

    /// <summary>Number of entries that were denied by policy (count only, no names).</summary>
    public int DeniedCount { get; init; }
}
