using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using C2C.Cli.Commands;

namespace C2C.Cli.Composition;

/// <summary>
/// CLI application command tree builder adhering to UC-CLI-01, UC-CLI-03, and 02-CLI-ARCHITECTURE.md.
/// </summary>
public static class CliApplication
{
    public static RootCommand BuildRootCommand(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var root = new RootCommand("C2C.NET CLI - Local runtime and MCP bridge manager");

        var jsonOption = new Option<bool>("--json")
        {
            Description = "Emit deterministic machine-readable JSON output to stdout",
            Recursive = true
        };
        root.Options.Add(jsonOption);

        // 1. setup
        var setupCommand = new Command("setup", "Configure and bind workspace paths (UC-CLI-01)");
        var pathOption = new Option<string?>("--path")
        {
            Description = "Target workspace directory path (defaults to current working directory)"
        };
        var labelOption = new Option<string?>("--label")
        {
            Description = "Human-readable label for the workspace"
        };
        var forceOption = new Option<bool>("--force")
        {
            Description = "Force reconfiguration if workspace is already bound"
        };

        setupCommand.Options.Add(pathOption);
        setupCommand.Options.Add(labelOption);
        setupCommand.Options.Add(forceOption);

        setupCommand.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathOption);
            var label = parseResult.GetValue(labelOption);
            var force = parseResult.GetValue(forceOption);
            var json = parseResult.GetValue(jsonOption);

            var cmd = services.GetRequiredService<SetupCommand>();
            return await cmd.ExecuteAsync(path, label, force, json, ct);
        });
        root.Subcommands.Add(setupCommand);

        // 2. status
        var statusCommand = new Command("status", "Inspect runtime, bridge, tunnel, and pairing status (UC-CLI-03)");
        statusCommand.SetAction(async (parseResult, ct) =>
        {
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<StatusCommand>();
            return await cmd.ExecuteAsync(json, ct);
        });
        root.Subcommands.Add(statusCommand);

        // 3. doctor
        var doctorCommand = new Command("doctor", "Run deep non-mutating diagnostics across runtime prerequisites (UC-CLI-03)");
        doctorCommand.SetAction(async (parseResult, ct) =>
        {
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<DoctorCommand>();
            return await cmd.ExecuteAsync(json, ct);
        });
        root.Subcommands.Add(doctorCommand);

        // 4. start
        var startCommand = new Command("start", "Start the local runtime bridge and optional tunnel (UC-CLI-02)");
        var tunnelOption = new Option<bool>("--tunnel")
        {
            Description = "Establish a public tunnel alongside the local bridge"
        };
        startCommand.Options.Add(tunnelOption);
        startCommand.Options.Add(forceOption);
        startCommand.SetAction(async (parseResult, ct) =>
        {
            var tunnel = parseResult.GetValue(tunnelOption);
            var force = parseResult.GetValue(forceOption);
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<StartCommand>();
            return await cmd.ExecuteAsync(tunnel, force, json, ct);
        });
        root.Subcommands.Add(startCommand);

        // 5. stop
        var stopCommand = new Command("stop", "Stop the local runtime bridge and associated tunnel (UC-CLI-02)");
        stopCommand.Options.Add(forceOption);
        stopCommand.SetAction(async (parseResult, ct) =>
        {
            var force = parseResult.GetValue(forceOption);
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<StopCommand>();
            return await cmd.ExecuteAsync(force, json, ct);
        });
        root.Subcommands.Add(stopCommand);

        // 6. ensure
        var ensureCommand = new Command("ensure", "Converge runtime to ready state for execution agents (UC-CLI-04)");
        ensureCommand.SetAction(async (parseResult, ct) =>
        {
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<EnsureCommand>();
            return await cmd.ExecuteAsync(json, ct);
        });
        root.Subcommands.Add(ensureCommand);

        // 7. pair
        var pairCommand = new Command("pair", "Create a short-lived local pairing session for client approval (UC-CLI-05)");
        pairCommand.SetAction(async (parseResult, ct) =>
        {
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<PairCommand>();
            return await cmd.ExecuteAsync(json, ct);
        });
        root.Subcommands.Add(pairCommand);

        // 8. unpair
        var unpairCommand = new Command("unpair", "Revoke client authorization and pairing associations (UC-CLI-05)");
        var clientIdOption = new Option<string?>("--client-id")
        {
            Description = "Specific client ID to unpair"
        };
        var allOption = new Option<bool>("--all")
        {
            Description = "Revoke all authorized clients for the current workspace"
        };
        unpairCommand.Options.Add(clientIdOption);
        unpairCommand.Options.Add(allOption);
        unpairCommand.SetAction(async (parseResult, ct) =>
        {
            var clientId = parseResult.GetValue(clientIdOption);
            var all = parseResult.GetValue(allOption);
            var json = parseResult.GetValue(jsonOption);
            var cmd = services.GetRequiredService<UnpairCommand>();
            return await cmd.ExecuteAsync(clientId, all, json, ct);
        });
        root.Subcommands.Add(unpairCommand);

        // 9. record
        var recordCommand = new Command("record", "Record execution evidence and test outcome (UC-CLI-07)");
        var inputOption = new Option<string?>("--input")
        {
            Description = "Path to JSON file containing structured ExecutionRecordRequest (max 1MB)"
        };
        var taskIdOption = new Option<string?>("--task-id")
        {
            Description = "Task identifier"
        };
        var iterationOption = new Option<int?>("--iteration")
        {
            Description = "Iteration count (defaults to 1)"
        };
        var exitStatusOption = new Option<int?>("--exit-status")
        {
            Description = "Process exit status code (defaults to 0)"
        };
        var categoryOption = new Option<string?>("--category")
        {
            Description = "Execution category (e.g. build, test, run)"
        };
        var commandTextOption = new Option<string?>("--command")
        {
            Description = "Executed command string"
        };
        var idempotencyKeyOption = new Option<string?>("--idempotency-key")
        {
            Description = "Unique idempotency key"
        };

        recordCommand.Options.Add(inputOption);
        recordCommand.Options.Add(taskIdOption);
        recordCommand.Options.Add(iterationOption);
        recordCommand.Options.Add(exitStatusOption);
        recordCommand.Options.Add(categoryOption);
        recordCommand.Options.Add(commandTextOption);
        recordCommand.Options.Add(idempotencyKeyOption);

        recordCommand.SetAction(async (parseResult, ct) =>
        {
            var input = parseResult.GetValue(inputOption);
            var taskId = parseResult.GetValue(taskIdOption);
            var iteration = parseResult.GetValue(iterationOption) ?? 1;
            var exitStatus = parseResult.GetValue(exitStatusOption) ?? 0;
            var category = parseResult.GetValue(categoryOption);
            var commandText = parseResult.GetValue(commandTextOption);
            var idempotencyKey = parseResult.GetValue(idempotencyKeyOption);
            var json = parseResult.GetValue(jsonOption);

            var cmd = services.GetRequiredService<RecordCommand>();
            return await cmd.ExecuteAsync(input, taskId, iteration, exitStatus, category, commandText, idempotencyKey, json, ct);
        });
        root.Subcommands.Add(recordCommand);

        // 10. logs
        var logsCommand = new Command("logs", "Read bounded C2C diagnostic logs (UC-CLI-07)");
        logsCommand.Aliases.Add("log");
        var tailOption = new Option<int?>("--tail")
        {
            Description = "Number of recent log entries to return (default 50, max 500)"
        };
        var levelOption = new Option<string?>("--level")
        {
            Description = "Filter by log level (info, warn, error, debug)"
        };
        var componentOption = new Option<string?>("--component")
        {
            Description = "Filter by component name"
        };
        var sinceOption = new Option<string?>("--since")
        {
            Description = "Filter log entries since timestamp (ISO-8601 format)"
        };

        logsCommand.Options.Add(tailOption);
        logsCommand.Options.Add(levelOption);
        logsCommand.Options.Add(componentOption);
        logsCommand.Options.Add(sinceOption);

        logsCommand.SetAction(async (parseResult, ct) =>
        {
            var tail = parseResult.GetValue(tailOption);
            var level = parseResult.GetValue(levelOption);
            var component = parseResult.GetValue(componentOption);
            var since = parseResult.GetValue(sinceOption);
            var json = parseResult.GetValue(jsonOption);

            var cmd = services.GetRequiredService<LogsCommand>();
            return await cmd.ExecuteAsync(tail, level, component, since, json, ct);
        });
        root.Subcommands.Add(logsCommand);

        return root;
    }
}
