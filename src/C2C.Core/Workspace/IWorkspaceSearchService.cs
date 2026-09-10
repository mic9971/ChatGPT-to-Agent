using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Workspace search service contract adhering to UC-WS-05 and BR-APP-003.
/// </summary>
public interface IWorkspaceSearchService
{
    /// <summary>
    /// Searches visible files within the workspace for text matches with pre-disclosure filtering,
    /// stable ordering, and cursor-based pagination.
    /// </summary>
    Task<OperationResult<PageResult<WorkspaceSearchMatch>>> SearchAsync(
        IWorkspaceContext context,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default);
}
