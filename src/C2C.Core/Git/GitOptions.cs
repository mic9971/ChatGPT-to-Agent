namespace C2C.Core.Git;

/// <summary>
/// Configurable limits and timeouts for Git process execution adhering to BR-COM-006 and BR-COM-008.
/// </summary>
public sealed class GitOptions
{
    /// <summary>Maximum milliseconds to wait for a Git process to complete.</summary>
    public int ProcessTimeoutMs { get; set; } = 5000;

    /// <summary>Maximum number of changed-file entries returned by git_status.</summary>
    public int MaxStatusEntries { get; set; } = 500;

    /// <summary>Maximum total diff bytes returned across all paginated records.</summary>
    public int MaxDiffBytes { get; set; } = 512 * 1024; // 512 KB

    /// <summary>Maximum number of diff records (per-file hunks) returned per page.</summary>
    public int MaxDiffRecords { get; set; } = 100;

    /// <summary>Default diff records per page when caller does not specify a limit.</summary>
    public int DefaultDiffPageSize { get; set; } = 20;
}
