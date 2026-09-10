namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Configurable limits and thresholds for workspace file reading adhering to BR-COM-006.
/// </summary>
public sealed class WorkspaceFileReaderOptions
{
    public int DefaultMaxLines { get; set; } = 1000;
    public int AbsoluteMaxLines { get; set; } = 5000;
    public int DefaultMaxBytes { get; set; } = 64 * 1024; // 64 KB
    public int AbsoluteMaxBytes { get; set; } = 512 * 1024; // 512 KB
    public int BinarySampleSizeBytes { get; set; } = 8192; // 8 KB
}
