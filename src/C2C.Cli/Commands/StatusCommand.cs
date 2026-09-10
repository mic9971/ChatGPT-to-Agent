using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Diagnostics;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c status' command adhering to UC-CLI-03, BR-COM-007, and BR-OBS-004.
/// Read-only inspection of workspace, bridge, tunnel, and pairing states.
/// </summary>
public sealed class StatusCommand
{
    private readonly IRuntimeDiagnostics _diagnostics;

    public StatusCommand(IRuntimeDiagnostics diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public async Task<int> ExecuteAsync(bool json, CancellationToken cancellationToken = default)
    {
        var snapshot = await _diagnostics.GetStatusAsync(cancellationToken: cancellationToken);

        string status;
        string actionRequired;
        int exitCode;

        if (!snapshot.IsWorkspaceConfigured)
        {
            status = "not_configured";
            actionRequired = "run_setup";
            exitCode = CliExitCode.ActionRequired;
        }
        else if (snapshot.IsBridgeHealthy)
        {
            status = "ready";
            actionRequired = "none";
            exitCode = CliExitCode.Success;
        }
        else if (snapshot.IsPairingActive)
        {
            status = "pairing_required";
            actionRequired = "run_pair";
            exitCode = CliExitCode.Success;
        }
        else
        {
            status = "not_ready";
            actionRequired = "none";
            exitCode = CliExitCode.Success;
        }

        if (json)
        {
            CliOutputWriter.WriteJson(new CliEnvelope<RuntimeStatusSnapshot>
            {
                Command = "status",
                Status = status,
                ActionRequired = actionRequired,
                Data = snapshot
            });
        }
        else
        {
            CliOutputWriter.WriteHuman("=== C2C.NET Runtime Status ===");
            CliOutputWriter.WriteHuman($"Workspace Configured : {(snapshot.IsWorkspaceConfigured ? $"Yes ({snapshot.WorkspaceId})" : "No")}");
            CliOutputWriter.WriteHuman($"Bridge Running       : {(snapshot.IsBridgeRunning ? "Yes" : "No")}");
            CliOutputWriter.WriteHuman($"Bridge Healthy       : {(snapshot.IsBridgeHealthy ? "Yes" : "No")}");
            if (!string.IsNullOrEmpty(snapshot.LocalEndpoint))
            {
                CliOutputWriter.WriteHuman($"Local Endpoint       : {snapshot.LocalEndpoint}");
            }
            CliOutputWriter.WriteHuman($"Tunnel Active        : {(snapshot.IsTunnelActive ? "Yes" : "No")}");
            if (!string.IsNullOrEmpty(snapshot.TunnelPublicUrl))
            {
                CliOutputWriter.WriteHuman($"Tunnel Public URL    : {snapshot.TunnelPublicUrl}");
            }
            CliOutputWriter.WriteHuman($"Pairing Active       : {(snapshot.IsPairingActive ? $"Yes ({snapshot.PairingStatus})" : "No")}");
            CliOutputWriter.WriteHuman($"Overall Status       : {status.ToUpperInvariant()}");

            if (actionRequired != "none")
            {
                CliOutputWriter.WriteHuman($"Action Recommended   : {actionRequired}");
            }
        }

        return exitCode;
    }
}
