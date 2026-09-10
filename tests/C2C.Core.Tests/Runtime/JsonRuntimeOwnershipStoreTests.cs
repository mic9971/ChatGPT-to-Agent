using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Runtime;
using C2C.Infrastructure.Runtime;

namespace C2C.Core.Tests.Runtime;

public sealed class JsonRuntimeOwnershipStoreTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly JsonRuntimeOwnershipStore _store;

    public JsonRuntimeOwnershipStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_ownership_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _store = new JsonRuntimeOwnershipStore(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task LoadAsync_WhenNoRecord_ReturnsNull()
    {
        var record = await _store.LoadAsync("ws-empty");
        Assert.Null(record);
    }

    [Fact]
    public async Task SaveAndLoadAsync_PersistsRecordAtomically()
    {
        var record = new RuntimeInstanceRecord
        {
            WorkspaceId = "ws-1",
            RuntimeInstanceId = "inst-1",
            ProcessId = 4321,
            ProcessStartTime = DateTimeOffset.UtcNow,
            LocalEndpoint = "http://127.0.0.1:5000",
            ExecutablePath = "/usr/local/bin/dotnet",
            StartedAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        };

        await _store.SaveAsync("ws-1", record);

        var loaded = await _store.LoadAsync("ws-1");
        Assert.NotNull(loaded);
        Assert.Equal("ws-1", loaded.WorkspaceId);
        Assert.Equal("inst-1", loaded.RuntimeInstanceId);
        Assert.Equal(4321, loaded.ProcessId);
        Assert.Equal("http://127.0.0.1:5000", loaded.LocalEndpoint);
        Assert.Equal(1, loaded.SchemaVersion);

        // Temp files should have been renamed
        string wsDir = Path.Combine(_tempRoot, "workspaces", "ws-1");
        var tmpFiles = Directory.GetFiles(wsDir, "*.tmp.*");
        Assert.Empty(tmpFiles);
    }

    [Fact]
    public async Task ClearAsync_RemovesRecordFile()
    {
        var record = new RuntimeInstanceRecord
        {
            WorkspaceId = "ws-clear",
            RuntimeInstanceId = "inst-clear",
            ProcessId = 1111,
            ProcessStartTime = DateTimeOffset.UtcNow,
            LocalEndpoint = "http://127.0.0.1:5000"
        };

        await _store.SaveAsync("ws-clear", record);
        Assert.NotNull(await _store.LoadAsync("ws-clear"));

        await _store.ClearAsync("ws-clear");
        Assert.Null(await _store.LoadAsync("ws-clear"));
    }

    [Fact]
    public async Task LoadAsync_InvalidSchemaVersion_ThrowsInvalidOperationException()
    {
        string wsDir = Path.Combine(_tempRoot, "workspaces", "ws-invalid");
        Directory.CreateDirectory(wsDir);
        string filePath = Path.Combine(wsDir, "runtime.json");

        await File.WriteAllTextAsync(filePath, "{\"schemaVersion\": 99, \"workspaceId\": \"ws-invalid\", \"runtimeInstanceId\": \"i\", \"processId\": 1, \"processStartTime\": \"2026-01-01T00:00:00Z\", \"localEndpoint\": \"http://127.0.0.1:5000\"}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.LoadAsync("ws-invalid"));
    }
}
