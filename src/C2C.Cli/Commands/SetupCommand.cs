using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c setup' command adhering to UC-CLI-01, BR-COM-007, and BR-COM-010.
/// Delegates configuration to IWorkspaceConfigurator and ensures idempotent execution.
/// </summary>
public sealed class SetupCommand
{
    private readonly IWorkspaceConfigurator _configurator;

    public SetupCommand(IWorkspaceConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    public async Task<int> ExecuteAsync(
        string? path,
        string? label,
        bool force,
        bool json,
        CancellationToken cancellationToken = default)
    {
        string targetPath = string.IsNullOrWhiteSpace(path)
            ? Directory.GetCurrentDirectory()
            : path;

        var request = new WorkspaceConfigureRequest(targetPath, label, force);
        var result = await _configurator.ConfigureAsync(request, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            var config = result.Value;
            var data = new
            {
                workspaceId = config.WorkspaceId.Value,
                label = config.Label,
                createdAt = config.CreatedAt
            };

            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "setup",
                    Status = "success",
                    ActionRequired = "none",
                    Data = data
                });
            }
            else
            {
                CliOutputWriter.WriteHuman($"[SUCCESS] Workspace configured: {config.WorkspaceId.Value}");
                if (!string.IsNullOrEmpty(config.Label))
                {
                    CliOutputWriter.WriteHuman($"  Label: {config.Label}");
                }
                CliOutputWriter.WriteHuman("Next steps: Run 'c2c status' or 'c2c start' to activate the runtime.");
            }

            return CliExitCode.Success;
        }

        string errorCode = result.Error?.Code ?? CommonErrorCodes.InvalidArgument;
        string errorMessage = result.Error?.Message ?? "Workspace configuration failed.";

        int exitCode = errorCode switch
        {
            CommonErrorCodes.Conflict => CliExitCode.Conflict,
            CommonErrorCodes.WorkspacePathDenied => CliExitCode.SecurityDenied,
            CommonErrorCodes.InvalidArgument => CliExitCode.InvalidUsageOrConfig,
            _ => CliExitCode.RuntimeFailure
        };

        string actionRequired = errorCode == CommonErrorCodes.Conflict
            ? "resolve_conflict"
            : "retry";

        if (json)
        {
            CliOutputWriter.WriteJson(new CliEnvelope<object>
            {
                Command = "setup",
                Status = "failed",
                ErrorCode = errorCode,
                ActionRequired = actionRequired,
                Warnings = [errorMessage]
            });
        }
        else
        {
            CliOutputWriter.WriteError($"[ERROR] Setup failed ({errorCode}): {errorMessage}");
        }

        return exitCode;
    }
}
