namespace C2C.Core.Workspace;

/// <summary>
/// Request parameters for reading a bounded text chunk from a workspace file (UC-WS-04).
/// </summary>
public sealed record FileReadRequest(
    string Path,
    int StartLine = 1,
    int? MaxLines = null,
    long StartByte = 0,
    int? MaxBytes = null,
    string? ExpectedFingerprint = null);
