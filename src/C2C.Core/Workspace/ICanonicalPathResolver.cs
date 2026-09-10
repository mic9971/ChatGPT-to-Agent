using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Resolves and canonicalizes filesystem paths, strictly enforcing containment and symlink boundaries.
/// </summary>
public interface ICanonicalPathResolver
{
    /// <summary>
    /// Validates and resolves a workspace root directory to its canonical absolute path.
    /// Fails closed if the path does not exist or is not a directory.
    /// </summary>
    OperationResult<string> ResolveCanonicalRoot(string rawPath);

    /// <summary>
    /// Resolves a workspace-relative candidate path against the canonical root, ensuring it does not
    /// escape through traversal ('..'), absolute notation, or symlinks/junctions.
    /// </summary>
    OperationResult<string> ResolveWorkspaceRelativePath(string canonicalRoot, string relativePath);
}
