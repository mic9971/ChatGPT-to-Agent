namespace C2C.Core.Authorization;

/// <summary>
/// Status of a refresh token family adhering to 08-UC-AUTH-04-IMPLEMENTATION.md and BR-CON-005.
/// </summary>
public enum RefreshFamilyStatus
{
    Active = 1,
    Revoked = 2,
    ReplayDetected = 3
}
