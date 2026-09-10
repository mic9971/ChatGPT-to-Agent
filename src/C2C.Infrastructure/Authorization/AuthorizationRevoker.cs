using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Domain service implementing client unpairing and grant revocation adhering to UC-AUTH-05,
/// BR-AUTH-006, BR-SEC-006, and BR-COM-009.
/// Atomically revokes grants and associated refresh families without persisting or logging raw token values.
/// </summary>
public sealed class AuthorizationRevoker : IAuthorizationRevoker
{
    private readonly IAuthorizationStateStore _grantStore;
    private readonly IRefreshFamilyStore _refreshStore;
    private readonly IPairingStore _pairingStore;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AuthorizationRevoker(
        IAuthorizationStateStore grantStore,
        IRefreshFamilyStore refreshStore,
        IPairingStore pairingStore)
    {
        _grantStore = grantStore ?? throw new ArgumentNullException(nameof(grantStore));
        _refreshStore = refreshStore ?? throw new ArgumentNullException(nameof(refreshStore));
        _pairingStore = pairingStore ?? throw new ArgumentNullException(nameof(pairingStore));
    }

    public async Task<RevocationResult> RevokeClientAsync(
        string workspaceId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var activeGrants = await _grantStore.GetActiveGrantsAsync(workspaceId, cancellationToken);
            var clientGrants = activeGrants
                .Where(g => string.Equals(g.ClientId, clientId, StringComparison.Ordinal))
                .ToList();

            int grantsRevoked = 0;
            foreach (var grant in clientGrants)
            {
                await _grantStore.RevokeGrantAsync(workspaceId, grant.GrantId, cancellationToken);
                await _refreshStore.RevokeFamiliesForGrantAsync(workspaceId, grant.GrantId, cancellationToken);
                grantsRevoked++;
            }

            // Check if current pairing session was approved for this client
            var session = await _pairingStore.GetSessionAsync(workspaceId, cancellationToken);
            if (session != null && string.Equals(session.ApprovedClientId, clientId, StringComparison.Ordinal))
            {
                await _pairingStore.ClearSessionAsync(workspaceId, cancellationToken);
            }

            return new RevocationResult
            {
                WorkspaceId = workspaceId,
                ClientId = clientId,
                GrantsRevoked = grantsRevoked
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<RevocationResult> RevokeGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(grantId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var grant = await _grantStore.GetGrantAsync(workspaceId, grantId, cancellationToken);
            int grantsRevoked = 0;
            if (grant != null && grant.Status == AuthorizationGrantStatus.Active)
            {
                await _grantStore.RevokeGrantAsync(workspaceId, grantId, cancellationToken);
                await _refreshStore.RevokeFamiliesForGrantAsync(workspaceId, grantId, cancellationToken);
                grantsRevoked = 1;
            }

            return new RevocationResult
            {
                WorkspaceId = workspaceId,
                GrantId = grantId,
                GrantsRevoked = grantsRevoked
            };
        }
        finally
        {
            _lock.Release();
        }
    }
}
