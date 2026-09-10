using System;
using System.Collections.Generic;

namespace C2C.Core.Execution;

/// <summary>
/// Inbound execution record request adhering to UC-EXE-01.
/// </summary>
public sealed class ExecutionRecordRequest
{
    public required string WorkspaceId { get; init; }

    public required string TaskId { get; init; }

    public required int Iteration { get; init; }

    public required string IdempotencyKey { get; init; }

    public string? ExecutorName { get; init; }

    public string? ExecutorVersion { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset FinishedAt { get; init; }

    public int ExitStatus { get; init; }

    public string? CommandCategory { get; init; }

    public string? CommandText { get; init; }

    public IReadOnlyList<string>? ChangedFiles { get; init; }

    public TestSummaryDto? TestSummary { get; init; }

    public IReadOnlyList<ArtifactRecordRequest>? Artifacts { get; init; }
}
