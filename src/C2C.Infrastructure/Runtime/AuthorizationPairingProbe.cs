using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Runtime;

/// <summary>
/// Real authorization-backed pairing probe adhering to UC-TUN-03, UC-CLI-04, and UC-AUTH-01.
/// Verifies whether active authorization grants exist for the configured workspace.
/// </summary>
public sealed class AuthorizationPairingProbe : IPairingProbe
{
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly IAuthorizationStateStore _authorizationStateStore;

    public AuthorizationPairingProbe(
        IWorkspaceConfigStore workspaceConfigStore,
        IAuthorizationStateStore authorizationStateStore)
    {
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
        _authorizationStateStore = authorizationStateStore ?? throw new ArgumentNullException(nameof(authorizationStateStore));
    }

    public async Task<PairingStatus> CheckPairingAsync(CancellationToken cancellationToken = default)
    {
        WorkspaceConfig? config = await _workspaceConfigStore.LoadAsync(cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.WorkspaceId))
        {
            return PairingStatus.NeedPairing;
        }

        var activeGrants = await _authorizationStateStore.GetActiveGrantsAsync(config.WorkspaceId, cancellationToken);
        if (activeGrants != null && activeGrants.Count > 0)
        {
            return PairingStatus.Paired;
        }

        return PairingStatus.NeedPairing;
    }
}
