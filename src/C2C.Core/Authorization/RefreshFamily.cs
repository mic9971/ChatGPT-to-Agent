using System;

namespace C2C.Core.Authorization;

/// <summary>
/// Persisted refresh token family state adhering to 08-UC-AUTH-04-IMPLEMENTATION.md, BR-AUTH-005, and BR-SEC-008.
/// Never stores plaintext refresh tokens; only stores CurrentTokenHash.
/// </summary>
public sealed class RefreshFamily
{
    public required string FamilyId { get; init; }

    public required string GrantId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string ClientId { get; init; }

    public required string Issuer { get; init; }

    public required string Resource { get; init; }

    public int CurrentGeneration { get; set; } = 1;

    public required string CurrentTokenHash { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }

    public RefreshFamilyStatus Status { get; set; } = RefreshFamilyStatus.Active;

    public required DateTimeOffset UpdatedAt { get; set; }

    public int SchemaVersion { get; init; } = 1;
}
