namespace C2C.Core.Workspace;

/// <summary>
/// Safe workspace metadata DTO adhering to UC-WS-02 and BR-COM-007 (no absolute path leakage).
/// </summary>
public sealed record WorkspaceInfoDto
{
    public required string WorkspaceId { get; init; }
    public string? Label { get; init; }
    public required bool GitAvailable { get; init; }
    public string? GitBranch { get; init; }
    public required bool ReadOnly { get; init; }
    public required IReadOnlyList<string> Capabilities { get; init; }
    public required WorkspaceLimitsDto Limits { get; init; }
    public required string BridgeVersion { get; init; }
}

public sealed record WorkspaceLimitsDto(
    int DefaultMaxLines,
    int AbsoluteMaxLines,
    int DefaultMaxBytes,
    int AbsoluteMaxBytes);
