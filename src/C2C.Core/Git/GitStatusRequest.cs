namespace C2C.Core.Git;

/// <summary>
/// Request parameters for UC-GIT-01 git status operation.
/// </summary>
public sealed class GitStatusRequest
{
    /// <summary>
    /// Optional workspace-relative path scope. When null the entire workspace root is used.
    /// </summary>
    public string? PathScope { get; init; }
}
