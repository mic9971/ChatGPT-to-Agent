namespace C2C.Core.Execution;

/// <summary>
/// Service contract for local, deterministic artifact sanitization and classification adhering to BR-SEC-009, BR-SEC-012, and 08-EXECUTION-REVIEW.md.
/// </summary>
public interface IArtifactSanitizer
{
    SanitizedArtifactResult Sanitize(string artifactName, string rawContent);
}
