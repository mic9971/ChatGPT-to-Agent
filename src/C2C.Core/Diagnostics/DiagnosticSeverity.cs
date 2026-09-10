using System.Text.Json.Serialization;

namespace C2C.Core.Diagnostics;

/// <summary>
/// Severity level of a diagnostic check adhering to 04-UC-CLI-01-03-FOUNDATION.md.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DiagnosticSeverity
{
    /// <summary>
    /// Check succeeded; no action required.
    /// </summary>
    Pass,

    /// <summary>
    /// Optional capability missing or degraded; system remains functional.
    /// </summary>
    Warn,

    /// <summary>
    /// Required capability unavailable or invalid.
    /// </summary>
    Error,

    /// <summary>
    /// Security, ownership, or configuration conflict requiring explicit user intervention.
    /// </summary>
    Blocked
}
