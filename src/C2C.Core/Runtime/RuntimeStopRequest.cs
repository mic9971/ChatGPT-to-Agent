namespace C2C.Core.Runtime;

/// <summary>
/// Request parameters to stop the runtime bridge and associated tunnel.
/// </summary>
public sealed record RuntimeStopRequest
{
    public required string WorkspaceId { get; init; }

    public bool Force { get; init; }
}
