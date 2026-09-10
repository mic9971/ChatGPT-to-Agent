using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Persistence contract for authorization grants adhering to 11-DATA-MODEL.md and BR-CON-002.
/// </summary>
public interface IAuthorizationStateStore
{
    Task SaveGrantAsync(
        string workspaceId,
        AuthorizationGrant grant,
        CancellationToken cancellationToken = default);

    Task<AuthorizationGrant?> GetGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizationGrant>> GetActiveGrantsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task RevokeGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default);

    Task ClearGrantsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
