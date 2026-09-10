namespace C2C.Core.Workspace;

/// <summary>
/// Enforces built-in sensitive path deny rules (e.g. .env, private keys, credentials) per BR-SEC-003.
/// </summary>
public interface ISensitivePathPolicy
{
    /// <summary>
    /// Checks if a normalized relative path or filename is considered sensitive and must be denied.
    /// </summary>
    bool IsSensitive(string relativePath);
}
