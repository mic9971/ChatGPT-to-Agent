using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Workspace directory reader service contract adhering to UC-WS-03 and BR-APP-003.
/// </summary>
public interface IWorkspaceDirectoryReader
{
    /// <summary>
    /// Enumerates direct child entries under the specified workspace-relative path with pre-disclosure filtering,
    /// stable ordering, and cursor-based pagination.
    /// </summary>
    Task<OperationResult<PageResult<WorkspaceDirectoryEntry>>> ListAsync(
        IWorkspaceContext context,
        string? relativePath,
        PageRequest? pageRequest = null,
        CancellationToken cancellationToken = default);
}
