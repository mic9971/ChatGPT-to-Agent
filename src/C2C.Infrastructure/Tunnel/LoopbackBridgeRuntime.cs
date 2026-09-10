using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Probes loopback bridge listener health and manages readiness adhering to UC-TUN-03 and BR-SEC-005.
/// </summary>
public sealed class LoopbackBridgeRuntime : IBridgeRuntime
{
    private readonly TunnelOptions _options;

    public LoopbackBridgeRuntime(TunnelOptions? options = null)
    {
        _options = options ?? new TunnelOptions();
    }

    public async Task<BridgeHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        Uri endpoint = _options.LocalEndpoint;

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await client.ConnectAsync(endpoint.Host, endpoint.Port, linkedCts.Token);
            return new BridgeHealth(BridgeStatus.Healthy, endpoint);
        }
        catch (SocketException)
        {
            return new BridgeHealth(BridgeStatus.Stopped, endpoint, "Bridge port is not currently accepting connections.");
        }
        catch (OperationCanceledException)
        {
            return new BridgeHealth(BridgeStatus.Stopped, endpoint, "Bridge health probe timed out.");
        }
        catch (Exception ex)
        {
            return new BridgeHealth(BridgeStatus.Failed, endpoint, $"Bridge probe error: {ex.Message}");
        }
    }

    public Task<OperationResult<Uri>> EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        // For in-host runtime, local endpoint is configured and active
        return Task.FromResult(OperationResult<Uri>.Success(_options.LocalEndpoint));
    }
}
