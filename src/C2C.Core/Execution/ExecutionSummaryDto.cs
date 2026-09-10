using System;
using System.Collections.Generic;

namespace C2C.Core.Execution;

/// <summary>
/// Safe execution summary DTO returned by execution_summary adhering to UC-EXE-02, BR-EXE-002, and BR-EXE-005.
/// </summary>
public sealed class ExecutionSummaryDto
{
    public required string ExecutionId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string TaskId { get; init; }

    public required int Iteration { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset FinishedAt { get; init; }

    public int ExitStatus { get; init; }

    public string? CommandCategory { get; init; }

    public IReadOnlyList<string> VisibleChangedFiles { get; init; } = Array.Empty<string>();

    public TestSummaryDto? TestSummary { get; init; }

    public IReadOnlyList<ArtifactRef> Artifacts { get; init; } = Array.Empty<ArtifactRef>();

    public int SchemaVersion { get; init; } = 1;
}
