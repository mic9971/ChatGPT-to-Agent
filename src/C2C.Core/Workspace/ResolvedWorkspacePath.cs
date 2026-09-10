namespace C2C.Core.Workspace;

/// <summary>
/// Verified canonical path within a workspace adhering to all containment and deny rules.
/// </summary>
public sealed record ResolvedWorkspacePath(
    string RelativePath,
    string CanonicalFullPath,
    bool Exists,
    bool IsDirectory);
