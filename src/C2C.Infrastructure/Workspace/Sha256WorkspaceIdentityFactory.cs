using System.Security.Cryptography;
using System.Text;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements stable, salted workspace identity derivation per UC-WS-01 and BR-COM-009.
/// </summary>
public sealed class Sha256WorkspaceIdentityFactory : IWorkspaceIdentityFactory
{
    private const string DefaultSalt = "c2c_workspace_salt_v1";
    private readonly string _salt;

    public Sha256WorkspaceIdentityFactory(string? salt = null)
    {
        _salt = string.IsNullOrWhiteSpace(salt) ? DefaultSalt : salt;
    }

    public WorkspaceId Create(string canonicalRoot)
    {
        if (string.IsNullOrWhiteSpace(canonicalRoot))
        {
            throw new ArgumentException("Canonical root cannot be null or whitespace.", nameof(canonicalRoot));
        }

        // Normalize path representation for consistent hashing across platform conventions
        string normalized = canonicalRoot.TrimEnd('/', '\\');
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            normalized = normalized.ToLowerInvariant();
        }

        byte[] inputBytes = Encoding.UTF8.GetBytes($"{_salt}:{normalized}");
        byte[] hashBytes = SHA256.HashData(inputBytes);

        // Use 16-byte (32-character hex) prefix for compact, collision-resistant identifier
        string hex = Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
        return new WorkspaceId($"ws_{hex}");
    }
}
