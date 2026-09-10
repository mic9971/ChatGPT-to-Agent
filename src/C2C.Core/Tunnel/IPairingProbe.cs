using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Tunnel;

/// <summary>
/// Status of client pairing/authorization readiness adhering to UC-TUN-03 and UC-AUTH-01.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PairingStatus
{
    Paired,
    NeedPairing
}

/// <summary>
/// Probe for verifying if an authorized pairing session exists.
/// </summary>
public interface IPairingProbe
{
    Task<PairingStatus> CheckPairingAsync(CancellationToken cancellationToken = default);
}
