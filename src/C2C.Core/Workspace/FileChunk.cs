namespace C2C.Core.Workspace;

/// <summary>
/// Bounded text chunk returned by workspace file reader adhering to BR-COM-006 and UC-WS-04.
/// </summary>
public sealed record FileChunk
{
    public required string RelativePath { get; init; }
    public required string Content { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
    public required int TotalLinesRead { get; init; }
    public required long StartByte { get; init; }
    public required long EndByte { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string Encoding { get; init; }
    public required string Fingerprint { get; init; }
    public required bool IsTruncated { get; init; }
    public string? NextCursor { get; init; }
}
