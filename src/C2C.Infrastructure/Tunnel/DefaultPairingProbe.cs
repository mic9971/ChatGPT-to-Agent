using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Default pairing status probe adhering to UC-TUN-03 and UC-AUTH-01.
/// In Phase V0.4 prior to V0.5 pairing implementation, returns Paired or configured state.
/// </summary>
public sealed class DefaultPairingProbe : IPairingProbe
{
    private readonly PairingStatus _status;

    public DefaultPairingProbe(PairingStatus status = PairingStatus.Paired)
    {
        _status = status;
    }

    public Task<PairingStatus> CheckPairingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_status);
    }
}
