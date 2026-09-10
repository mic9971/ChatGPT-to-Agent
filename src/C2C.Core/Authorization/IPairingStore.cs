using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Persistence contract for pairing session metadata adhering to 11-DATA-MODEL.md and BR-CON-002.
/// </summary>
public interface IPairingStore
{
    Task SaveSessionAsync(
        string workspaceId,
        PairingSession session,
        CancellationToken cancellationToken = default);

    Task<PairingSession?> GetSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task ClearSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
