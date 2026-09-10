using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Immutable context describing the currently bound workspace.
/// </summary>
public interface IWorkspaceContext
{
    WorkspaceId Id { get; }
    string CanonicalRoot { get; }
    string? Label { get; }
}

public sealed record WorkspaceContext(
    WorkspaceId Id,
    string CanonicalRoot,
    string? Label = null) : IWorkspaceContext;
