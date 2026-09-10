using System.Text.Json.Serialization;

namespace C2C.Core.Tunnel;

/// <summary>
/// Status of a public tunnel adhering to 07-TUNNEL-DESIGN.md.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TunnelStatus
{
    Starting = 0,
    Healthy = 1,
    Degraded = 2,
    Stopped = 3,
    Failed = 4
}
