using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using C2C.Cli.Output;
using C2C.Core.Diagnostics;

namespace C2C.Cli.Commands;

/// <summary>
/// Implements 'c2c doctor' command adhering to UC-CLI-03, BR-COM-007, and BR-OBS-004.
/// Executes deep diagnostic checks without mutating runtime or filesystem state.
/// </summary>
public sealed class DoctorCommand
{
    private readonly IRuntimeDiagnostics _diagnostics;

    public DoctorCommand(IRuntimeDiagnostics diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public async Task<int> ExecuteAsync(bool json, CancellationToken cancellationToken = default)
    {
        var report = await _diagnostics.RunDoctorAsync(cancellationToken: cancellationToken);

        string status;
        string actionRequired;
        int exitCode;

        switch (report.OverallSeverity)
        {
            case DiagnosticSeverity.Pass:
                status = "ready";
                actionRequired = "none";
                exitCode = CliExitCode.Success;
                break;
            case DiagnosticSeverity.Warn:
                status = "degraded";
                actionRequired = "none";
                exitCode = CliExitCode.Success;
                break;
            case DiagnosticSeverity.Blocked:
                status = "conflict";
                actionRequired = "manual_intervention";
                exitCode = CliExitCode.Conflict;
                break;
            default:
                status = "not_ready";
                actionRequired = "run_setup";
                exitCode = CliExitCode.ActionRequired;
                break;
        }

        var warnings = report.Checks
            .Where(c => c.Severity is DiagnosticSeverity.Warn or DiagnosticSeverity.Error or DiagnosticSeverity.Blocked)
            .Select(c => $"{c.CheckName}: {c.Message}")
            .ToList();

        if (json)
        {
            CliOutputWriter.WriteJson(new CliEnvelope<DoctorReport>
            {
                Command = "doctor",
                Status = status,
                ActionRequired = actionRequired,
                Data = report,
                Warnings = warnings
            });
        }
        else
        {
            CliOutputWriter.WriteHuman("=== C2C.NET Doctor Diagnostics ===");
            foreach (var check in report.Checks)
            {
                string tag = check.Severity switch
                {
                    DiagnosticSeverity.Pass => "[PASS]   ",
                    DiagnosticSeverity.Warn => "[WARN]   ",
                    DiagnosticSeverity.Error => "[ERROR]  ",
                    DiagnosticSeverity.Blocked => "[BLOCKED]",
                    _ => "[UNKNOWN]"
                };

                CliOutputWriter.WriteHuman($"{tag} {check.CheckName,-24} : {check.Message}");
                if (!string.IsNullOrEmpty(check.SuggestedRemediation))
                {
                    CliOutputWriter.WriteHuman($"         -> Remediation: {check.SuggestedRemediation}");
                }
            }

            CliOutputWriter.WriteHuman("----------------------------------");
            CliOutputWriter.WriteHuman($"Overall Result: {report.OverallSeverity.ToString().ToUpperInvariant()} (completed in {report.TotalDurationMs}ms)");

            if (actionRequired != "none")
            {
                CliOutputWriter.WriteHuman($"Action Recommended: {actionRequired}");
            }
        }

        return exitCode;
    }
}
