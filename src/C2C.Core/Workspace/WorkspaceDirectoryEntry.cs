namespace C2C.Core.Workspace;

/// <summary>
/// Safe subset of directory entry metadata adhering to UC-WS-03 and docs/01-architecture/04-MCP-DESIGN.md.
/// </summary>
public sealed record WorkspaceDirectoryEntry
{
    public required string Name { get; init; }
    public required string RelativePath { get; init; }
    public required bool IsDirectory { get; init; }
    public long? SizeBytes { get; init; }
    public DateTimeOffset? LastModifiedUtc { get; init; }
}
