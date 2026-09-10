using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Unified workspace access policy gate required by BR-APP-003 and architecture/05-WORKSPACE-SECURITY.md.
/// </summary>
public sealed class WorkspaceAccessPolicy : IWorkspaceAccessPolicy
{
    private readonly ICanonicalPathResolver _canonicalPathResolver;
    private readonly ISensitivePathPolicy _sensitivePathPolicy;
    private readonly Func<IWorkspaceContext, IIgnorePolicy> _ignorePolicyFactory;

    public WorkspaceAccessPolicy(
        ICanonicalPathResolver canonicalPathResolver,
        ISensitivePathPolicy sensitivePathPolicy,
        Func<IWorkspaceContext, IIgnorePolicy>? ignorePolicyFactory = null)
    {
        _canonicalPathResolver = canonicalPathResolver ?? throw new ArgumentNullException(nameof(canonicalPathResolver));
        _sensitivePathPolicy = sensitivePathPolicy ?? throw new ArgumentNullException(nameof(sensitivePathPolicy));
        _ignorePolicyFactory = ignorePolicyFactory ?? (ctx => new IgnorePolicy(ctx.CanonicalRoot));
    }

    public OperationResult<ResolvedWorkspacePath> EvaluatePath(IWorkspaceContext context, string relativePath)
    {
        if (context == null)
        {
            return OperationResult<ResolvedWorkspacePath>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Workspace context cannot be null.",
                nameof(context));
        }

        if (relativePath == null)
        {
            return OperationResult<ResolvedWorkspacePath>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Relative path cannot be null.",
                nameof(relativePath));
        }

        string effectiveRelativePath = string.IsNullOrWhiteSpace(relativePath) || relativePath.Trim() == "/"
            ? "."
            : relativePath;

        // 1. Resolve canonical path with strict containment and symlink checks
        OperationResult<string> resolveResult =
            _canonicalPathResolver.ResolveWorkspaceRelativePath(context.CanonicalRoot, effectiveRelativePath);

        if (resolveResult.IsFailure)
        {
            return OperationResult<ResolvedWorkspacePath>.Failure(resolveResult.Error!);
        }

        string canonicalFullPath = resolveResult.Value!;
        string normalizedRelative = Path.GetRelativePath(context.CanonicalRoot, canonicalFullPath).Replace('\\', '/');

        // 2. Evaluate sensitive path policy
        if (_sensitivePathPolicy.IsSensitive(normalizedRelative))
        {
            return OperationResult<ResolvedWorkspacePath>.Failure(
                CommonErrorCodes.SensitiveContentDenied,
                "Access to sensitive file or directory is denied by policy.",
                relativePath);
        }

        // 3. Evaluate .c2cignore policy
        IIgnorePolicy ignorePolicy = _ignorePolicyFactory(context);
        if (ignorePolicy.IsIgnored(normalizedRelative))
        {
            return OperationResult<ResolvedWorkspacePath>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                "Path is excluded by .c2cignore policy.",
                relativePath);
        }

        bool isDir = Directory.Exists(canonicalFullPath);
        bool exists = isDir || File.Exists(canonicalFullPath);

        return OperationResult<ResolvedWorkspacePath>.Success(new ResolvedWorkspacePath(
            normalizedRelative,
            canonicalFullPath,
            exists,
            isDir));
    }
}
