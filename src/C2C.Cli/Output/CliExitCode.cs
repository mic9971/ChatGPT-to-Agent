namespace C2C.Cli.Output;

/// <summary>
/// Exit codes adhering to 03-CLI-CONTRACT-AND-EXIT-CODES.md.
/// </summary>
public static class CliExitCode
{
    /// <summary>
    /// Command completed successfully or requested state reached.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Runtime failure or unexpected internal error.
    /// </summary>
    public const int RuntimeFailure = 1;

    /// <summary>
    /// Invalid usage or invalid configuration.
    /// </summary>
    public const int InvalidUsageOrConfig = 2;

    /// <summary>
    /// Valid command but action is required before readiness (e.g. setup, pairing).
    /// </summary>
    public const int ActionRequired = 3;

    /// <summary>
    /// Security, authentication, or authorization denial.
    /// </summary>
    public const int SecurityDenied = 4;

    /// <summary>
    /// Required external dependency unavailable.
    /// </summary>
    public const int DependencyMissing = 5;

    /// <summary>
    /// Ownership, port, or runtime conflict.
    /// </summary>
    public const int Conflict = 6;

    /// <summary>
    /// Timeout or cancellation.
    /// </summary>
    public const int TimeoutOrCancelled = 7;
}
