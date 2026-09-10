using System;

namespace C2C.Core.Execution;

/// <summary>
/// Request to read a sanitized execution artifact chunk adhering to UC-EXE-04 and BR-COM-006.
/// </summary>
public sealed class ArtifactChunkRequest
{
    public required string ExecutionId { get; init; }

    public required string ArtifactId { get; init; }

    public string? Cursor { get; init; }

    public int? LimitLines { get; init; }

    public int? LimitBytes { get; init; }
}
