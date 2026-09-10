using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Orchestrates runtime convergence to READY adhering to UC-TUN-03, BR-CON-003, and BR-CON-004.
/// </summary>
public sealed class RuntimeEnsurer : IRuntimeEnsurer
{
    private readonly IBridgeRuntime _bridgeRuntime;
    private readonly ITunnelService _tunnelService;
    private readonly IPairingProbe _pairingProbe;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    public RuntimeEnsurer(
        IBridgeRuntime bridgeRuntime,
        ITunnelService tunnelService,
        IPairingProbe? pairingProbe = null)
    {
        _bridgeRuntime = bridgeRuntime ?? throw new ArgumentNullException(nameof(bridgeRuntime));
        _tunnelService = tunnelService ?? throw new ArgumentNullException(nameof(tunnelService));
        _pairingProbe = pairingProbe ?? new DefaultPairingProbe();
    }

    public async Task<RuntimeEnsureResult> EnsureAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            // 1. Check and ensure bridge runtime health
            BridgeHealth bridgeHealth = await _bridgeRuntime.CheckHealthAsync(cancellationToken);
            if (bridgeHealth.Status == BridgeStatus.Conflict)
            {
                return new RuntimeEnsureResult
                {
                    Status = RuntimeStatus.Error,
                    Bridge = "conflict",
                    Tunnel = "stopped",
                    Pairing = "unknown",
                    ErrorCode = CommonErrorCodes.Conflict,
                    Message = bridgeHealth.Message ?? "Bridge port occupied by foreign process."
                };
            }

            if (bridgeHealth.Status != BridgeStatus.Healthy)
            {
                var startBridgeResult = await _bridgeRuntime.EnsureStartedAsync(cancellationToken);
                if (startBridgeResult.IsFailure)
                {
                    return new RuntimeEnsureResult
                    {
                        Status = RuntimeStatus.Error,
                        Bridge = "failed",
                        Tunnel = "stopped",
                        Pairing = "unknown",
                        ErrorCode = startBridgeResult.Error?.Code ?? CommonErrorCodes.NotReady,
                        Message = startBridgeResult.Error?.Message ?? "Failed to start bridge listener."
                    };
                }
            }

            // 2. Ensure tunnel session (with bounded retry for transient failure)
            var tunnelResult = await _tunnelService.EnsureTunnelAsync(cancellationToken);
            if (tunnelResult.IsFailure)
            {
                return new RuntimeEnsureResult
                {
                    Status = RuntimeStatus.Error,
                    Bridge = "healthy",
                    Tunnel = "failed",
                    Pairing = "unknown",
                    ErrorCode = tunnelResult.Error?.Code ?? CommonErrorCodes.TunnelStartFailed,
                    Message = tunnelResult.Error?.Message ?? "Failed to establish public tunnel."
                };
            }

            TunnelSession tunnelSession = tunnelResult.Value!;

            // 3. Check client pairing / authorization readiness
            PairingStatus pairingStatus = await _pairingProbe.CheckPairingAsync(cancellationToken);
            if (pairingStatus == PairingStatus.NeedPairing)
            {
                return new RuntimeEnsureResult
                {
                    Status = RuntimeStatus.NeedPairing,
                    Bridge = "healthy",
                    Tunnel = "healthy",
                    Pairing = "need_pairing",
                    PublicUrl = tunnelSession.PublicUrl,
                    LocalEndpoint = tunnelSession.LocalEndpoint,
                    ErrorCode = CommonErrorCodes.NotReady,
                    Message = "Client pairing required."
                };
            }

            // 4. All stages healthy -> READY
            return new RuntimeEnsureResult
            {
                Status = RuntimeStatus.Ready,
                Bridge = "healthy",
                Tunnel = "healthy",
                Pairing = "paired",
                PublicUrl = tunnelSession.PublicUrl,
                LocalEndpoint = tunnelSession.LocalEndpoint
            };
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
