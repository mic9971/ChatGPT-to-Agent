using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Orchestrates public tunnel lifecycle with serialized concurrency and session reuse adhering to UC-TUN-01 and BR-CON-004.
/// </summary>
public sealed class TunnelService : ITunnelService
{
    private readonly ITunnelProvider _provider;
    private readonly ITunnelSessionStore _sessionStore;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly TunnelOptions _options;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    public TunnelService(
        ITunnelProvider provider,
        ITunnelSessionStore sessionStore,
        IWorkspaceContext workspaceContext,
        TunnelOptions? options = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _options = options ?? new TunnelOptions();
    }

    public async Task<OperationResult<TunnelSession>> StartTunnelAsync(
        CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            string workspaceId = _workspaceContext.Id.Value;

            // 1. Check existing session for idempotent reuse (BR-CON-004)
            TunnelSession? existing = await _sessionStore.GetSessionAsync(workspaceId, cancellationToken);
            if (existing != null)
            {
                TunnelHealth health = await _provider.GetHealthAsync(existing, cancellationToken);
                if (health.Status == TunnelStatus.Healthy)
                {
                    return OperationResult<TunnelSession>.Success(existing);
                }

                // Stale or dead session: clear without terminating foreign PID (BR-CON-008)
                await _sessionStore.ClearSessionAsync(workspaceId, cancellationToken);
            }

            // 2. Start new tunnel via configured provider
            var startResult = await _provider.StartAsync(_options.LocalEndpoint, cancellationToken);
            if (startResult.IsFailure)
            {
                return startResult;
            }

            // 3. Atomically persist session
            await _sessionStore.SaveSessionAsync(workspaceId, startResult.Value!, cancellationToken);

            return startResult;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<OperationResult<TunnelSession>> EnsureTunnelAsync(
        CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            string workspaceId = _workspaceContext.Id.Value;

            // 1. Check existing session for healthy reuse (BR-CON-004)
            TunnelSession? existing = await _sessionStore.GetSessionAsync(workspaceId, cancellationToken);
            if (existing != null)
            {
                TunnelHealth health = await _provider.GetHealthAsync(existing, cancellationToken);
                if (health.Status == TunnelStatus.Healthy)
                {
                    return OperationResult<TunnelSession>.Success(existing);
                }

                // Stale or dead session: repair state by stopping owned process and clearing storage (BR-CON-008)
                await _provider.StopAsync(existing, cancellationToken);
                await _sessionStore.ClearSessionAsync(workspaceId, cancellationToken);
            }

            // 2. Start new tunnel via configured provider with bounded retry (max 1 retry per UC-TUN-03)
            var startResult = await _provider.StartAsync(_options.LocalEndpoint, cancellationToken);
            if (startResult.IsFailure)
            {
                // Bounded transient retry
                startResult = await _provider.StartAsync(_options.LocalEndpoint, cancellationToken);
                if (startResult.IsFailure)
                {
                    return startResult;
                }
            }

            // 3. Atomically persist session (BR-CON-002)
            await _sessionStore.SaveSessionAsync(workspaceId, startResult.Value!, cancellationToken);

            return startResult;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<OperationResult<bool>> StopTunnelAsync(
        CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            string workspaceId = _workspaceContext.Id.Value;

            // 1. Load session metadata
            TunnelSession? session = await _sessionStore.GetSessionAsync(workspaceId, cancellationToken);
            if (session == null)
            {
                // Idempotent success when no session exists
                return OperationResult<bool>.Success(true);
            }

            // 2. Stop owned tunnel process (provider enforces ownership validation per BR-CON-008)
            await _provider.StopAsync(session, cancellationToken);

            // 3. Atomically clear session metadata (BR-CON-002)
            await _sessionStore.ClearSessionAsync(workspaceId, cancellationToken);

            return OperationResult<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return OperationResult<bool>.Failure(
                CommonErrorCodes.Timeout,
                "Stop tunnel operation timed out or was cancelled.");
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure(
                CommonErrorCodes.Conflict,
                $"Failed to stop tunnel: {ex.Message}");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<TunnelSession?> GetCurrentSessionAsync(
        CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            string workspaceId = _workspaceContext.Id.Value;
            TunnelSession? existing = await _sessionStore.GetSessionAsync(workspaceId, cancellationToken);
            if (existing == null)
            {
                return null;
            }

            TunnelHealth health = await _provider.GetHealthAsync(existing, cancellationToken);
            if (health.Status != TunnelStatus.Healthy)
            {
                await _sessionStore.ClearSessionAsync(workspaceId, cancellationToken);
                return null;
            }

            return existing;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
