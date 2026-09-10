using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Domain contract for client unpairing and authorization grant revocation adhering to UC-AUTH-05,
/// BR-AUTH-006, BR-SEC-006, and BR-COM-009.
/// </summary>
public interface IAuthorizationRevoker
{
    Task<RevocationResult> RevokeClientAsync(
        string workspaceId,
        string clientId,
        CancellationToken cancellationToken = default);

    Task<RevocationResult> RevokeGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default);
}
