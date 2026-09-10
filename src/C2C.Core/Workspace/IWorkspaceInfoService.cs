namespace C2C.Core.Workspace;

/// <summary>
/// Query contract for returning safe workspace metadata (UC-WS-02).
/// </summary>
public interface IWorkspaceInfoService
{
    Task<WorkspaceInfoDto> GetWorkspaceInfoAsync(
        IWorkspaceContext context,
        CancellationToken cancellationToken = default);
}
