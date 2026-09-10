using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Tunnel;
using C2C.Infrastructure.Tunnel;

namespace C2C.Core.Tests.Tunnel;

public sealed class JsonTunnelSessionStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonTunnelSessionStore _store;

    public JsonTunnelSessionStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_tunnel_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _store = new JsonTunnelSessionStore(new TunnelOptions
        {
            StorageDirectory = _tempDir
        });
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
    public async Task SaveAndGetSessionAsync_AtomicSave_LoadsCorrectly()
    {
        string workspaceId = "ws-test-1";
        TunnelSession session = new()
        {
            WorkspaceId = workspaceId,
            Provider = "cloudflare-quick",
            PublicUrl = new Uri("https://test-tunnel.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            ProcessId = 1234,
            StartedAt = DateTimeOffset.UtcNow,
            OwnershipMarker = "marker-1",
            SchemaVersion = 1
        };

        await _store.SaveSessionAsync(workspaceId, session);

        TunnelSession? loaded = await _store.GetSessionAsync(workspaceId);

        Assert.NotNull(loaded);
        Assert.Equal(workspaceId, loaded.WorkspaceId);
        Assert.Equal("cloudflare-quick", loaded.Provider);
        Assert.Equal("https://test-tunnel.trycloudflare.com/", loaded.PublicUrl.ToString());
        Assert.Equal("http://127.0.0.1:5000/", loaded.LocalEndpoint.ToString());
        Assert.Equal(1234, loaded.ProcessId);
        Assert.Equal("marker-1", loaded.OwnershipMarker);
        Assert.Equal(1, loaded.SchemaVersion);
    }

    [Fact]
    public async Task GetSessionAsync_NonExistent_ReturnsNull()
    {
        TunnelSession? session = await _store.GetSessionAsync("non-existent-ws");
        Assert.Null(session);
    }

    [Fact]
    public async Task ClearSessionAsync_DeletesSessionFile()
    {
        string workspaceId = "ws-clear";
        TunnelSession session = new()
        {
            WorkspaceId = workspaceId,
            Provider = "cloudflare-quick",
            PublicUrl = new Uri("https://test-clear.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            StartedAt = DateTimeOffset.UtcNow,
            OwnershipMarker = "marker-c",
            SchemaVersion = 1
        };

        await _store.SaveSessionAsync(workspaceId, session);
        Assert.NotNull(await _store.GetSessionAsync(workspaceId));

        await _store.ClearSessionAsync(workspaceId);
        Assert.Null(await _store.GetSessionAsync(workspaceId));
    }

    [Fact]
    public async Task GetSessionAsync_UnsupportedSchemaVersion_ThrowsException()
    {
        string workspaceId = "ws-corrupt";
        string workspaceDir = Path.Combine(_tempDir, "workspaces", workspaceId);
        Directory.CreateDirectory(workspaceDir);

        string json = """
        {
            "workspaceId": "ws-corrupt",
            "provider": "cloudflare-quick",
            "publicUrl": "https://corrupt.trycloudflare.com",
            "localEndpoint": "http://127.0.0.1:5000",
            "startedAt": "2026-09-10T00:00:00Z",
            "ownershipMarker": "m",
            "schemaVersion": 999
        }
        """;

        await File.WriteAllTextAsync(Path.Combine(workspaceDir, "tunnel.json"), json);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.GetSessionAsync(workspaceId));
    }
}
