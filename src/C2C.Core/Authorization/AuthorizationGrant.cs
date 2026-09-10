using System;
using System.Collections.Generic;

namespace C2C.Core.Authorization;

/// <summary>
/// Persisted authorization grant adhering to 04-DETAILED-AUTH-DESIGN.md, BR-SEC-006, and BR-AUTH-004.
/// Represents an approved pairing session converted to an active OAuth grant.
/// </summary>
public sealed class AuthorizationGrant
{
    public required string GrantId { get; init; }

    public required string WorkspaceId { get; init; }

    public required string ClientId { get; init; }

    public required string Issuer { get; init; }

    public required string Resource { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }

    public AuthorizationGrantStatus Status { get; set; } = AuthorizationGrantStatus.Active;

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RefreshFamilyId { get; set; }

    public int SchemaVersion { get; init; } = 1;
}
