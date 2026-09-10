using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c ensure' command adhering to UC-CLI-04 and 06-UC-CLI-04-ENSURE.md.
/// Primary agent-readiness convergence command.
/// </summary>
public sealed class EnsureCommand
{
    private readonly IRuntimeEnsurer _runtimeEnsurer;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;

    public EnsureCommand(
        IRuntimeEnsurer runtimeEnsurer,
        IWorkspaceConfigStore workspaceConfigStore)
    {
        _runtimeEnsurer = runtimeEnsurer ?? throw new ArgumentNullException(nameof(runtimeEnsurer));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
    }

    public async Task<int> ExecuteAsync(
        bool json,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Workspace binding check
            WorkspaceConfig? config = await _workspaceConfigStore.LoadAsync(cancellationToken);
            if (config == null || string.IsNullOrWhiteSpace(config.WorkspaceId))
            {
                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "ensure",
                        Status = "not_configured",
                        ErrorCode = CommonErrorCodes.WorkspaceNotConfigured,
                        ActionRequired = "run_setup",
                        Warnings = ["Workspace is not configured. Run 'c2c setup' to configure workspace."]
                    });
                }
                else
                {
                    CliOutputWriter.WriteError("[ERROR] Workspace not configured. Run 'c2c setup' first.");
                }

                return CliExitCode.ActionRequired;
            }

            // 2. Invoke runtime ensurer
            RuntimeEnsureResult result = await _runtimeEnsurer.EnsureAsync(cancellationToken);

        string authStatus = result.Pairing switch
        {
            "paired" => "paired",
            _ => "pairing_required"
        };

        var data = new
        {
            bridge = result.Bridge,
            tunnel = result.Tunnel,
            authorization = authStatus,
            publicEndpoint = result.PublicUrl?.ToString(),
            localEndpoint = result.LocalEndpoint?.ToString()
        };

        // 3. Status mapping
        switch (result.Status)
        {
            case RuntimeStatus.Ready:
                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "ensure",
                        Status = "ready",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman("[READY] Runtime bridge and tunnel are ready.");
                    if (result.PublicUrl != null)
                    {
                        CliOutputWriter.WriteHuman($"  Public URL: {result.PublicUrl}");
                    }
                }
                return CliExitCode.Success;

            case RuntimeStatus.NeedPairing:
                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "ensure",
                        Status = "pairing_required",
                        ErrorCode = CommonErrorCodes.NotReady,
                        ActionRequired = "run_pair",
                        Data = data,
                        Warnings = [result.Message ?? "Client pairing required."]
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman("[ACTION REQUIRED] Pairing is required.");
                    CliOutputWriter.WriteHuman("Run 'c2c pair' to generate a pairing code for ChatGPT.");
                }
                return CliExitCode.ActionRequired;

            case RuntimeStatus.Degraded:
                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "ensure",
                        Status = "degraded",
                        ActionRequired = "none",
                        Data = data,
                        Warnings = [result.Message ?? "Runtime is operating in degraded mode."]
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman("[DEGRADED] Runtime is operating in degraded mode.");
                }
                return CliExitCode.Success;

            default:
                string errorCode = result.ErrorCode ?? CommonErrorCodes.NotReady;
                int exitCode = errorCode == CommonErrorCodes.Conflict
                    ? CliExitCode.Conflict
                    : CliExitCode.RuntimeFailure;

                string status = errorCode == CommonErrorCodes.Conflict ? "conflict" : "failed";
                string actionRequired = errorCode == CommonErrorCodes.Conflict
                    ? "resolve_port_conflict"
                    : "run_doctor";

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "ensure",
                        Status = status,
                        ErrorCode = errorCode,
                        ActionRequired = actionRequired,
                        Data = data,
                        Warnings = [result.Message ?? "Runtime convergence failed."]
                    });
                }
                else
                {
                    CliOutputWriter.WriteError($"[ERROR] Ensure failed ({errorCode}): {result.Message}");
                }
                return exitCode;
        }
        }
        catch (OperationCanceledException)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "ensure",
                    Status = "cancelled",
                    ErrorCode = CommonErrorCodes.Timeout,
                    ActionRequired = "retry",
                    Warnings = ["Ensure operation was cancelled or timed out."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Ensure operation cancelled or timed out.");
            }

            return CliExitCode.TimeoutOrCancelled;
        }
    }
}
