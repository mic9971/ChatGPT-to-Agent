using System;

namespace C2C.Core.Execution;

/// <summary>
/// Inbound artifact payload for execution recording.
/// </summary>
public sealed class ArtifactRecordRequest
{
    public required string ArtifactId { get; init; }

    public required string Name { get; init; }

    public string? ArtifactType { get; init; }

    public required string Content { get; init; }
}
