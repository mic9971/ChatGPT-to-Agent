using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Core.Git;

/// <summary>
/// Application service for Git evidence use cases (UC-GIT-01, UC-GIT-02).
/// Applies workspace visibility policy before returning any path or diff body.
/// </summary>
public interface IGitReader
{
    /// <summary>
    /// Returns bounded machine-readable working-tree status for visible paths only (UC-GIT-01).
    /// Denied paths are excluded without leaking their names per BR-SEC-003 and BR-SEC-004.
    /// </summary>
    Task<OperationResult<GitStatusResult>> GetStatusAsync(
        IWorkspaceContext context,
        GitStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns bounded diff bodies only for changed paths that pass visibility policy (UC-GIT-02).
    /// Critical invariant: allowed path list is resolved BEFORE any diff body is fetched.
    /// </summary>
    Task<OperationResult<GitDiffResult>> GetDiffAsync(
        IWorkspaceContext context,
        GitDiffRequest request,
        CancellationToken cancellationToken = default);
}
