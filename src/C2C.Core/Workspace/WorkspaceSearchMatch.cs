namespace C2C.Core.Workspace;

/// <summary>
/// Represents a single text search match adhering to UC-WS-05 and docs/01-architecture/04-MCP-DESIGN.md.
/// </summary>
public sealed record WorkspaceSearchMatch
{
    public required string RelativePath { get; init; }
    public required int LineNumber { get; init; }
    public required string Snippet { get; init; }
}
