using System.Text.Json.Serialization;

namespace C2C.Core.Execution;

/// <summary>
/// Classification of execution artifacts adhering to BR-SEC-009 and 08-EXECUTION-REVIEW.md.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtifactClassification
{
    /// <summary>
    /// Safe text artifact; body can be requested through chunked reads after sanitization.
    /// </summary>
    Readable = 0,

    /// <summary>
    /// Contains sensitive secrets (tokens, private keys), pairing codes, or binary data;
    /// metadata is exposed, but body is strictly unavailable remotely.
    /// </summary>
    Restricted = 1,

    /// <summary>
    /// Referenced file could not be found or read at record time.
    /// </summary>
    Missing = 2,

    /// <summary>
    /// Artifact body was pruned by retention policy; metadata may remain.
    /// </summary>
    Expired = 3
}
