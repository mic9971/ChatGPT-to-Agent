using System.Diagnostics;

using C2C.Core.Common;
using C2C.Core.Git;

namespace C2C.Infrastructure.Git;

/// <summary>
/// Implements safe Git process execution using typed argument arrays per UC-GIT-01/02.
/// No shell string concatenation is used (BR-APP-001, BR-COM-008).
/// </summary>
public sealed class GitProcess : IGitProcess
{
    private readonly GitOptions _options;

    public GitProcess(GitOptions? options = null)
    {
        _options = options ?? new GitOptions();
    }

    public async Task<OperationResult<string>> RunAsync(
        string workingDirectory,
        string[] arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentNullException.ThrowIfNull(arguments);

        using var timeoutCts = new CancellationTokenSource(_options.ProcessTimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();

            // Read stdout and stderr concurrently to prevent deadlock on full buffers
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            Task<string> stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (process.ExitCode == 0)
            {
                return OperationResult<string>.Success(stdout);
            }

            // stderr may contain sensitive paths or tokens — never log it; surface only typed error code
            return OperationResult<string>.Failure(new OperationError(
                CommonErrorCodes.GitNotAvailable,
                $"Git process exited with code {process.ExitCode}."));
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            return OperationResult<string>.Failure(new OperationError(
                CommonErrorCodes.Timeout,
                $"Git process did not complete within {_options.ProcessTimeoutMs} ms."));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return OperationResult<string>.Failure(new OperationError(
                CommonErrorCodes.GitNotAvailable,
                "Git process could not be started. Ensure Git is installed and available on PATH."));
        }
    }
}
