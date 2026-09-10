using C2C.Core.Common;

namespace C2C.Core.Git;

/// <summary>
/// Request parameters for UC-GIT-02 git diff operation.
/// Two-stage design: paths are filtered by policy before diff bodies are fetched.
/// </summary>
public sealed class GitDiffRequest
{
    /// <summary>
    /// Optional base ref (e.g. "HEAD", "main"). When null, compares working tree to index.
    /// </summary>
    public string? Base { get; init; }

    /// <summary>Pagination cursor from a prior response. Null for first page.</summary>
    public string? Cursor { get; init; }

    /// <summary>Maximum number of diff records to return. Server enforces hard limit.</summary>
    public int? Limit { get; init; }
}
