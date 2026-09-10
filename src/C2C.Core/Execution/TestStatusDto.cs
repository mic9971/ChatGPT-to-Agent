using System;

namespace C2C.Core.Execution;

/// <summary>
/// Normalized test status response adhering to UC-EXE-03, BR-EXE-007, and BR-COM-010.
/// </summary>
public sealed class TestStatusDto
{
    public required string ExecutionId { get; init; }

    public required TestStatus Status { get; init; }

    public TestSummaryDto? Summary { get; init; }

    public long? DurationMs { get; init; }

    public int SchemaVersion { get; init; } = 1;
}
