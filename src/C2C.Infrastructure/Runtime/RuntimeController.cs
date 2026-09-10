using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Runtime;
using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Runtime;

/// <summary>
/// Orchestrates owned host runtime lifecycle adhering to 05-UC-CLI-02-RUNTIME-LIFECYCLE.md, BR-CON-008, and BR-SEC-005.
/// </summary>
public sealed class RuntimeController : IRuntimeController
{
    private readonly IRuntimeOwnershipStore _ownershipStore;
    private readonly IOwnedProcessValidator _processValidator;
    private readonly IBridgeRuntime _bridgeRuntime;
    private readonly ITunnelService _tunnelService;
    private readonly IRuntimeHostLauncher _hostLauncher;
    private readonly TunnelOptions _options;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    public RuntimeController(
        IRuntimeOwnershipStore ownershipStore,
        IOwnedProcessValidator processValidator,
        IBridgeRuntime bridgeRuntime,
        ITunnelService tunnelService,
        IRuntimeHostLauncher? hostLauncher = null,
        TunnelOptions? options = null)
    {
        _ownershipStore = ownershipStore ?? throw new ArgumentNullException(nameof(ownershipStore));
        _processValidator = processValidator ?? throw new ArgumentNullException(nameof(processValidator));
        _bridgeRuntime = bridgeRuntime ?? throw new ArgumentNullException(nameof(bridgeRuntime));
        _tunnelService = tunnelService ?? throw new ArgumentNullException(nameof(tunnelService));
        _hostLauncher = hostLauncher ?? new DefaultRuntimeHostLauncher();
        _options = options ?? new TunnelOptions();
    }

    public async Task<OperationResult<RuntimeStartResult>> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            Uri localEndpoint = !string.IsNullOrWhiteSpace(request.LocalEndpoint)
                ? new Uri(request.LocalEndpoint)
                : _options.LocalEndpoint;

            // BR-SEC-005: Bridge listener binds loopback only in V1. Fail closed on wildcard/public.
            if (!localEndpoint.IsLoopback)
            {
                return OperationResult<RuntimeStartResult>.Failure(
                    CommonErrorCodes.SecurityViolation,
                    $"BR-SEC-005 violation: Bridge endpoint '{localEndpoint}' must be a loopback address.");
            }

            // 1. Check existing ownership record
            var existingRecord = await _ownershipStore.LoadAsync(request.WorkspaceId, cancellationToken);
            if (existingRecord != null)
            {
                var ownership = _processValidator.ValidateOwnership(
                    existingRecord.ProcessId,
                    existingRecord.ProcessStartTime,
                    existingRecord.RuntimeInstanceId);

                if (ownership == ProcessOwnershipStatus.OwnedAndActive)
                {
                    var health = await _bridgeRuntime.CheckHealthAsync(cancellationToken);
                    if (health.Status == BridgeStatus.Healthy)
                    {
                        // Idempotent: already running and healthy
                        TunnelSession? tunnel = null;
                        string tunnelStatus = "stopped";
                        if (request.EnsureTunnel)
                        {
                            var tunnelResult = await _tunnelService.EnsureTunnelAsync(cancellationToken);
                            if (tunnelResult.IsSuccess)
                            {
                                tunnel = tunnelResult.Value;
                                tunnelStatus = "healthy";
                            }
                            else
                            {
                                tunnelStatus = "failed";
                            }
                        }

                        return OperationResult<RuntimeStartResult>.Success(new RuntimeStartResult
                        {
                            RuntimeInstanceId = existingRecord.RuntimeInstanceId,
                            BridgeStatus = "healthy",
                            LocalEndpoint = existingRecord.LocalEndpoint,
                            TunnelStatus = tunnelStatus,
                            PublicUrl = tunnel?.PublicUrl,
                            AlreadyRunning = true
                        });
                    }
                }
                else if (ownership == ProcessOwnershipStatus.ForeignOrReused)
                {
                    return OperationResult<RuntimeStartResult>.Failure(
                        CommonErrorCodes.Conflict,
                        "Process ID recorded in ownership record is occupied by a foreign or reused process.");
                }
                else
                {
                    // Stale record: clean metadata safely
                    await _ownershipStore.ClearAsync(request.WorkspaceId, cancellationToken);
                }
            }

            // 2. Check for port conflicts with foreign processes
            var preCheckHealth = await _bridgeRuntime.CheckHealthAsync(cancellationToken);
            if (preCheckHealth.Status == BridgeStatus.Healthy || preCheckHealth.Status == BridgeStatus.Conflict)
            {
                return OperationResult<RuntimeStartResult>.Failure(
                    CommonErrorCodes.Conflict,
                    $"Port {localEndpoint.Port} is already in use by another process.");
            }

            // 3. Launch host
            LaunchedProcessInfo launched;
            try
            {
                launched = await _hostLauncher.LaunchAsync(request.WorkspaceId, localEndpoint, cancellationToken);
            }
            catch (Exception ex)
            {
                return OperationResult<RuntimeStartResult>.Failure(
                    CommonErrorCodes.RuntimeStartFailed,
                    $"Failed to start host process: {ex.Message}");
            }

            // 4. Bounded health polling (up to 5s)
            bool healthy = false;
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    var health = await _bridgeRuntime.CheckHealthAsync(linkedCts.Token);
                    if (health.Status == BridgeStatus.Healthy)
                    {
                        healthy = true;
                        break;
                    }

                    await Task.Delay(150, linkedCts.Token);
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                healthy = false;
            }

            if (!healthy)
            {
                // Startup failed: terminate ONLY the newly launched process
                TryTerminateProcess(launched.ProcessId);

                return OperationResult<RuntimeStartResult>.Failure(
                    CommonErrorCodes.NotReady,
                    "Host process failed to report healthy listener status within startup deadline.");
            }

            // 5. Persist ownership atomically
            var record = new RuntimeInstanceRecord
            {
                WorkspaceId = request.WorkspaceId,
                RuntimeInstanceId = Guid.NewGuid().ToString("N"),
                ProcessId = launched.ProcessId,
                ProcessStartTime = launched.StartTime,
                LocalEndpoint = localEndpoint.ToString(),
                ExecutablePath = launched.ExecutablePath,
                StartedAt = DateTimeOffset.UtcNow,
                SchemaVersion = 1
            };

            await _ownershipStore.SaveAsync(request.WorkspaceId, record, cancellationToken);

            // 6. Ensure tunnel if requested
            TunnelSession? tunnelSession = null;
            string tunnelState = "stopped";
            if (request.EnsureTunnel)
            {
                var tr = await _tunnelService.EnsureTunnelAsync(cancellationToken);
                if (tr.IsSuccess)
                {
                    tunnelSession = tr.Value;
                    tunnelState = "healthy";
                }
                else
                {
                    tunnelState = "failed";
                }
            }

            return OperationResult<RuntimeStartResult>.Success(new RuntimeStartResult
            {
                RuntimeInstanceId = record.RuntimeInstanceId,
                BridgeStatus = "healthy",
                LocalEndpoint = record.LocalEndpoint,
                TunnelStatus = tunnelState,
                PublicUrl = tunnelSession?.PublicUrl,
                AlreadyRunning = false
            });
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<OperationResult<bool>> StopAsync(
        RuntimeStopRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            var record = await _ownershipStore.LoadAsync(request.WorkspaceId, cancellationToken);
            if (record == null)
            {
                // Idempotent: already stopped
                return OperationResult<bool>.Success(true);
            }

            // Validate process identity before touching anything
            var ownership = _processValidator.ValidateOwnership(
                record.ProcessId,
                record.ProcessStartTime,
                record.RuntimeInstanceId);

            if (ownership == ProcessOwnershipStatus.ForeignOrReused)
            {
                // Never terminate foreign or reused process (BR-CON-008, BR-COM-011)
                return OperationResult<bool>.Failure(
                    CommonErrorCodes.Conflict,
                    "Process identity mismatch: PID in ownership record is foreign or has been reused.");
            }

            // Stop tunnel first
            await _tunnelService.StopTunnelAsync(cancellationToken);

            // If process is active and owned, stop it gracefully or forcefully
            if (ownership == ProcessOwnershipStatus.OwnedAndActive)
            {
                await StopOwnedProcessAsync(record.ProcessId, cancellationToken);
            }

            // Remove ownership record atomically
            await _ownershipStore.ClearAsync(request.WorkspaceId, cancellationToken);

            return OperationResult<bool>.Success(true);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private static async Task StopOwnedProcessAsync(int processId, CancellationToken cancellationToken)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            if (proc.HasExited)
            {
                return;
            }

            // Cooperative shutdown signal
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                kill(proc.Id, 15); // SIGTERM
            }
            else if (OperatingSystem.IsWindows())
            {
                proc.CloseMainWindow();
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await proc.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!proc.HasExited)
                {
                    proc.Kill(entireProcessTree: true);
                }
            }
        }
        catch (ArgumentException)
        {
            // Process already exited
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
    }

    private static void TryTerminateProcess(int processId)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            if (!proc.HasExited)
            {
                proc.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cleanup of startup-failed child process
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);
}
