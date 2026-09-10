using System;

namespace C2C.Core.Tunnel;

/// <summary>
/// Health snapshot of a public tunnel session adhering to 07-TUNNEL-DESIGN.md.
/// </summary>
public sealed class TunnelHealth
{
    public required TunnelStatus Status { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset CheckedAt { get; init; }

    public string? ReasonCode { get; init; }

    public static TunnelHealth Healthy(DateTimeOffset checkedAt, string? message = null) => new()
    {
        Status = TunnelStatus.Healthy,
        Message = message,
        CheckedAt = checkedAt
    };

    public static TunnelHealth Failed(string reasonCode, string message, DateTimeOffset checkedAt) => new()
    {
        Status = TunnelStatus.Failed,
        ReasonCode = reasonCode,
        Message = message,
        CheckedAt = checkedAt
    };

    public static TunnelHealth Degraded(string reasonCode, string message, DateTimeOffset checkedAt) => new()
    {
        Status = TunnelStatus.Degraded,
        ReasonCode = reasonCode,
        Message = message,
        CheckedAt = checkedAt
    };

    public static TunnelHealth Starting(DateTimeOffset checkedAt) => new()
    {
        Status = TunnelStatus.Starting,
        CheckedAt = checkedAt
    };

    public static TunnelHealth Stopped(DateTimeOffset checkedAt, string? message = null) => new()
    {
        Status = TunnelStatus.Stopped,
        Message = message,
        CheckedAt = checkedAt
    };
}
