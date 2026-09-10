using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class JsonRefreshFamilyStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PairingOptions _options;
    private readonly JsonRefreshFamilyStore _store;

    public JsonRefreshFamilyStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_refresh_tests_" + Guid.NewGuid().ToString("N"));
        _options = new PairingOptions { StorageDirectory = _tempDir };
        _store = new JsonRefreshFamilyStore(_options);
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
    public async Task SaveAndGetFamilyAsync_PersistsAndLoadsSuccessfully()
    {
        // Arrange
        string workspaceId = "ws-test-1";
        string familyId = Guid.NewGuid().ToString("N");
        string grantId = "grant-123";
        string tokenSecret = "rt_super_secret_12345";
        string tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenSecret)));

        var family = new RefreshFamily
        {
            FamilyId = familyId,
            GrantId = grantId,
            WorkspaceId = workspaceId,
            ClientId = "client-abc",
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            CurrentGeneration = 1,
            CurrentTokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            Status = RefreshFamilyStatus.Active,
            UpdatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };

        // Act
        await _store.SaveFamilyAsync(workspaceId, family);
        var loaded = await _store.GetFamilyAsync(workspaceId, familyId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(familyId, loaded.FamilyId);
        Assert.Equal(grantId, loaded.GrantId);
        Assert.Equal(1, loaded.CurrentGeneration);
        Assert.Equal(tokenHash, loaded.CurrentTokenHash);
        Assert.Equal(RefreshFamilyStatus.Active, loaded.Status);

        // Verify BR-SEC-008: raw token secret is never stored on disk
        string filePath = Path.Combine(_tempDir, "workspaces", workspaceId, "auth", "refresh-families.json");
        string rawJson = await File.ReadAllTextAsync(filePath);
        Assert.DoesNotContain(tokenSecret, rawJson);
        Assert.Contains(tokenHash, rawJson);
    }

    [Fact]
    public async Task RevokeFamilyAsync_UpdatesStatusToRevoked()
    {
        // Arrange
        string workspaceId = "ws-test-2";
        string familyId = Guid.NewGuid().ToString("N");
        var family = new RefreshFamily
        {
            FamilyId = familyId,
            GrantId = "grant-456",
            WorkspaceId = workspaceId,
            ClientId = "client-xyz",
            Issuer = "https://c2c.local/",
            Resource = "http://127.0.0.1:5000/mcp",
            CurrentGeneration = 1,
            CurrentTokenHash = "hash1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            Status = RefreshFamilyStatus.Active,
            UpdatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };

        await _store.SaveFamilyAsync(workspaceId, family);

        // Act
        await _store.RevokeFamilyAsync(workspaceId, familyId);
        var loaded = await _store.GetFamilyAsync(workspaceId, familyId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(RefreshFamilyStatus.Revoked, loaded.Status);
    }

    [Fact]
    public async Task CorruptedFile_FailsClosed()
    {
        // Arrange
        string workspaceId = "ws-corrupt";
        string authDir = Path.Combine(_tempDir, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);
        string filePath = Path.Combine(authDir, "refresh-families.json");
        await File.WriteAllTextAsync(filePath, "{ invalid_json ]]]");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _store.GetFamilyAsync(workspaceId, "any-id"));
    }
}
