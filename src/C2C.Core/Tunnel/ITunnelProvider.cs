using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Tunnel;

/// <summary>
/// Abstraction for a tunnel provider adhering to 07-TUNNEL-DESIGN.md.
/// </summary>
public interface ITunnelProvider
{
    string ProviderName { get; }

    Task<OperationResult<TunnelSession>> StartAsync(
        Uri localEndpoint,
        CancellationToken cancellationToken);

    Task<TunnelHealth> GetHealthAsync(
        TunnelSession session,
        CancellationToken cancellationToken);

    Task StopAsync(
        TunnelSession session,
        CancellationToken cancellationToken);
}
