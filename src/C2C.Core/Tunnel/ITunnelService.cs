using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Tunnel;

/// <summary>
/// Service contract for managing public tunnel lifecycle adhering to UC-TUN-01 and BR-CON-004.
/// </summary>
public interface ITunnelService
{
    Task<OperationResult<TunnelSession>> StartTunnelAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<TunnelSession>> EnsureTunnelAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> StopTunnelAsync(
        CancellationToken cancellationToken = default);

    Task<TunnelSession?> GetCurrentSessionAsync(
        CancellationToken cancellationToken = default);
}
