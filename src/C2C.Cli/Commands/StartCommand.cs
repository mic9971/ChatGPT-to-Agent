using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Runtime;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c start' command adhering to UC-CLI-02 and 05-UC-CLI-02-RUNTIME-LIFECYCLE.md.
/// </summary>
public sealed class StartCommand
{
    private readonly IRuntimeController _runtimeController;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;

    public StartCommand(
        IRuntimeController runtimeController,
        IWorkspaceConfigStore workspaceConfigStore)
    {
        _runtimeController = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
    }

    public async Task<int> ExecuteAsync(
        bool tunnel,
        bool force,
        bool json,
        CancellationToken cancellationToken = default)
    {
        WorkspaceConfig? config = await _workspaceConfigStore.LoadAsync(cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.WorkspaceId))
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "start",
                    Status = "not_configured",
                    ErrorCode = CommonErrorCodes.WorkspaceNotConfigured,
                    ActionRequired = "run_setup",
                    Warnings = ["Workspace is not configured. Run 'c2c setup' first."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Workspace not configured. Run 'c2c setup' first.");
            }

            return CliExitCode.ActionRequired;
        }

        var request = new RuntimeStartRequest
        {
            WorkspaceId = config.WorkspaceId,
            EnsureTunnel = tunnel,
            Force = force
        };

        var result = await _runtimeController.StartAsync(request, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var res = result.Value;
            bool isDegraded = tunnel && res.TunnelStatus == "failed";
            string status = isDegraded ? "degraded" : "ready";
            string actionRequired = isDegraded ? "run_doctor" : "none";

            var data = new
            {
                runtimeInstanceId = res.RuntimeInstanceId,
                bridge = res.BridgeStatus,
                localEndpoint = res.LocalEndpoint,
                tunnel = res.TunnelStatus,
                publicEndpoint = res.PublicUrl?.ToString(),
                alreadyRunning = res.AlreadyRunning
            };

            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "start",
                    Status = status,
                    ActionRequired = actionRequired,
                    Data = data
                });
            }
            else
            {
                string stateText = res.AlreadyRunning ? "already running" : "started successfully";
                CliOutputWriter.WriteHuman($"[SUCCESS] Runtime {stateText}.");
                CliOutputWriter.WriteHuman($"  Instance ID: {res.RuntimeInstanceId}");
                CliOutputWriter.WriteHuman($"  Bridge Endpoint: {res.LocalEndpoint}");
                if (!string.IsNullOrEmpty(res.TunnelStatus))
                {
                    CliOutputWriter.WriteHuman($"  Tunnel Status: {res.TunnelStatus}");
                }
                if (res.PublicUrl != null)
                {
                    CliOutputWriter.WriteHuman($"  Public Endpoint: {res.PublicUrl}");
                }
            }

            return CliExitCode.Success;
        }

        string errorCode = result.Error?.Code ?? CommonErrorCodes.RuntimeStartFailed;
        string errorMessage = result.Error?.Message ?? "Failed to start runtime.";

        int exitCode = errorCode switch
        {
            CommonErrorCodes.Conflict => CliExitCode.Conflict,
            CommonErrorCodes.SecurityViolation => CliExitCode.SecurityDenied,
            _ => CliExitCode.RuntimeFailure
        };

        string failureStatus = errorCode switch
        {
            CommonErrorCodes.Conflict => "conflict",
            CommonErrorCodes.SecurityViolation => "denied",
            _ => "failed"
        };

        string failureAction = errorCode switch
        {
            CommonErrorCodes.Conflict => "resolve_port_conflict",
            _ => "retry"
        };

        if (json)
        {
            CliOutputWriter.WriteJson(new CliEnvelope<object>
            {
                Command = "start",
                Status = failureStatus,
                ErrorCode = errorCode,
                ActionRequired = failureAction,
                Warnings = [errorMessage]
            });
        }
        else
        {
            CliOutputWriter.WriteError($"[ERROR] Runtime start failed ({errorCode}): {errorMessage}");
        }

        return exitCode;
    }
}
