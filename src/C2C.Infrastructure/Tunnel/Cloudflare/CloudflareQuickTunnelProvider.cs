using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Tunnel.Cloudflare;

/// <summary>
/// Implements Cloudflare Quick Tunnel provider adhering to UC-TUN-01, BR-SEC-005, and BR-CON-008.
/// </summary>
public sealed class CloudflareQuickTunnelProvider : ITunnelProvider
{
    private readonly IOwnedProcessRunner _processRunner;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly TimeProvider _timeProvider;
    private readonly TunnelOptions _options;
    private readonly object _lock = new();

    private IOwnedProcess? _activeProcess;

    public CloudflareQuickTunnelProvider(
        IOwnedProcessRunner processRunner,
        IWorkspaceContext workspaceContext,
        TimeProvider? timeProvider = null,
        TunnelOptions? options = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = options ?? new TunnelOptions();
    }

    public string ProviderName => "cloudflare-quick";

    public async Task<OperationResult<TunnelSession>> StartAsync(
        Uri localEndpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(localEndpoint);

        // 1. Enforce BR-SEC-005: local endpoint must be loopback only
        if (!IsLoopback(localEndpoint))
        {
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.InvalidArgument,
                $"BR-SEC-005 violation: Local endpoint '{localEndpoint}' is not a loopback address.",
                nameof(localEndpoint));
        }

        // 2. Resolve cloudflared binary
        string? binaryPath = CloudflaredBinaryResolver.Resolve(_options);
        if (string.IsNullOrWhiteSpace(binaryPath))
        {
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.TunnelStartFailed,
                "cloudflared executable was not found. Please ensure cloudflared is installed or set TunnelOptions.BinaryPath.");
        }

        // 3. Build typed arguments without shell interpolation
        string formattedEndpoint = localEndpoint.ToString().TrimEnd('/');
        string[] arguments =
        [
            "tunnel",
            "--url",
            formattedEndpoint,
            "--no-autoupdate"
        ];

        string ownershipMarker = Guid.NewGuid().ToString("N");
        IOwnedProcess process;

        try
        {
            process = _processRunner.Start(binaryPath, arguments, Directory.GetCurrentDirectory(), ownershipMarker);
        }
        catch (Exception ex)
        {
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.TunnelStartFailed,
                $"Failed to spawn cloudflared process: {ex.Message}");
        }

        // 4. Stream discovery with timeout
        using var timeoutCts = new CancellationTokenSource(_options.StartupTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var urlDiscoveryTcs = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Monitor stdout and stderr concurrently
        _ = ReadStreamForUrlAsync(process.StandardOutput, urlDiscoveryTcs, linkedCts.Token);
        _ = ReadStreamForUrlAsync(process.StandardError, urlDiscoveryTcs, linkedCts.Token);

        // Also monitor premature process exit
        _ = MonitorProcessExitAsync(process, urlDiscoveryTcs, linkedCts.Token);

        try
        {
            Uri publicUrl = await urlDiscoveryTcs.Task;

            lock (_lock)
            {
                _activeProcess?.Dispose();
                _activeProcess = process;
            }

            TunnelSession session = new()
            {
                WorkspaceId = _workspaceContext.Id.Value,
                Provider = ProviderName,
                PublicUrl = publicUrl,
                LocalEndpoint = localEndpoint,
                ProcessId = process.Id,
                StartedAt = _timeProvider.GetUtcNow(),
                OwnershipMarker = ownershipMarker,
                SchemaVersion = 1
            };

            return OperationResult<TunnelSession>.Success(session);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            process.Kill();
            process.Dispose();
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.Timeout,
                $"Public tunnel startup timed out after {_options.StartupTimeoutMs} ms.");
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            process.Dispose();
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Tunnel startup was cancelled.");
        }
        catch (Exception ex)
        {
            process.Kill();
            process.Dispose();
            return OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.TunnelStartFailed,
                $"Tunnel startup failed: {ex.Message}");
        }
    }

    public Task<TunnelHealth> GetHealthAsync(TunnelSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        DateTimeOffset now = _timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (_activeProcess == null || _activeProcess.HasExited)
            {
                return Task.FromResult(TunnelHealth.Failed(
                    "PROCESS_EXITED",
                    "Tunnel process is not running.",
                    now));
            }

            if (session.ProcessId.HasValue && _activeProcess.Id != session.ProcessId.Value)
            {
                return Task.FromResult(TunnelHealth.Failed(
                    "PID_MISMATCH",
                    "Active process ID does not match session process ID.",
                    now));
            }

            return Task.FromResult(TunnelHealth.Healthy(now));
        }
    }

    public async Task StopAsync(TunnelSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        IOwnedProcess? processToStop;
        lock (_lock)
        {
            processToStop = _activeProcess;
            _activeProcess = null;
        }

        if (processToStop != null)
        {
            try
            {
                await processToStop.StopAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            finally
            {
                processToStop.Dispose();
            }
        }
    }

    private static async Task ReadStreamForUrlAsync(
        StreamReader reader,
        TaskCompletionSource<Uri> tcs,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (line != null)
                {
                    Uri? discovered = CloudflareUrlParser.TryParseUrl(line);
                    if (discovered != null)
                    {
                        tcs.TrySetResult(discovered);
                        return;
                    }
                }
            }
        }
        catch
        {
            // Stream ended or cancelled
        }
    }

    private static async Task MonitorProcessExitAsync(
        IOwnedProcess process,
        TaskCompletionSource<Uri> tcs,
        CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetException(new InvalidOperationException(
                    $"cloudflared process exited prematurely with code {process.ExitCode}."));
            }
        }
        catch
        {
            // Cancelled or completed
        }
    }

    private static bool IsLoopback(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return true;
        }

        string host = uri.Host;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
               string.Equals(host, "::1", StringComparison.Ordinal);
    }
}
