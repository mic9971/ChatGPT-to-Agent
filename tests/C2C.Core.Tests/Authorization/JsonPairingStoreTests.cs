using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class JsonPairingStoreTests : IDisposable
{
    private readonly string _testDir;
    private readonly JsonPairingStore _store;

    public JsonPairingStoreTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "c2c_pairing_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        PairingOptions options = new()
        {
            StorageDirectory = _testDir
        };
        _store = new JsonPairingStore(options);
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
    public async Task SaveSessionAsync_PersistsSessionAtCorrectPathAndCanBeReadBack()
    {
        // Arrange
        string workspaceId = "ws-test-1";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PairingSession session = new()
        {
            PairingSessionId = "session-123",
            WorkspaceId = workspaceId,
            CodeHash = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
            AttemptsRemaining = 3,
            Status = PairingSessionStatus.Active,
            SchemaVersion = 1
        };

        // Act
        await _store.SaveSessionAsync(workspaceId, session);

        // Assert
        string expectedPath = Path.Combine(_testDir, "workspaces", workspaceId, "auth", "pairing.json");
        Assert.True(File.Exists(expectedPath));

        PairingSession? loaded = await _store.GetSessionAsync(workspaceId);
        Assert.NotNull(loaded);
        Assert.Equal("session-123", loaded.PairingSessionId);
        Assert.Equal(workspaceId, loaded.WorkspaceId);
        Assert.Equal(session.CodeHash, loaded.CodeHash);
        Assert.Equal(3, loaded.AttemptsRemaining);
        Assert.Equal(PairingSessionStatus.Active, loaded.Status);
        Assert.Equal(1, loaded.SchemaVersion);
    }

    [Fact]
    public async Task SaveSessionAsync_OverwritesPriorSessionAtomically()
    {
        // Arrange
        string workspaceId = "ws-test-2";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PairingSession session1 = new()
        {
            PairingSessionId = "session-1",
            WorkspaceId = workspaceId,
            CodeHash = "HASH1",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
            AttemptsRemaining = 3,
            Status = PairingSessionStatus.Active,
            SchemaVersion = 1
        };

        PairingSession session2 = new()
        {
            PairingSessionId = "session-2",
            WorkspaceId = workspaceId,
            CodeHash = "HASH2",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
            AttemptsRemaining = 2,
            Status = PairingSessionStatus.Active,
            SchemaVersion = 1
        };

        // Act
        await _store.SaveSessionAsync(workspaceId, session1);
        await _store.SaveSessionAsync(workspaceId, session2);

        // Assert
        PairingSession? loaded = await _store.GetSessionAsync(workspaceId);
        Assert.NotNull(loaded);
        Assert.Equal("session-2", loaded.PairingSessionId);
        Assert.Equal("HASH2", loaded.CodeHash);
    }

    [Fact]
    public async Task ClearSessionAsync_DeletesSessionFile()
    {
        // Arrange
        string workspaceId = "ws-test-3";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PairingSession session = new()
        {
            PairingSessionId = "session-clear",
            WorkspaceId = workspaceId,
            CodeHash = "HASH",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
            AttemptsRemaining = 3,
            Status = PairingSessionStatus.Active,
            SchemaVersion = 1
        };

        await _store.SaveSessionAsync(workspaceId, session);
        Assert.NotNull(await _store.GetSessionAsync(workspaceId));

        // Act
        await _store.ClearSessionAsync(workspaceId);

        // Assert
        Assert.Null(await _store.GetSessionAsync(workspaceId));
    }

    [Fact]
    public async Task GetSessionAsync_WhenFileNotFound_ReturnsNull()
    {
        // Act
        PairingSession? result = await _store.GetSessionAsync("ws-nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionAsync_WhenCorruptedJson_ThrowsInvalidOperationException()
    {
        // Arrange
        string workspaceId = "ws-corrupted";
        string authDir = Path.Combine(_testDir, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);
        string filePath = Path.Combine(authDir, "pairing.json");
        await File.WriteAllTextAsync(filePath, "{ invalid json content! }}}");

        // Act & Assert (Fails closed per BR-COM-011)
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.GetSessionAsync(workspaceId));
    }

    [Fact]
    public async Task GetSessionAsync_WhenUnsupportedSchemaVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        string workspaceId = "ws-schema-mismatch";
        string authDir = Path.Combine(_testDir, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);
        string filePath = Path.Combine(authDir, "pairing.json");
        string json = """
        {
            "pairingSessionId": "session-1",
            "workspaceId": "ws-schema-mismatch",
            "codeHash": "HASH",
            "createdAt": "2026-09-10T12:00:00Z",
            "expiresAt": "2026-09-10T12:05:00Z",
            "attemptsRemaining": 3,
            "status": "Active",
            "schemaVersion": 999
        }
        """;
        await File.WriteAllTextAsync(filePath, json);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.GetSessionAsync(workspaceId));
    }

    [Fact]
    public async Task ThrowsArgumentNullException_OnNullParameters()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.GetSessionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.SaveSessionAsync(null!, new PairingSession
        {
            PairingSessionId = "id",
            WorkspaceId = "ws",
            CodeHash = "h",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow,
            SchemaVersion = 1
        }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.ClearSessionAsync(null!));
    }
}
