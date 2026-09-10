namespace C2C.Core.Diagnostics;

/// <summary>
/// Result of an individual diagnostic check adhering to 04-UC-CLI-01-03-FOUNDATION.md.
/// </summary>
public sealed record DiagnosticCheckResult
{
    public required string CheckName { get; init; }

    public required DiagnosticSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? SuggestedRemediation { get; init; }

    public long DurationMs { get; init; }
}
