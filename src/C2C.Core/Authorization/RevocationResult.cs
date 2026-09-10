namespace C2C.Core.Authorization;

/// <summary>
/// Result of an unpair or revocation action adhering to UC-AUTH-05, BR-AUTH-006, and BR-COM-009.
/// Never contains plaintext tokens.
/// </summary>
public sealed class RevocationResult
{
    public required string WorkspaceId { get; init; }

    public string? ClientId { get; init; }

    public string? GrantId { get; init; }

    public int GrantsRevoked { get; init; }

    public bool IsSuccess => true;
}
