using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class AuthorizationRevokerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PairingOptions _options;
    private readonly JsonAuthorizationStateStore _grantStore;
    private readonly JsonRefreshFamilyStore _refreshStore;
    private readonly JsonPairingStore _pairingStore;
    private readonly AuthorizationRevoker _revoker;

    public AuthorizationRevokerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_revoker_tests_" + Guid.NewGuid().ToString("N"));
        _options = new PairingOptions { StorageDirectory = _tempDir };
        _grantStore = new JsonAuthorizationStateStore(_options);
        _refreshStore = new JsonRefreshFamilyStore(_options);
        _pairingStore = new JsonPairingStore(_options);
        _revoker = new AuthorizationRevoker(_grantStore, _refreshStore, _pairingStore);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task RevokeClientAsync_RevokesAllGrantsAndRefreshFamiliesForClient()
    {
        // Arrange
        string workspaceId = "ws-revoke-1";
        string targetClientId = "client-target";
        string otherClientId = "client-other";

        // Grant 1 for target client
        var grant1 = new AuthorizationGrant
        {
            GrantId = "grant-1",
            WorkspaceId = workspaceId,
            ClientId = targetClientId,
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = ["workspace.read"],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };
        await _grantStore.SaveGrantAsync(workspaceId, grant1);

        // Grant 2 for other client (should NOT be revoked)
        var grant2 = new AuthorizationGrant
        {
            GrantId = "grant-2",
            WorkspaceId = workspaceId,
            ClientId = otherClientId,
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = ["git.read"],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };
        await _grantStore.SaveGrantAsync(workspaceId, grant2);

        // Refresh family for target grant
        var family1 = new RefreshFamily
        {
            FamilyId = "family-1",
            GrantId = "grant-1",
            WorkspaceId = workspaceId,
            ClientId = targetClientId,
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            CurrentGeneration = 1,
            CurrentTokenHash = "hash1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            Status = RefreshFamilyStatus.Active,
            UpdatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };
        await _refreshStore.SaveFamilyAsync(workspaceId, family1);

        // Act
        var result = await _revoker.RevokeClientAsync(workspaceId, targetClientId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.GrantsRevoked);

        var loadedGrant1 = await _grantStore.GetGrantAsync(workspaceId, "grant-1");
        Assert.NotNull(loadedGrant1);
        Assert.Equal(AuthorizationGrantStatus.Revoked, loadedGrant1.Status);

        var loadedGrant2 = await _grantStore.GetGrantAsync(workspaceId, "grant-2");
        Assert.NotNull(loadedGrant2);
        Assert.Equal(AuthorizationGrantStatus.Active, loadedGrant2.Status); // Preserved!

        var loadedFamily1 = await _refreshStore.GetFamilyAsync(workspaceId, "family-1");
        Assert.NotNull(loadedFamily1);
        Assert.Equal(RefreshFamilyStatus.Revoked, loadedFamily1.Status);
    }

    [Fact]
    public async Task RevokeClientAsync_IsIdempotent()
    {
        // Arrange
        string workspaceId = "ws-revoke-2";
        string clientId = "client-idempotent";

        // Act 1: when no grants exist
        var result1 = await _revoker.RevokeClientAsync(workspaceId, clientId);
        Assert.True(result1.IsSuccess);
        Assert.Equal(0, result1.GrantsRevoked);

        // Act 2: second call
        var result2 = await _revoker.RevokeClientAsync(workspaceId, clientId);
        Assert.True(result2.IsSuccess);
        Assert.Equal(0, result2.GrantsRevoked);
    }

    [Fact]
    public async Task RevokeGrantAsync_RevokesTargetGrantAndRefreshFamily()
    {
        // Arrange
        string workspaceId = "ws-revoke-3";
        string grantId = "grant-target";

        var grant = new AuthorizationGrant
        {
            GrantId = grantId,
            WorkspaceId = workspaceId,
            ClientId = "client-xyz",
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = ["terminal.read"],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };
        await _grantStore.SaveGrantAsync(workspaceId, grant);

        var family = new RefreshFamily
        {
            FamilyId = "family-target",
            GrantId = grantId,
            WorkspaceId = workspaceId,
            ClientId = "client-xyz",
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            CurrentGeneration = 1,
            CurrentTokenHash = "hash-target",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            Status = RefreshFamilyStatus.Active,
            UpdatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };
        await _refreshStore.SaveFamilyAsync(workspaceId, family);

        // Act
        var result = await _revoker.RevokeGrantAsync(workspaceId, grantId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.GrantsRevoked);

        var loadedGrant = await _grantStore.GetGrantAsync(workspaceId, grantId);
        Assert.NotNull(loadedGrant);
        Assert.Equal(AuthorizationGrantStatus.Revoked, loadedGrant.Status);

        var loadedFamily = await _refreshStore.GetFamilyAsync(workspaceId, "family-target");
        Assert.NotNull(loadedFamily);
        Assert.Equal(RefreshFamilyStatus.Revoked, loadedFamily.Status);
    }
}
