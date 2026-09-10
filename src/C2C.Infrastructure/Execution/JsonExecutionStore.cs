using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Execution;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Atomic JSON execution store adhering to 11-DATA-MODEL.md, BR-CON-001, and BR-CON-002.
/// </summary>
public sealed class JsonExecutionStore : IExecutionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonExecutionStore(ExecutionOptions? options = null)
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

    public async Task SaveAsync(ExecutionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string taskDir = GetTaskDirectory(record.WorkspaceId, record.TaskId);
            Directory.CreateDirectory(taskDir);

            string filePath = Path.Combine(taskDir, $"{record.ExecutionId}.json");
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

    public async Task<ExecutionRecord?> GetAsync(
        string workspaceId,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(executionId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string workspaceDir = Path.Combine(_storageDirectory, "executions", workspaceId);
            if (!Directory.Exists(workspaceDir))
            {
                return null;
            }

            string[] candidateFiles = Directory.GetFiles(
                workspaceDir,
                $"{executionId}.json",
                SearchOption.AllDirectories);

            if (candidateFiles.Length == 0)
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(candidateFiles[0], cancellationToken);
            ExecutionRecord? record = JsonSerializer.Deserialize<ExecutionRecord>(json, JsonOptions);

            if (record != null && record.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported execution schema version '{record.SchemaVersion}'. Expected 1.");
            }

            return record;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ExecutionRecord?> FindByIdempotencyKeyAsync(
        string workspaceId,
        string taskId,
        int iteration,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(taskId);
        ArgumentNullException.ThrowIfNull(idempotencyKey);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string taskDir = GetTaskDirectory(workspaceId, taskId);
            if (!Directory.Exists(taskDir))
            {
                return null;
            }

            string[] files = Directory.GetFiles(taskDir, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string json = await File.ReadAllTextAsync(file, cancellationToken);
                ExecutionRecord? record = JsonSerializer.Deserialize<ExecutionRecord>(json, JsonOptions);
                if (record != null &&
                    record.Iteration == iteration &&
                    string.Equals(record.IdempotencyKey, idempotencyKey, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int?> GetLatestIterationAsync(
        string workspaceId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(taskId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string taskDir = GetTaskDirectory(workspaceId, taskId);
            if (!Directory.Exists(taskDir))
            {
                return null;
            }

            string[] files = Directory.GetFiles(taskDir, "*.json", SearchOption.TopDirectoryOnly);
            int? maxIteration = null;

            foreach (string file in files)
            {
                string json = await File.ReadAllTextAsync(file, cancellationToken);
                ExecutionRecord? record = JsonSerializer.Deserialize<ExecutionRecord>(json, JsonOptions);
                if (record != null)
                {
                    if (!maxIteration.HasValue || record.Iteration > maxIteration.Value)
                    {
                        maxIteration = record.Iteration;
                    }
                }
            }

            return maxIteration;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSanitizedArtifactAsync(
        string workspaceId,
        string taskId,
        string executionId,
        string artifactId,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(taskId);
        ArgumentNullException.ThrowIfNull(executionId);
        ArgumentNullException.ThrowIfNull(artifactId);
        ArgumentNullException.ThrowIfNull(content);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string artifactsDir = Path.Combine(
                _storageDirectory,
                "executions",
                workspaceId,
                taskId,
                "artifacts",
                executionId);

            Directory.CreateDirectory(artifactsDir);

            string safeArtifactFileName = SanitizeFileName(artifactId) + ".txt";
            string filePath = Path.Combine(artifactsDir, safeArtifactFileName);
            string tempFile = $"{filePath}.tmp.{Guid.NewGuid():N}";

            await using (FileStream fs = new(
                tempFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
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

    public async Task<string?> GetSanitizedArtifactAsync(
        string workspaceId,
        string taskId,
        string executionId,
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(taskId);
        ArgumentNullException.ThrowIfNull(executionId);
        ArgumentNullException.ThrowIfNull(artifactId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string safeArtifactFileName = SanitizeFileName(artifactId) + ".txt";
            string filePath = Path.Combine(
                _storageDirectory,
                "executions",
                workspaceId,
                taskId,
                "artifacts",
                executionId,
                safeArtifactFileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetTaskDirectory(string workspaceId, string taskId)
    {
        return Path.Combine(_storageDirectory, "executions", workspaceId, taskId);
    }

    private static string SanitizeFileName(string artifactId)
    {
        char[] invalids = Path.GetInvalidFileNameChars();
        char[] sanitized = new char[artifactId.Length];
        for (int i = 0; i < artifactId.Length; i++)
        {
            char c = artifactId[i];
            sanitized[i] = Array.IndexOf(invalids, c) >= 0 ? '_' : c;
        }
        return new string(sanitized);
    }
}
