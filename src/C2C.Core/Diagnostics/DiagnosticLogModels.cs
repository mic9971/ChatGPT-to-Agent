using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Diagnostics;

/// <summary>
/// Structured diagnostic log entry adhering to UC-CLI-07, BR-COM-009, and 08-UC-CLI-07-EVIDENCE-AND-LOGS.md.
/// Guaranteed to contain redacted content only.
/// </summary>
public sealed record DiagnosticLogEntry
{
    public required DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public required string Level { get; init; }

    public required string Component { get; init; }

    public required string Message { get; init; }

    public string? ErrorCode { get; init; }
}

/// <summary>
/// Query filter request for bounded diagnostic log reading.
/// </summary>
public sealed record DiagnosticLogRequest
{
    public int? Tail { get; init; } = 50;

    public string? Level { get; init; }

    public string? Component { get; init; }

    public DateTimeOffset? Since { get; init; }
}

/// <summary>
/// Bounded page of returned diagnostic log entries.
/// </summary>
public sealed record DiagnosticLogPage
{
    public required IReadOnlyList<DiagnosticLogEntry> Entries { get; init; }

    public int TotalReturned => Entries.Count;
}

/// <summary>
/// Contract for reading C2C-owned diagnostic logs adhering to UC-CLI-07.
/// Never allows reading arbitrary external filesystem paths.
/// </summary>
public interface IDiagnosticLogReader
{
    Task<DiagnosticLogPage> ReadAsync(
        DiagnosticLogRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contract for writing C2C diagnostic log entries with pre-persistence redaction.
/// </summary>
public interface IDiagnosticLogSink
{
    Task WriteAsync(
        DiagnosticLogEntry entry,
        CancellationToken cancellationToken = default);
}
