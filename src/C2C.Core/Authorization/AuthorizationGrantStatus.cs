namespace C2C.Core.Authorization;

/// <summary>
/// Status of an authorization grant adhering to 04-DETAILED-AUTH-DESIGN.md.
/// </summary>
public enum AuthorizationGrantStatus
{
    Active = 1,
    Revoked = 2
}
