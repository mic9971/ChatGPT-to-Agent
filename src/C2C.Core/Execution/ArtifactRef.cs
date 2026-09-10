using System;

namespace C2C.Core.Execution;

/// <summary>
/// Safe metadata describing an execution artifact adhering to BR-EXE-005 and BR-SEC-009.
/// </summary>
public sealed class ArtifactRef
{
    public required string ArtifactId { get; init; }

    public required string Name { get; init; }

    public required ArtifactClassification Classification { get; init; }

    public long SizeBytes { get; init; }

    public int LineCount { get; init; }

    public string? ReasonCode { get; init; }

    public string? Sha256Fingerprint { get; init; }
}
