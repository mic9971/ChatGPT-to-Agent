using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c record' command adhering to UC-CLI-07, BR-EXE-006, and 08-UC-CLI-07-EVIDENCE-AND-LOGS.md.
/// Exposes local execution recording via IExecutionRecorder without echoing raw artifact bodies.
/// </summary>
public sealed class RecordCommand
{
    private const long MaxInputFileSize = 1024 * 1024; // 1 MB limit per 08-UC-CLI-07

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IExecutionRecorder _recorder;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;

    public RecordCommand(
        IExecutionRecorder recorder,
        IWorkspaceConfigStore workspaceConfigStore)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _workspaceConfigStore = workspaceConfigStore ?? throw new ArgumentNullException(nameof(workspaceConfigStore));
    }

    public async Task<int> ExecuteAsync(
        string? inputFilePath,
        string? taskId,
        int iteration,
        int exitStatus,
        string? category,
        string? command,
        string? idempotencyKey,
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
                        Command = "record",
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

            // 2. Prepare ExecutionRecordRequest
            ExecutionRecordRequest request;
            if (!string.IsNullOrWhiteSpace(inputFilePath))
            {
                if (!File.Exists(inputFilePath))
                {
                    string errorMsg = $"Input file '{inputFilePath}' does not exist.";
                    if (json)
                    {
                        CliOutputWriter.WriteJson(new CliEnvelope<object>
                        {
                            Command = "record",
                            Status = "failed",
                            ErrorCode = CommonErrorCodes.InvalidArgument,
                            ActionRequired = "check_path",
                            Warnings = [errorMsg]
                        });
                    }
                    else
                    {
                        CliOutputWriter.WriteError($"[ERROR] {errorMsg}");
                    }

                    return CliExitCode.InvalidUsageOrConfig;
                }

                FileInfo fileInfo = new(inputFilePath);
                if (fileInfo.Length > MaxInputFileSize)
                {
                    string errorMsg = $"Input file exceeds maximum allowed size of {MaxInputFileSize} bytes (1MB).";
                    if (json)
                    {
                        CliOutputWriter.WriteJson(new CliEnvelope<object>
                        {
                            Command = "record",
                            Status = "failed",
                            ErrorCode = CommonErrorCodes.OutputLimitExceeded,
                            ActionRequired = "reduce_payload_size",
                            Warnings = [errorMsg]
                        });
                    }
                    else
                    {
                        CliOutputWriter.WriteError($"[ERROR] {errorMsg}");
                    }

                    return CliExitCode.InvalidUsageOrConfig;
                }

                string content = await File.ReadAllTextAsync(inputFilePath, cancellationToken);
                try
                {
                    var parsed = JsonSerializer.Deserialize<RawRecordInput>(content, JsonOptions);
                    if (parsed == null)
                    {
                        throw new JsonException("Deserialized request was null.");
                    }

                    request = new ExecutionRecordRequest
                    {
                        WorkspaceId = config.WorkspaceId.Value,
                        TaskId = !string.IsNullOrWhiteSpace(parsed.TaskId) ? parsed.TaskId : (!string.IsNullOrWhiteSpace(taskId) ? taskId : "task-default"),
                        Iteration = parsed.Iteration ?? (iteration > 0 ? iteration : 1),
                        IdempotencyKey = !string.IsNullOrWhiteSpace(parsed.IdempotencyKey) ? parsed.IdempotencyKey : (!string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : Guid.NewGuid().ToString("N")),
                        ExecutorName = parsed.ExecutorName,
                        ExecutorVersion = parsed.ExecutorVersion,
                        StartedAt = parsed.StartedAt.HasValue ? parsed.StartedAt.Value : DateTimeOffset.UtcNow.AddSeconds(-1),
                        FinishedAt = parsed.FinishedAt.HasValue ? parsed.FinishedAt.Value : DateTimeOffset.UtcNow,
                        ExitStatus = parsed.ExitStatus ?? exitStatus,
                        CommandCategory = parsed.CommandCategory ?? (category ?? "general"),
                        CommandText = parsed.CommandText ?? (command ?? ""),
                        ChangedFiles = parsed.ChangedFiles,
                        TestSummary = parsed.TestSummary,
                        Artifacts = parsed.Artifacts
                    };
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Invalid structured record JSON: {ex.Message}";
                    if (json)
                    {
                        CliOutputWriter.WriteJson(new CliEnvelope<object>
                        {
                            Command = "record",
                            Status = "failed",
                            ErrorCode = CommonErrorCodes.InvalidArgument,
                            ActionRequired = "fix_json_schema",
                            Warnings = [errorMsg]
                        });
                    }
                    else
                    {
                        CliOutputWriter.WriteError($"[ERROR] {errorMsg}");
                    }

                    return CliExitCode.InvalidUsageOrConfig;
                }
            }
            else
            {
                request = new ExecutionRecordRequest
                {
                    WorkspaceId = config.WorkspaceId.Value,
                    TaskId = !string.IsNullOrWhiteSpace(taskId) ? taskId : "task-default",
                    Iteration = iteration > 0 ? iteration : 1,
                    IdempotencyKey = !string.IsNullOrWhiteSpace(idempotencyKey) ? idempotencyKey : Guid.NewGuid().ToString("N"),
                    StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    FinishedAt = DateTimeOffset.UtcNow,
                    ExitStatus = exitStatus,
                    CommandCategory = category ?? "general",
                    CommandText = command ?? ""
                };
            }

            // 3. Delegate to IExecutionRecorder
            var result = await _recorder.RecordAsync(request, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var summary = result.Value;
                var data = new
                {
                    executionId = summary.ExecutionId,
                    recorded = true,
                    idempotentReplay = false
                };

                if (json)
                {
                    CliOutputWriter.WriteJson(new CliEnvelope<object>
                    {
                        Command = "record",
                        Status = "success",
                        ActionRequired = "none",
                        Data = data
                    });
                }
                else
                {
                    CliOutputWriter.WriteHuman($"[SUCCESS] Execution evidence recorded: {summary.ExecutionId}");
                    CliOutputWriter.WriteHuman($"  Task ID: {summary.TaskId}");
                    CliOutputWriter.WriteHuman($"  Exit Status: {summary.ExitStatus}");
                }

                return CliExitCode.Success;
            }

            string errorCode = result.Error?.Code ?? CommonErrorCodes.InvalidArgument;
            string errorMessage = result.Error?.Message ?? "Execution recording failed.";

            int exitCode = errorCode switch
            {
                CommonErrorCodes.InvalidArgument => CliExitCode.InvalidUsageOrConfig,
                _ => CliExitCode.RuntimeFailure
            };

            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "record",
                    Status = "failed",
                    ErrorCode = errorCode,
                    ActionRequired = "retry",
                    Warnings = [errorMessage]
                });
            }
            else
            {
                CliOutputWriter.WriteError($"[ERROR] Recording failed ({errorCode}): {errorMessage}");
            }

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "record",
                    Status = "cancelled",
                    ErrorCode = CommonErrorCodes.Timeout,
                    ActionRequired = "retry",
                    Warnings = ["Record operation was cancelled or timed out."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Record operation cancelled or timed out.");
            }

            return CliExitCode.TimeoutOrCancelled;
        }
    }

    private sealed class RawRecordInput
    {
        public string? WorkspaceId { get; set; }
        public string? TaskId { get; set; }
        public int? Iteration { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? ExecutorName { get; set; }
        public string? ExecutorVersion { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public int? ExitStatus { get; set; }
        public string? CommandCategory { get; set; }
        public string? CommandText { get; set; }
        public System.Collections.Generic.IReadOnlyList<string>? ChangedFiles { get; set; }
        public TestSummaryDto? TestSummary { get; set; }
        public System.Collections.Generic.IReadOnlyList<ArtifactRecordRequest>? Artifacts { get; set; }
    }
}
