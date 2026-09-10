using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Persistence contract for refresh token families adhering to 11-DATA-MODEL.md and BR-CON-002.
/// </summary>
public interface IRefreshFamilyStore
{
    Task SaveFamilyAsync(
        string workspaceId,
        RefreshFamily family,
        CancellationToken cancellationToken = default);

    Task<RefreshFamily?> GetFamilyAsync(
        string workspaceId,
        string familyId,
        CancellationToken cancellationToken = default);

    Task RevokeFamilyAsync(
        string workspaceId,
        string familyId,
        CancellationToken cancellationToken = default);

    Task RevokeFamiliesForGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default);

    Task ClearFamiliesAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);
}
