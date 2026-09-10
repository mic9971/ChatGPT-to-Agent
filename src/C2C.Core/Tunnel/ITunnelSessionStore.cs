using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Tunnel;

/// <summary>
/// Persistence contract for tunnel session metadata adhering to 11-DATA-MODEL.md and BR-CON-002.
/// </summary>
public interface ITunnelSessionStore
{
    Task SaveSessionAsync(
        string workspaceId,
        TunnelSession session,
        CancellationToken cancellationToken = default);

    Task<TunnelSession?> GetSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task ClearSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
