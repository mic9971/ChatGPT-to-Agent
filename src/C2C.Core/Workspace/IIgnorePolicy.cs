namespace C2C.Core.Workspace;

/// <summary>
/// Enforces .c2cignore additive exclusions per BR-SEC-004.
/// </summary>
public interface IIgnorePolicy
{
    /// <summary>
    /// Checks if a relative path is ignored by the workspace .c2cignore configuration.
    /// </summary>
    bool IsIgnored(string relativePath);
}
