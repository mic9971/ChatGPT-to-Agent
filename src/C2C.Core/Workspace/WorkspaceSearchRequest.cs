namespace C2C.Core.Workspace;

/// <summary>
/// Workspace search request arguments adhering to UC-WS-05.
/// </summary>
public sealed record WorkspaceSearchRequest(
    string Query,
    string? PathScope = null,
    string? Cursor = null,
    int? Limit = null);
