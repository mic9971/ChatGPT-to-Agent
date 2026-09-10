using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Tunnel;

/// <summary>
/// Status of the loopback MCP bridge listener adhering to UC-TUN-03 and BR-SEC-005.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BridgeStatus
{
    Healthy,
    Stopped,
    Conflict,
    Failed
}

/// <summary>
/// Health snapshot of the loopback bridge listener.
/// </summary>
public sealed record BridgeHealth(BridgeStatus Status, Uri? Endpoint, string? Message = null);

/// <summary>
/// Abstraction for managing and probing loopback bridge lifecycle.
/// </summary>
public interface IBridgeRuntime
{
    Task<BridgeHealth> CheckHealthAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<Uri>> EnsureStartedAsync(CancellationToken cancellationToken = default);
}
