using System;

namespace C2C.Core.Runtime;

/// <summary>
/// Request parameters to start the runtime bridge and optional public tunnel.
/// </summary>
public sealed record RuntimeStartRequest
{
    public required string WorkspaceId { get; init; }

    public string? LocalEndpoint { get; init; }

    public bool EnsureTunnel { get; init; }

    public bool Force { get; init; }
}

/// <summary>
/// Result of starting or converging the local runtime.
/// </summary>
public sealed record RuntimeStartResult
{
    public required string RuntimeInstanceId { get; init; }

    public required string BridgeStatus { get; init; }

    public required string LocalEndpoint { get; init; }

    public string? TunnelStatus { get; init; }

    public Uri? PublicUrl { get; init; }

    public bool AlreadyRunning { get; init; }
}
