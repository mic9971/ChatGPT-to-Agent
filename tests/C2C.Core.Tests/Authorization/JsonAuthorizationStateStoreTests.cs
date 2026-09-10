using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class JsonAuthorizationStateStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly JsonAuthorizationStateStore _store;

    public JsonAuthorizationStateStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "c2c_grants_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        PairingOptions options = new()
        {
            StorageDirectory = _testDir
        };
        _store = new JsonAuthorizationStateStore(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignored in test cleanup
            }
        }
    }

    [Fact]
    public async Task SaveGrantAsync_PersistsGrantAndCanBeRetrieved()
    {
        string workspaceId = "ws-grant-1";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthorizationGrant grant = new()
        {
            GrantId = "grant-101",
            WorkspaceId = workspaceId,
            ClientId = "test-client",
            Issuer = "https://127.0.0.1:5000/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = [AuthorizationScopes.WorkspaceRead, AuthorizationScopes.GitRead],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = now,
            SchemaVersion = 1
        };

        await _store.SaveGrantAsync(workspaceId, grant);

        string expectedPath = Path.Combine(_testDir, "workspaces", workspaceId, "auth", "grants.json");
        Assert.True(File.Exists(expectedPath));

        AuthorizationGrant? loaded = await _store.GetGrantAsync(workspaceId, "grant-101");
        Assert.NotNull(loaded);
        Assert.Equal("grant-101", loaded.GrantId);
        Assert.Equal("test-client", loaded.ClientId);
        Assert.Equal(2, loaded.Scopes.Count);
        Assert.Equal(AuthorizationGrantStatus.Active, loaded.Status);
    }

    [Fact]
    public async Task RevokeGrantAsync_UpdatesStatusToRevoked()
    {
        string workspaceId = "ws-grant-2";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AuthorizationGrant grant = new()
        {
            GrantId = "grant-202",
            WorkspaceId = workspaceId,
            ClientId = "test-client",
            Issuer = "https://127.0.0.1:5000/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = [AuthorizationScopes.WorkspaceRead],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = now,
            SchemaVersion = 1
        };

        await _store.SaveGrantAsync(workspaceId, grant);
        await _store.RevokeGrantAsync(workspaceId, "grant-202");

        AuthorizationGrant? loaded = await _store.GetGrantAsync(workspaceId, "grant-202");
        Assert.NotNull(loaded);
        Assert.Equal(AuthorizationGrantStatus.Revoked, loaded.Status);
        Assert.NotNull(loaded.RevokedAt);

        IReadOnlyList<AuthorizationGrant> activeGrants = await _store.GetActiveGrantsAsync(workspaceId);
        Assert.Empty(activeGrants);
    }

    [Fact]
    public async Task ClearGrantsAsync_DeletesGrantsFile()
    {
        string workspaceId = "ws-grant-3";
        AuthorizationGrant grant = new()
        {
            GrantId = "grant-303",
            WorkspaceId = workspaceId,
            ClientId = "test-client",
            Issuer = "https://127.0.0.1:5000/",
            Resource = "http://127.0.0.1:5000/mcp",
            Scopes = [AuthorizationScopes.WorkspaceRead],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };

        await _store.SaveGrantAsync(workspaceId, grant);
        Assert.NotNull(await _store.GetGrantAsync(workspaceId, "grant-303"));

        await _store.ClearGrantsAsync(workspaceId);
        Assert.Null(await _store.GetGrantAsync(workspaceId, "grant-303"));
    }

    [Fact]
    public async Task GetGrantAsync_WhenCorruptedJson_ThrowsInvalidOperationException()
    {
        string workspaceId = "ws-grant-corrupted";
        string authDir = Path.Combine(_testDir, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);
        string filePath = Path.Combine(authDir, "grants.json");
        await File.WriteAllTextAsync(filePath, "{ corrupt json data ... ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.GetGrantAsync(workspaceId, "any-id"));
    }
}
