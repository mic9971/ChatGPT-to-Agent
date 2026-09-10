using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c unpair' command adhering to UC-CLI-05, BR-AUTH-006, BR-SEC-006, and BR-COM-009.
/// Revokes client authorization grants and pairing associations for the current workspace.
/// </summary>
public sealed class UnpairCommand
{
    private readonly IAuthorizationRevoker _revoker;
    private readonly IAuthorizationStateStore _authStore;
    private readonly IPairingStore _pairingStore;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;

    public UnpairCommand(
        IAuthorizationRevoker revoker,
        IAuthorizationStateStore authStore,
        IPairingStore pairingStore,
        IWorkspaceConfigStore workspaceConfigStore)
    {
        _revoker = revoker ?? throw new ArgumentNullException(nameof(revoker));
        _authStore = authStore ?? throw new ArgumentNullException(nameof(authStore));
        _pairingStore = pairingStore ?? throw new ArgumentNullException(nameof(pairingStore));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
    }

    public async Task<int> ExecuteAsync(
        string? clientId,
        bool all,
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
                        Command = "unpair",
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

            // 2. Handle --all: revoke all clients for current workspace
            if (all)
            {
                var activeGrants = await _authStore.GetActiveGrantsAsync(config.WorkspaceId, cancellationToken);
                var clientIds = activeGrants.Select(g => g.ClientId).Distinct().ToList();

                int totalGrantsRevoked = 0;
                foreach (var cid in clientIds)
                {
                    var res = await _revoker.RevokeClientAsync(config.WorkspaceId, cid, cancellationToken);
                    totalGrantsRevoked += res.GrantsRevoked;
                }

                await _pairingStore.ClearSessionAsync(config.WorkspaceId, cancellationToken);

                var data = new
                {
                    workspaceId = config.WorkspaceId.Value,
                    allRevoked = true,
                    clientsRevoked = clientIds.Count,
                    grantsRevoked = totalGrantsRevoked
                };

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "unpair",
                        Status = "success",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman($"[SUCCESS] All clients unpaired for workspace {config.WorkspaceId.Value}. ({totalGrantsRevoked} grant(s) revoked)");
                }

                return CliExitCode.Success;
            }

            // 3. Handle explicit --client-id
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                var res = await _revoker.RevokeClientAsync(config.WorkspaceId, clientId, cancellationToken);

                var data = new
                {
                    workspaceId = config.WorkspaceId.Value,
                    clientId = clientId,
                    grantsRevoked = res.GrantsRevoked
                };

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "unpair",
                        Status = "success",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    string actionText = res.GrantsRevoked > 0
                        ? $"Client '{clientId}' unpaired ({res.GrantsRevoked} grant(s) revoked)."
                        : $"Client '{clientId}' was already unpaired (0 grants revoked).";
                    CliOutputWriter.WriteHuman($"[SUCCESS] {actionText}");
                }

                return CliExitCode.Success;
            }

            // 4. No client ID and not --all: inspect active candidates
            var grants = await _authStore.GetActiveGrantsAsync(config.WorkspaceId, cancellationToken);
            var candidates = grants.Select(g => g.ClientId).Distinct().ToList();

            if (candidates.Count == 0)
            {
                // Idempotent: nothing to unpair
                await _pairingStore.ClearSessionAsync(config.WorkspaceId, cancellationToken);

                var data = new
                {
                    workspaceId = config.WorkspaceId.Value,
                    grantsRevoked = 0
                };

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "unpair",
                        Status = "success",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman("[SUCCESS] No active client authorizations found. Workspace is already unpaired.");
                }

                return CliExitCode.Success;
            }

            if (candidates.Count == 1)
            {
                // Single candidate: unpair deterministically
                string singleClient = candidates[0];
                var res = await _revoker.RevokeClientAsync(config.WorkspaceId, singleClient, cancellationToken);

                var data = new
                {
                    workspaceId = config.WorkspaceId.Value,
                    clientId = singleClient,
                    grantsRevoked = res.GrantsRevoked
                };

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "unpair",
                        Status = "success",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman($"[SUCCESS] Client '{singleClient}' unpaired ({res.GrantsRevoked} grant(s) revoked).");
                }

                return CliExitCode.Success;
            }

            // Multiple candidates: non-interactive mode requires explicit client id (07-UC-CLI-05-AUTH-COMMANDS.md)
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "unpair",
                    Status = "failed",
                    ErrorCode = CommonErrorCodes.InvalidArgument,
                    ActionRequired = "specify_client_id",
                    Data = new { candidates = candidates.ToArray() },
                    Warnings = ["Multiple clients are authorized. Specify --client-id <id> or --all to unpair."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Multiple clients are authorized for this workspace. Specify --client-id <id> or --all.");
                CliOutputWriter.WriteHuman("Authorized client candidates:");
                foreach (string candidate in candidates)
                {
                    CliOutputWriter.WriteHuman($"  - {candidate}");
                }
            }

            return CliExitCode.InvalidUsageOrConfig;
        }
        catch (OperationCanceledException)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "unpair",
                    Status = "cancelled",
                    ErrorCode = CommonErrorCodes.Timeout,
                    ActionRequired = "retry",
                    Warnings = ["Unpair operation was cancelled or timed out."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Unpair operation cancelled or timed out.");
            }

            return CliExitCode.TimeoutOrCancelled;
        }
    }
}
