using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Orchestrates binding, validation, and idempotent configuration of a workspace (UC-WS-01).
/// </summary>
public interface IWorkspaceConfigurator
{
    Task<OperationResult<WorkspaceConfig>> ConfigureAsync(
        WorkspaceConfigureRequest request,
        CancellationToken cancellationToken = default);
}
