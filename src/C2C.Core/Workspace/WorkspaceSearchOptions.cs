namespace C2C.Core.Workspace;

/// <summary>
/// Configurable limits and constraints for workspace text search operations adhering to BR-APP-005 and UC-WS-05.
/// </summary>
public sealed record WorkspaceSearchOptions
{
    public int MaxQueryLength { get; init; } = 256;
    public int DefaultLimit { get; init; } = 50;
    public int MaxLimit { get; init; } = 200;
    public int MaxSnippetLength { get; init; } = 500;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024; // 5 MB per file
}
