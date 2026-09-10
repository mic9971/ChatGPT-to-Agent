namespace C2C.Core.Workspace;

/// <summary>
/// Request parameters for binding or reconfiguring a workspace.
/// </summary>
public sealed record WorkspaceConfigureRequest(
    string Path,
    string? Label = null,
    bool Force = false);
