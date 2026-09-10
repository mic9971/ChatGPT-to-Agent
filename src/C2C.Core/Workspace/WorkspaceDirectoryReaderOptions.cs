namespace C2C.Core.Workspace;

/// <summary>
/// Configurable bounds for directory listing operations adhering to BR-APP-005 and UC-WS-03.
/// </summary>
public sealed record WorkspaceDirectoryReaderOptions
{
    public int DefaultLimit { get; init; } = 100;
    public int MaxLimit { get; init; } = 500;
}
