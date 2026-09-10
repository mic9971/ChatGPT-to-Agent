namespace C2C.Core.Workspace;

/// <summary>
/// Persistence contract for workspace configuration adhering to BR-CON-002 (atomic write).
/// </summary>
public interface IWorkspaceConfigStore
{
    Task<WorkspaceConfig?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WorkspaceConfig config, CancellationToken cancellationToken = default);
}
