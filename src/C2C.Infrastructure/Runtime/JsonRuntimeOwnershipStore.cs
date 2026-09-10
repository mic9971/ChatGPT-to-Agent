using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Runtime;

namespace C2C.Infrastructure.Runtime;

/// <summary>
/// Atomic JSON runtime instance ownership store adhering to 11-DATA-MODEL.md, BR-CON-002, and BR-CON-008.
/// Stores runtime process identity under ~/.c2c/workspaces/{workspaceId}/runtime.json.
/// </summary>
public sealed class JsonRuntimeOwnershipStore : IRuntimeOwnershipStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonRuntimeOwnershipStore(string? storageDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(storageDirectory))
        {
            _storageDirectory = Path.GetFullPath(storageDirectory);
        }
        else
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _storageDirectory = Path.Combine(home, ".c2c");
        }
    }

    public async Task SaveAsync(
        string workspaceId,
        RuntimeInstanceRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(record);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string workspaceDir = Path.Combine(_storageDirectory, "workspaces", workspaceId);
            Directory.CreateDirectory(workspaceDir);

            string filePath = Path.Combine(workspaceDir, "runtime.json");
            string tempFile = $"{filePath}.tmp.{Guid.NewGuid():N}";
            string json = JsonSerializer.Serialize(record, JsonOptions);

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

    public async Task<RuntimeInstanceRecord?> LoadAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "runtime.json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            RuntimeInstanceRecord? record = JsonSerializer.Deserialize<RuntimeInstanceRecord>(json, JsonOptions);

            if (record != null && record.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported runtime record schema version '{record.SchemaVersion}'. Expected 1.");
            }

            return record;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "runtime.json");
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
