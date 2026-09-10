using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Runtime;

/// <summary>
/// Persistence contract for runtime process ownership record adhering to BR-CON-002 and BR-CON-008.
/// </summary>
public interface IRuntimeOwnershipStore
{
    Task SaveAsync(
        string workspaceId,
        RuntimeInstanceRecord record,
        CancellationToken cancellationToken = default);

    Task<RuntimeInstanceRecord?> LoadAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
