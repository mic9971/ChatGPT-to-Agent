using System;

namespace C2C.Core.Diagnostics;

/// <summary>
/// Lightweight non-mutating status snapshot of the local C2C runtime adhering to UC-CLI-03.
/// </summary>
public sealed record RuntimeStatusSnapshot
{
    public bool IsWorkspaceConfigured { get; init; }

    public string? WorkspaceId { get; init; }

    public string? WorkspaceRoot { get; init; }

    public bool IsBridgeRunning { get; init; }

    public bool IsBridgeHealthy { get; init; }

    public string? LocalEndpoint { get; init; }

    public bool IsTunnelActive { get; init; }

    public string? TunnelPublicUrl { get; init; }

    public bool IsPairingActive { get; init; }

    public string? PairingStatus { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
