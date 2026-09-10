using System;
using System.Collections.Generic;

namespace C2C.Core.Diagnostics;

/// <summary>
/// Full diagnostic doctor report across runtime, workspace, bridge, tunnel, and auth.
/// </summary>
public sealed record DoctorReport
{
    public required DiagnosticSeverity OverallSeverity { get; init; }

    public required IReadOnlyList<DiagnosticCheckResult> Checks { get; init; }

    public long TotalDurationMs { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
