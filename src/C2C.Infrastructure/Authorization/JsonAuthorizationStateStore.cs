using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Atomic JSON authorization state store adhering to 11-DATA-MODEL.md, BR-CON-002, and BR-COM-011.
/// Persists grants at workspaces/{workspaceId}/auth/grants.json.
/// </summary>
public sealed class JsonAuthorizationStateStore : IAuthorizationStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonAuthorizationStateStore(PairingOptions? options = null)
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

    public async Task SaveGrantAsync(
        string workspaceId,
        AuthorizationGrant grant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(grant);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, AuthorizationGrant> grants = await LoadGrantsUnderLockAsync(workspaceId, cancellationToken);
            grants[grant.GrantId] = grant;
            await SaveGrantsUnderLockAsync(workspaceId, grants, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AuthorizationGrant?> GetGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(grantId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, AuthorizationGrant> grants = await LoadGrantsUnderLockAsync(workspaceId, cancellationToken);
            grants.TryGetValue(grantId, out AuthorizationGrant? grant);
            return grant;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<AuthorizationGrant>> GetActiveGrantsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, AuthorizationGrant> grants = await LoadGrantsUnderLockAsync(workspaceId, cancellationToken);
            return grants.Values
                .Where(g => g.Status == AuthorizationGrantStatus.Active)
                .OrderBy(g => g.CreatedAt)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RevokeGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(grantId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, AuthorizationGrant> grants = await LoadGrantsUnderLockAsync(workspaceId, cancellationToken);
            if (grants.TryGetValue(grantId, out AuthorizationGrant? grant))
            {
                grant.Status = AuthorizationGrantStatus.Revoked;
                grant.RevokedAt = DateTimeOffset.UtcNow;
                await SaveGrantsUnderLockAsync(workspaceId, grants, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearGrantsAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth", "grants.json");
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

    private async Task<Dictionary<string, AuthorizationGrant>> LoadGrantsUnderLockAsync(
        string workspaceId,
        CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth", "grants.json");
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, AuthorizationGrant>(StringComparer.Ordinal);
        }

        string json = await File.ReadAllTextAsync(filePath, cancellationToken);
        Dictionary<string, AuthorizationGrant>? grants;
        try
        {
            grants = JsonSerializer.Deserialize<Dictionary<string, AuthorizationGrant>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize authorization grants; corrupted JSON fails closed.", ex);
        }

        if (grants == null)
        {
            throw new InvalidOperationException("Grants dictionary was null; corrupted JSON fails closed.");
        }

        foreach (var grant in grants.Values)
        {
            if (grant.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported grant schema version '{grant.SchemaVersion}'. Expected 1.");
            }
        }

        return grants;
    }

    private async Task SaveGrantsUnderLockAsync(
        string workspaceId,
        Dictionary<string, AuthorizationGrant> grants,
        CancellationToken cancellationToken)
    {
        string authDir = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);

        string filePath = Path.Combine(authDir, "grants.json");
        string tempFile = $"{filePath}.tmp.{Guid.NewGuid():N}";
        string json = JsonSerializer.Serialize(grants, JsonOptions);

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
}
