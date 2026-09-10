using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Atomic JSON tunnel session store adhering to 11-DATA-MODEL.md and BR-CON-002.
/// </summary>
public sealed class JsonTunnelSessionStore : ITunnelSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonTunnelSessionStore(TunnelOptions? options = null)
    {
        if (!string.IsNullOrWhiteSpace(options?.StorageDirectory))
        {
            _storageDirectory = Path.GetFullPath(options.StorageDirectory);
        }
        else
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _storageDirectory = Path.Combine(home, ".c2c");
        }
    }

    public async Task SaveSessionAsync(
        string workspaceId,
        TunnelSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(session);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string workspaceDir = Path.Combine(_storageDirectory, "workspaces", workspaceId);
            Directory.CreateDirectory(workspaceDir);

            string filePath = Path.Combine(workspaceDir, "tunnel.json");
            string tempFile = $"{filePath}.tmp.{Guid.NewGuid():N}";
            string json = JsonSerializer.Serialize(session, JsonOptions);

            await using (FileStream fs = new(
                tempFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                await fs.WriteAsync(bytes, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }

            File.Move(tempFile, filePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TunnelSession?> GetSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "tunnel.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            TunnelSession? session = JsonSerializer.Deserialize<TunnelSession>(json, JsonOptions);

            if (session != null && session.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported tunnel session schema version '{session.SchemaVersion}'. Expected 1.");
            }

            return session;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearSessionAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "tunnel.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
