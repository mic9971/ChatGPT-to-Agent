using System;

namespace C2C.Core.Authorization;

/// <summary>
/// Persisted pairing session metadata adhering to UC-AUTH-01, 04-DETAILED-AUTH-DESIGN.md, and BR-SEC-008.
/// Plaintext pairing code is NEVER stored; only CodeHash is persisted.
/// </summary>
public sealed class PairingSession
{
    public required string PairingSessionId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string CodeHash { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public int AttemptsRemaining { get; set; } = 3;

    public PairingSessionStatus Status { get; set; } = PairingSessionStatus.Active;

    public string? ApprovedClientId { get; set; }

    public int SchemaVersion { get; init; } = 1;
}
