using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Atomic JSON refresh family store adhering to 11-DATA-MODEL.md, BR-CON-002, and BR-COM-011.
/// Persists refresh family state at workspaces/{workspaceId}/auth/refresh-families.json.
/// </summary>
public sealed class JsonRefreshFamilyStore : IRefreshFamilyStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storageDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonRefreshFamilyStore(PairingOptions? options = null)
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

    public async Task SaveFamilyAsync(
        string workspaceId,
        RefreshFamily family,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(family);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, RefreshFamily> families = await LoadFamiliesUnderLockAsync(workspaceId, cancellationToken);
            families[family.FamilyId] = family;
            await SaveFamiliesUnderLockAsync(workspaceId, families, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<RefreshFamily?> GetFamilyAsync(
        string workspaceId,
        string familyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(familyId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, RefreshFamily> families = await LoadFamiliesUnderLockAsync(workspaceId, cancellationToken);
            families.TryGetValue(familyId, out RefreshFamily? family);
            return family;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RevokeFamilyAsync(
        string workspaceId,
        string familyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(familyId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, RefreshFamily> families = await LoadFamiliesUnderLockAsync(workspaceId, cancellationToken);
            if (families.TryGetValue(familyId, out RefreshFamily? family))
            {
                family.Status = RefreshFamilyStatus.Revoked;
                family.UpdatedAt = DateTimeOffset.UtcNow;
                await SaveFamiliesUnderLockAsync(workspaceId, families, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RevokeFamiliesForGrantAsync(
        string workspaceId,
        string grantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(grantId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            Dictionary<string, RefreshFamily> families = await LoadFamiliesUnderLockAsync(workspaceId, cancellationToken);
            bool modified = false;
            foreach (var family in families.Values)
            {
                if (string.Equals(family.GrantId, grantId, StringComparison.Ordinal) &&
                    family.Status == RefreshFamilyStatus.Active)
                {
                    family.Status = RefreshFamilyStatus.Revoked;
                    family.UpdatedAt = DateTimeOffset.UtcNow;
                    modified = true;
                }
            }

            if (modified)
            {
                await SaveFamiliesUnderLockAsync(workspaceId, families, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearFamiliesAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth", "refresh-families.json");
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

    private async Task<Dictionary<string, RefreshFamily>> LoadFamiliesUnderLockAsync(
        string workspaceId,
        CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth", "refresh-families.json");
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, RefreshFamily>(StringComparer.Ordinal);
        }

        string json = await File.ReadAllTextAsync(filePath, cancellationToken);
        Dictionary<string, RefreshFamily>? families;
        try
        {
            families = JsonSerializer.Deserialize<Dictionary<string, RefreshFamily>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize refresh families; corrupted JSON fails closed.", ex);
        }

        if (families == null)
        {
            throw new InvalidOperationException("Refresh families dictionary was null; corrupted JSON fails closed.");
        }

        foreach (var family in families.Values)
        {
            if (family.SchemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported refresh family schema version '{family.SchemaVersion}'. Expected 1.");
            }
        }

        return families;
    }

    private async Task SaveFamiliesUnderLockAsync(
        string workspaceId,
        Dictionary<string, RefreshFamily> families,
        CancellationToken cancellationToken)
    {
        string authDir = Path.Combine(_storageDirectory, "workspaces", workspaceId, "auth");
        Directory.CreateDirectory(authDir);

        string filePath = Path.Combine(authDir, "refresh-families.json");
        string tempFile = $"{filePath}.tmp.{Guid.NewGuid():N}";
        string json = JsonSerializer.Serialize(families, JsonOptions);

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
