using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c pair' command adhering to UC-CLI-05, BR-AUTH-007, BR-SEC-011, and BR-COM-009.
/// Creates a short-lived local approval bootstrap for pairing a remote MCP client.
/// </summary>
public sealed class PairCommand
{
    private readonly IPairingService _pairingService;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly ITunnelService _tunnelService;
    private readonly TunnelOptions _options;

    public PairCommand(
        IPairingService pairingService,
        IWorkspaceConfigStore workspaceConfigStore,
        ITunnelService tunnelService,
        TunnelOptions? options = null)
    {
        _pairingService = pairingService ?? throw new ArgumentNullException(nameof(pairingService));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
        _tunnelService = tunnelService ?? throw new ArgumentNullException(nameof(tunnelService));
        _options = options ?? new TunnelOptions();
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
                        Command = "pair",
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

            // 2. Create pairing session
            PairingCreateResult result = await _pairingService.CreateSessionAsync(config.WorkspaceId, cancellationToken);

            // 3. Resolve pairing URL
            var tunnelSession = await _tunnelService.GetCurrentSessionAsync(cancellationToken);
            string pairingUrl = tunnelSession?.PublicUrl != null
                ? $"{tunnelSession.PublicUrl.ToString().TrimEnd('/')}/connect/authorize"
                : $"{_options.LocalEndpoint.ToString().TrimEnd('/')}/connect/authorize";

            int expiresInSeconds = Math.Max(0, (int)(result.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds);

            var data = new
            {
                pairingSessionId = result.PairingSessionId,
                pairingCode = result.DisplayCode,
                pairingUrl = pairingUrl,
                expiresInSeconds = expiresInSeconds,
                expiresAt = result.ExpiresAt
            };

            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "pair",
                    Status = "success",
                    ActionRequired = "manual_intervention",
                    Data = data,
                    Warnings = ["pairingCode is short-lived sensitive output; do not log it"]
                });
            }
            else
            {
                CliOutputWriter.WriteHuman("[SUCCESS] Pairing session created.");
                CliOutputWriter.WriteHuman($"  Pairing Code : {result.DisplayCode}");
                CliOutputWriter.WriteHuman($"  Authorize URL: {pairingUrl}");
                CliOutputWriter.WriteHuman($"  Expires In   : {expiresInSeconds}s ({result.ExpiresAt:u})");
                CliOutputWriter.WriteHuman("");
                CliOutputWriter.WriteHuman("Enter the pairing code in the client authorization prompt to complete pairing.");
            }

            return CliExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "pair",
                    Status = "cancelled",
                    ErrorCode = CommonErrorCodes.Timeout,
                    ActionRequired = "retry",
                    Warnings = ["Pair operation was cancelled or timed out."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Pair operation cancelled or timed out.");
            }

            return CliExitCode.TimeoutOrCancelled;
        }
    }
}
