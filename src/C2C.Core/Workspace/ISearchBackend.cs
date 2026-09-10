namespace C2C.Core.Workspace;

/// <summary>
/// Search engine backend abstraction adhering to UC-WS-05.
/// </summary>
public interface ISearchBackend
{
    /// <summary>
    /// Searches visible files within the designated scope for text matches without exposing denied files or content.
    /// </summary>
    Task<IReadOnlyList<WorkspaceSearchMatch>> SearchAsync(
        IWorkspaceContext context,
        ResolvedWorkspacePath scopePath,
        string query,
        WorkspaceSearchOptions options,
        CancellationToken cancellationToken = default);
}
