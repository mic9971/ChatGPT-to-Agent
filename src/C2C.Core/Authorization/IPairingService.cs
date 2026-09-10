using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Domain service contract for managing short-lived, one-time pairing sessions adhering to UC-AUTH-01 and BR-SEC-011.
/// </summary>
public interface IPairingService
{
    /// <summary>
    /// Creates a new pairing session for the specified workspace, invalidating or rotating any prior active session.
    /// </summary>
    Task<PairingCreateResult> CreateSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current pairing session metadata (if any) for the specified workspace.
    /// </summary>
    Task<PairingSession?> GetCurrentSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and atomically consumes a pairing code for client authorization.
    /// </summary>
    Task<PairingValidationResult> ValidateAndConsumeCodeAsync(
        string workspaceId,
        string pairingCode,
        string? clientId = null,
        CancellationToken cancellationToken = default);
}
