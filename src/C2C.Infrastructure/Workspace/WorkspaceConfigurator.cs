using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements workspace configuration and binding per UC-WS-01, BR-COM-001, and BR-CON-002.
/// </summary>
public sealed class WorkspaceConfigurator : IWorkspaceConfigurator
{
    private readonly ICanonicalPathResolver _pathResolver;
    private readonly IWorkspaceIdentityFactory _identityFactory;
    private readonly IWorkspaceConfigStore _configStore;
    private readonly TimeProvider _timeProvider;

    public WorkspaceConfigurator(
        ICanonicalPathResolver pathResolver,
        IWorkspaceIdentityFactory identityFactory,
        IWorkspaceConfigStore configStore,
        TimeProvider? timeProvider = null)
    {
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _identityFactory = identityFactory ?? throw new ArgumentNullException(nameof(identityFactory));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<OperationResult<WorkspaceConfig>> ConfigureAsync(
        WorkspaceConfigureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return OperationResult<WorkspaceConfig>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Workspace path cannot be null or whitespace.",
                nameof(request.Path));
        }

        // 1. Resolve canonical workspace root
        OperationResult<string> rootResult = _pathResolver.ResolveCanonicalRoot(request.Path);
        if (rootResult.IsFailure)
        {
            return OperationResult<WorkspaceConfig>.Failure(rootResult.Error!);
        }

        string canonicalRoot = rootResult.Value!;

        // 2. Check existing configuration
        WorkspaceConfig? existing = await _configStore.LoadAsync(cancellationToken);
        StringComparison comp = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (existing != null)
        {
            // Same canonical root -> idempotent success (BR-CON-002)
            if (string.Equals(existing.CanonicalRoot, canonicalRoot, comp))
            {
                return OperationResult<WorkspaceConfig>.Success(existing);
            }

            // Different root without explicit force -> reject conflict (BR-COM-001: one bridge = one workspace)
            if (!request.Force)
            {
                return OperationResult<WorkspaceConfig>.Failure(
                    CommonErrorCodes.Conflict,
                    $"A different workspace is already bound ('{existing.WorkspaceId}'). Use Force to rebind.",
                    canonicalRoot);
            }
        }

        // 3. Derive stable salted workspace ID
        WorkspaceId workspaceId = _identityFactory.Create(canonicalRoot);

        // 4. Create and persist configuration atomically
        WorkspaceConfig newConfig = new()
        {
            SchemaVersion = 1,
            WorkspaceId = workspaceId,
            CanonicalRoot = canonicalRoot,
            CreatedAt = _timeProvider.GetUtcNow(),
            Label = request.Label
        };

        await _configStore.SaveAsync(newConfig, cancellationToken);

        return OperationResult<WorkspaceConfig>.Success(newConfig);
    }
}
