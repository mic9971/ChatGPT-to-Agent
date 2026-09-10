using System;
using System.Collections.Generic;

namespace C2C.Core.Execution;

/// <summary>
/// Immutable execution record entity adhering to 11-DATA-MODEL.md, BR-EXE-001, BR-CON-001, and BR-COM-010.
/// </summary>
public sealed class ExecutionRecord
{
    public required string ExecutionId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string TaskId { get; init; }

    public required int Iteration { get; init; }

    public required string IdempotencyKey { get; init; }

    public string? ExecutorName { get; init; }

    public string? ExecutorVersion { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public required DateTimeOffset FinishedAt { get; init; }

    public required int ExitStatus { get; init; }

    public string? CommandCategory { get; init; }

    public string? CommandText { get; init; }

    public required IReadOnlyList<string> ChangedFiles { get; init; }

    public TestSummaryDto? TestSummary { get; init; }

    public required IReadOnlyList<ArtifactRef> ArtifactRefs { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public int SchemaVersion { get; init; } = 1;

    public required string PayloadFingerprint { get; init; }
}
