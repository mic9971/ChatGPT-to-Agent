namespace C2C.Core.Authorization;

/// <summary>
/// Result of pairing code verification and consumption adhering to BR-AUTH-008 and BR-SEC-011.
/// </summary>
public sealed class PairingValidationResult
{
    public bool IsSuccess { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public PairingSession? Session { get; init; }

    public static PairingValidationResult Success(PairingSession session) =>
        new() { IsSuccess = true, Session = session };

    public static PairingValidationResult Failed(string errorCode, string errorMessage) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
