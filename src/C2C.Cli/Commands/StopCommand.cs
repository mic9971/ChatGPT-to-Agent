using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Runtime;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c stop' command adhering to UC-CLI-02 and 05-UC-CLI-02-RUNTIME-LIFECYCLE.md.
/// </summary>
public sealed class StopCommand
{
    private readonly IRuntimeController _runtimeController;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;

    public StopCommand(
        IRuntimeController runtimeController,
        IWorkspaceConfigStore workspaceConfigStore)
    {
        _runtimeController = runtimeController ?? throw new ArgumentNullException(nameof(runtimeController));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
    }

    public async Task<int> ExecuteAsync(
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
                    Command = "stop",
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

        var request = new RuntimeStopRequest
        {
            WorkspaceId = config.WorkspaceId,
            Force = force
        };

        var result = await _runtimeController.StopAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "stop",
                    Status = "success",
                    ActionRequired = "none",
                    Data = new
                    {
                        stopped = true
                    }
                });
            }
            else
            {
                CliOutputWriter.WriteHuman("[SUCCESS] Runtime and associated tunnels stopped.");
            }

            return CliExitCode.Success;
        }

        string errorCode = result.Error?.Code ?? CommonErrorCodes.RuntimeStopFailed;
        string errorMessage = result.Error?.Message ?? "Failed to stop runtime.";

        int exitCode = errorCode switch
        {
            CommonErrorCodes.Conflict => CliExitCode.Conflict,
            _ => CliExitCode.RuntimeFailure
        };

        string failureStatus = errorCode switch
        {
            CommonErrorCodes.Conflict => "conflict",
            _ => "failed"
        };

        string failureAction = errorCode switch
        {
            CommonErrorCodes.Conflict => "manual_intervention",
            _ => "retry"
        };

        if (json)
        {
            CliOutputWriter.WriteJson(new CliEnvelope<object>
            {
                Command = "stop",
                Status = failureStatus,
                ErrorCode = errorCode,
                ActionRequired = failureAction,
                Warnings = [errorMessage]
            });
        }
        else
        {
            CliOutputWriter.WriteError($"[ERROR] Runtime stop failed ({errorCode}): {errorMessage}");
        }

        return exitCode;
    }
}
