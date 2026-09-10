using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;
using C2C.Core.Diagnostics;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Diagnostics;

/// <summary>
/// Infrastructure implementation of non-mutating runtime status and doctor diagnostics adhering to
/// UC-CLI-03, BR-COM-007, BR-COM-009, and BR-OBS-004.
/// </summary>
public sealed class RuntimeDiagnostics : IRuntimeDiagnostics
{
    private readonly IWorkspaceConfigStore _configStore;
    private readonly IBridgeRuntime _bridgeRuntime;
    private readonly ITunnelService _tunnelService;
    private readonly IPairingStore _pairingStore;

    public RuntimeDiagnostics(
        IWorkspaceConfigStore configStore,
        IBridgeRuntime bridgeRuntime,
        ITunnelService tunnelService,
        IPairingStore pairingStore)
    {
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _bridgeRuntime = bridgeRuntime ?? throw new ArgumentNullException(nameof(bridgeRuntime));
        _tunnelService = tunnelService ?? throw new ArgumentNullException(nameof(tunnelService));
        _pairingStore = pairingStore ?? throw new ArgumentNullException(nameof(pairingStore));
    }

    public async Task<RuntimeStatusSnapshot> GetStatusAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _configStore.LoadAsync(cancellationToken);
        var bridgeHealth = await _bridgeRuntime.CheckHealthAsync(cancellationToken);
        var tunnelSession = await _tunnelService.GetCurrentSessionAsync(cancellationToken);

        string? workspaceId = config?.WorkspaceId.Value;
        PairingSession? pairingSession = null;
        if (!string.IsNullOrEmpty(workspaceId))
        {
            pairingSession = await _pairingStore.GetSessionAsync(workspaceId, cancellationToken);
        }

        bool isBridgeRunning = bridgeHealth.Status == BridgeStatus.Healthy || bridgeHealth.Status == BridgeStatus.Conflict;
        bool isBridgeHealthy = bridgeHealth.Status == BridgeStatus.Healthy;
        bool isTunnelActive = tunnelSession != null && tunnelSession.PublicUrl != null;
        bool isPairingActive = pairingSession != null && pairingSession.Status == PairingSessionStatus.Active;

        return new RuntimeStatusSnapshot
        {
            IsWorkspaceConfigured = config != null,
            WorkspaceId = workspaceId,
            WorkspaceRoot = config?.CanonicalRoot,
            IsBridgeRunning = isBridgeRunning,
            IsBridgeHealthy = isBridgeHealthy,
            LocalEndpoint = bridgeHealth.Endpoint?.ToString(),
            IsTunnelActive = isTunnelActive,
            TunnelPublicUrl = tunnelSession?.PublicUrl?.ToString(),
            IsPairingActive = isPairingActive,
            PairingStatus = pairingSession?.Status.ToString().ToLowerInvariant(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task<DoctorReport> RunDoctorAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var checks = new List<DiagnosticCheckResult>();

        // 1. runtime.dotnet
        checks.Add(CheckDotnetRuntime());

        // 2. workspace.config
        var config = await _configStore.LoadAsync(cancellationToken);
        if (config == null)
        {
            checks.Add(new DiagnosticCheckResult
            {
                CheckName = "workspace.config",
                Severity = DiagnosticSeverity.Error,
                Message = "No workspace is currently configured.",
                SuggestedRemediation = "Run 'c2c setup' to configure the workspace."
            });
        }
        else
        {
            checks.Add(new DiagnosticCheckResult
            {
                CheckName = "workspace.config",
                Severity = DiagnosticSeverity.Pass,
                Message = $"Workspace bound: {config.WorkspaceId.Value}"
            });

            // 3. workspace.permissions
            checks.Add(CheckWorkspacePermissions(config.CanonicalRoot));
        }

        // 4. git.binary
        checks.Add(CheckGitBinary());

        // 5. bridge.health
        var bridgeHealth = await _bridgeRuntime.CheckHealthAsync(cancellationToken);
        switch (bridgeHealth.Status)
        {
            case BridgeStatus.Healthy:
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "bridge.health",
                    Severity = DiagnosticSeverity.Pass,
                    Message = $"Bridge listener healthy at {bridgeHealth.Endpoint}"
                });
                break;
            case BridgeStatus.Stopped:
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "bridge.health",
                    Severity = DiagnosticSeverity.Warn,
                    Message = "Bridge listener is stopped.",
                    SuggestedRemediation = "Run 'c2c start' to launch the runtime."
                });
                break;
            case BridgeStatus.Conflict:
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "bridge.health",
                    Severity = DiagnosticSeverity.Blocked,
                    Message = "Bridge port is in use by an unowned or conflicting process.",
                    SuggestedRemediation = "Resolve conflicting process on port 5000 or check c2c status."
                });
                break;
            default:
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "bridge.health",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Bridge health check failed: {bridgeHealth.Message}",
                    SuggestedRemediation = "Inspect logs or restart runtime."
                });
                break;
        }

        // 6. tunnel.state
        var tunnelSession = await _tunnelService.GetCurrentSessionAsync(cancellationToken);
        if (tunnelSession != null && tunnelSession.PublicUrl != null)
        {
            checks.Add(new DiagnosticCheckResult
            {
                CheckName = "tunnel.state",
                Severity = DiagnosticSeverity.Pass,
                Message = $"Tunnel active: {tunnelSession.PublicUrl}"
            });
        }
        else
        {
            checks.Add(new DiagnosticCheckResult
            {
                CheckName = "tunnel.state",
                Severity = DiagnosticSeverity.Warn,
                Message = "Tunnel is inactive.",
                SuggestedRemediation = "Run 'c2c start --tunnel' to establish public connectivity."
            });
        }

        // 7. auth.pairing
        if (config != null)
        {
            var session = await _pairingStore.GetSessionAsync(config.WorkspaceId.Value, cancellationToken);
            if (session != null)
            {
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "auth.pairing",
                    Severity = session.Status == PairingSessionStatus.Active ? DiagnosticSeverity.Pass : DiagnosticSeverity.Warn,
                    Message = $"Pairing session state: {session.Status}"
                });
            }
            else
            {
                checks.Add(new DiagnosticCheckResult
                {
                    CheckName = "auth.pairing",
                    Severity = DiagnosticSeverity.Warn,
                    Message = "No pairing session found.",
                    SuggestedRemediation = "Run 'c2c pair' to generate a pairing code for remote client approval."
                });
            }
        }

        sw.Stop();

        var overallSeverity = DiagnosticSeverity.Pass;
        if (checks.Any(c => c.Severity == DiagnosticSeverity.Blocked))
        {
            overallSeverity = DiagnosticSeverity.Blocked;
        }
        else if (checks.Any(c => c.Severity == DiagnosticSeverity.Error))
        {
            overallSeverity = DiagnosticSeverity.Error;
        }
        else if (checks.Any(c => c.Severity == DiagnosticSeverity.Warn))
        {
            overallSeverity = DiagnosticSeverity.Warn;
        }

        return new DoctorReport
        {
            OverallSeverity = overallSeverity,
            Checks = checks,
            TotalDurationMs = sw.ElapsedMilliseconds,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static DiagnosticCheckResult CheckDotnetRuntime()
    {
        var version = Environment.Version;
        if (version.Major >= 8)
        {
            return new DiagnosticCheckResult
            {
                CheckName = "runtime.dotnet",
                Severity = DiagnosticSeverity.Pass,
                Message = $".NET runtime {version} is supported."
            };
        }

        return new DiagnosticCheckResult
        {
            CheckName = "runtime.dotnet",
            Severity = DiagnosticSeverity.Error,
            Message = $".NET runtime {version} is unsupported. .NET 8.0 or higher is required.",
            SuggestedRemediation = "Install .NET 8 SDK or runtime."
        };
    }

    private static DiagnosticCheckResult CheckWorkspacePermissions(string rootPath)
    {
        try
        {
            if (!Directory.Exists(rootPath))
            {
                return new DiagnosticCheckResult
                {
                    CheckName = "workspace.permissions",
                    Severity = DiagnosticSeverity.Error,
                    Message = "Configured workspace directory does not exist.",
                    SuggestedRemediation = "Ensure workspace folder exists or run 'c2c setup' with a valid path."
                };
            }

            // Probe directory listing
            _ = Directory.EnumerateFileSystemEntries(rootPath).Take(1).ToList();

            return new DiagnosticCheckResult
            {
                CheckName = "workspace.permissions",
                Severity = DiagnosticSeverity.Pass,
                Message = "Workspace directory is accessible and readable."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new DiagnosticCheckResult
            {
                CheckName = "workspace.permissions",
                Severity = DiagnosticSeverity.Blocked,
                Message = "Access denied to configured workspace directory.",
                SuggestedRemediation = "Check filesystem permissions."
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticCheckResult
            {
                CheckName = "workspace.permissions",
                Severity = DiagnosticSeverity.Error,
                Message = $"Failed to access workspace directory: {ex.Message}"
            };
        }
    }

    private static DiagnosticCheckResult CheckGitBinary()
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (proc.Start())
            {
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);
                if (proc.ExitCode == 0)
                {
                    return new DiagnosticCheckResult
                    {
                        CheckName = "git.binary",
                        Severity = DiagnosticSeverity.Pass,
                        Message = output.Trim()
                    };
                }
            }

            return new DiagnosticCheckResult
            {
                CheckName = "git.binary",
                Severity = DiagnosticSeverity.Warn,
                Message = "Git executable exited with non-zero code.",
                SuggestedRemediation = "Verify git installation."
            };
        }
        catch
        {
            return new DiagnosticCheckResult
            {
                CheckName = "git.binary",
                Severity = DiagnosticSeverity.Warn,
                Message = "Git executable not found in PATH. Git evidence tools will be unavailable.",
                SuggestedRemediation = "Install git and ensure it is available in system PATH."
            };
        }
    }
}
