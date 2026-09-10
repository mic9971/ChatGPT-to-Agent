using C2C.Core.Common;

namespace C2C.Core.Git;

/// <summary>
/// Low-level process abstraction for running Git commands safely.
/// Infrastructure implements this; Core contracts must not depend on System.Diagnostics.Process (BR-APP-001).
/// </summary>
public interface IGitProcess
{
    /// <summary>
    /// Runs a Git command with an explicit typed argument list (never shell-concatenated).
    /// </summary>
    /// <param name="workingDirectory">Absolute canonical workspace root.</param>
    /// <param name="arguments">Typed argument list — no shell interpolation.</param>
    /// <param name="cancellationToken">Propagated cancellation per BR-COM-008.</param>
    /// <returns>
    /// Success with stdout on exit code 0, or a typed <see cref="OperationError"/> on failure.
    /// stderr is captured separately and never logged with secret content.
    /// </returns>
    Task<OperationResult<string>> RunAsync(
        string workingDirectory,
        string[] arguments,
        CancellationToken cancellationToken = default);
}
