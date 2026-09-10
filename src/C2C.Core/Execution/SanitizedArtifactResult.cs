using System;

namespace C2C.Core.Execution;

/// <summary>
/// Result of artifact sanitization adhering to BR-SEC-009, BR-SEC-012, and BR-EXE-004.
/// </summary>
public sealed class SanitizedArtifactResult
{
    public required ArtifactClassification Classification { get; init; }

    public required string SanitizedContent { get; init; }

    public long SizeBytes { get; init; }

    public int LineCount { get; init; }

    public string? ReasonCode { get; init; }

    public string? Sha256Fingerprint { get; init; }
}
