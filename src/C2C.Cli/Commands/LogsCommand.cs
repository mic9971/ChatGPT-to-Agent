using System;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Diagnostics;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c logs' command adhering to UC-CLI-07, BR-COM-009, and 08-UC-CLI-07-EVIDENCE-AND-LOGS.md.
/// Strictly bounded reader for C2C runtime diagnostic logs with double-defense redaction.
/// Never accepts arbitrary file paths.
/// </summary>
public sealed class LogsCommand
{
    private readonly IDiagnosticLogReader _reader;

    public LogsCommand(IDiagnosticLogReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<int> ExecuteAsync(
        int? tail,
        string? level,
        string? component,
        string? since,
        bool json,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DateTimeOffset? sinceTime = null;
            if (!string.IsNullOrWhiteSpace(since))
            {
                if (DateTimeOffset.TryParse(since, out var parsed))
                {
                    sinceTime = parsed;
                }
                else
                {
                    string errorMsg = $"Invalid date format for --since: '{since}'. Expected ISO-8601 format.";
                    if (json)
                    {
                        CliOutputWriter.WriteJson(new CliEnvelope<object>
                        {
                            Command = "logs",
                            Status = "failed",
                            ErrorCode = CommonErrorCodes.InvalidArgument,
                            ActionRequired = "fix_filter_format",
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

            var request = new DiagnosticLogRequest
            {
                Tail = tail,
                Level = level,
                Component = component,
                Since = sinceTime
            };

            var page = await _reader.ReadAsync(request, cancellationToken);

            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "logs",
                    Status = "success",
                    ActionRequired = "none",
                    Data = new
                    {
                        totalReturned = page.TotalReturned,
                        entries = page.Entries
                    }
                });
            }
            else
            {
                if (page.Entries.Count == 0)
                {
                    CliOutputWriter.WriteHuman("No diagnostic log entries found matching criteria.");
                }
                else
                {
                    foreach (var entry in page.Entries)
                    {
                        string errSuffix = !string.IsNullOrEmpty(entry.ErrorCode) ? $" ({entry.ErrorCode})" : "";
                        CliOutputWriter.WriteHuman(
                            $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level.ToUpperInvariant()}] [{entry.Component}] {entry.Message}{errSuffix}");
                    }
                }
            }

            return CliExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            if (json)
            {
                CliOutputWriter.WriteJson(new CliEnvelope<object>
                {
                    Command = "logs",
                    Status = "cancelled",
                    ErrorCode = CommonErrorCodes.Timeout,
                    ActionRequired = "retry",
                    Warnings = ["Logs operation was cancelled or timed out."]
                });
            }
            else
            {
                CliOutputWriter.WriteError("[ERROR] Logs operation cancelled or timed out.");
            }

            return CliExitCode.TimeoutOrCancelled;
        }
    }
}
