using System;

namespace C2C.Core.Authorization;

/// <summary>
/// Ephemeral result returned to the local caller upon pairing session creation adhering to BR-AUTH-007 and BR-COM-009.
/// Contains plaintext DisplayCode returned once; never logged or written to persistent disk.
/// </summary>
public sealed class PairingCreateResult
{
    public required string PairingSessionId { get; init; }

    public required string DisplayCode { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public Uri? PairingUri { get; init; }
}
