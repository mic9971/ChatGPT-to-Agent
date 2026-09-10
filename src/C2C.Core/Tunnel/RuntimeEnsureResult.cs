using System;
using System.Text.Json.Serialization;

namespace C2C.Core.Tunnel;

/// <summary>
/// Overall readiness status of the C2C runtime adhering to UC-TUN-03, UC-CLI-04, and 09-CLI-DESIGN.md.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeStatus
{
    Ready,
    NeedPairing,
    Degraded,
    Error
}

/// <summary>
/// Persisted and machine-readable result of a runtime ensure operation.
/// </summary>
public sealed class RuntimeEnsureResult
{
    public int SchemaVersion { get; init; } = 1;

    public required RuntimeStatus Status { get; init; }

    public required string Bridge { get; init; }

    public required string Tunnel { get; init; }

    public required string Pairing { get; init; }

    public Uri? PublicUrl { get; init; }

    public Uri? LocalEndpoint { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }
}
