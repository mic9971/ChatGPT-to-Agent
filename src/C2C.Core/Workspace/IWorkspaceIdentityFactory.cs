using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Factory for deriving stable, salted workspace identifiers without leaking local absolute paths.
/// </summary>
public interface IWorkspaceIdentityFactory
{
    WorkspaceId Create(string canonicalRoot);
}
