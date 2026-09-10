namespace C2C.Core.Authorization;

/// <summary>
/// Enforces single-use redemption of authorization codes in database-free degraded mode
/// adhering to BR-AUTH-005, BR-CON-005, and 07-UC-AUTH-03-IMPLEMENTATION.md.
/// </summary>
public interface ICodeRedemptionTracker
{
    /// <summary>
    /// Attempts to mark a unique authorization code identifier as redeemed.
    /// Returns true if redemption succeeded; false if the code was already redeemed (replay detected).
    /// </summary>
    bool TryRedeemCode(string codeId);
}
