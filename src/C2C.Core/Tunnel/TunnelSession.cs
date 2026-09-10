using System;

namespace C2C.Core.Tunnel;

/// <summary>
/// Persisted tunnel session metadata adhering to 07-TUNNEL-DESIGN.md, 11-DATA-MODEL.md, and BR-COM-010.
/// </summary>
public sealed class TunnelSession
{
    public required string WorkspaceId { get; init; }

    public required string Provider { get; init; }

    public required Uri PublicUrl { get; init; }

    public required Uri LocalEndpoint { get; init; }

    public int? ProcessId { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required string OwnershipMarker { get; init; }

    public int SchemaVersion { get; init; } = 1;
}
