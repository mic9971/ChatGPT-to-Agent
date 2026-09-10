using System;

namespace C2C.Core.Execution;

/// <summary>
/// Safe chunk DTO returned by execution_output adhering to UC-EXE-04, BR-EXE-004, and BR-EXE-005.
/// </summary>
public sealed class ArtifactChunkDto
{
    public required string ExecutionId { get; init; }

    public required string ArtifactId { get; init; }

    public required ArtifactClassification Classification { get; init; }

    public string? Content { get; init; }

    public int StartLine { get; init; }

    public int EndLine { get; init; }

    public bool IsEof { get; init; }

    public string? NextCursor { get; init; }

    public string? ReasonCode { get; init; }
}
