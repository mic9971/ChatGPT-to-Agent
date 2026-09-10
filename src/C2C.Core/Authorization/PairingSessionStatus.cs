using System.Text.Json.Serialization;

namespace C2C.Core.Authorization;

/// <summary>
/// Lifecycle status of a workspace pairing session adhering to UC-AUTH-01 and 04-DETAILED-AUTH-DESIGN.md.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PairingSessionStatus
{
    /// <summary>
    /// Session is active and waiting for client approval.
    /// </summary>
    Active,

    /// <summary>
    /// Pairing code was successfully verified and consumed by an authorized client.
    /// </summary>
    Consumed,

    /// <summary>
    /// Session expired before approval.
    /// </summary>
    Expired,

    /// <summary>
    /// Maximum verification attempts exhausted.
    /// </summary>
    Locked
}
