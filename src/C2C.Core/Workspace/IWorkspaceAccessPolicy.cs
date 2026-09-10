using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Single entry-point access policy pipeline required by BR-APP-003 and BR-SEC-001..004.
/// </summary>
public interface IWorkspaceAccessPolicy
{
    /// <summary>
    /// Evaluates a relative path candidate against canonicalization, symlink containment, sensitive-file deny rules,
    /// and .c2cignore rules.
    /// </summary>
    OperationResult<ResolvedWorkspacePath> EvaluatePath(IWorkspaceContext context, string relativePath);
}
