using System.Text.Json;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Atomic JSON configuration store adhering to BR-CON-002 and BR-COM-010.
/// </summary>
public sealed class JsonWorkspaceConfigStore : IWorkspaceConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _configFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonWorkspaceConfigStore(string? configFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(configFilePath))
        {
            _configFilePath = Path.GetFullPath(configFilePath);
        }
        else
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _configFilePath = Path.Combine(home, ".c2c", "workspace.json");
        }
    }

    public async Task<WorkspaceConfig?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_configFilePath))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(_configFilePath, cancellationToken);
            WorkspaceConfig? config = JsonSerializer.Deserialize<WorkspaceConfig>(json, JsonOptions);

            if (config != null && config.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported schema version '{config.SchemaVersion}'. Expected 1.");
            }

            return config;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(WorkspaceConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string? dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempFile = $"{_configFilePath}.tmp.{Guid.NewGuid():N}";
            string json = JsonSerializer.Serialize(config, JsonOptions);

            // 1. Write to temporary file with flush
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

            // 2. Atomic replace
            File.Move(tempFile, _configFilePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }
}
