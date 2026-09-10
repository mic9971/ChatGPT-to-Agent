using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Production process runner wrapping System.Diagnostics.Process with typed argument lists and ownership tracking per BR-CON-008.
/// </summary>
public sealed class SystemOwnedProcessRunner : IOwnedProcessRunner
{
    public IOwnedProcess Start(
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string ownershipMarker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownershipMarker);

        ProcessStartInfo startInfo = new(executablePath)
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

        Process process = new() { StartInfo = startInfo };
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        try
        {
            process.Start();
            return new SystemOwnedProcess(process, ownershipMarker, startTime);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private sealed class SystemOwnedProcess : IOwnedProcess
    {
        private readonly Process _process;
        private bool _disposed;

        public SystemOwnedProcess(Process process, string ownershipMarker, DateTimeOffset startTime)
        {
            _process = process;
            OwnershipMarker = ownershipMarker;
            StartTime = startTime;
        }

        public int Id => _process.Id;

        public bool HasExited => _process.HasExited;

        public int ExitCode => _process.ExitCode;

        public string OwnershipMarker { get; }

        public DateTimeOffset StartTime { get; }

        public StreamReader StandardOutput => _process.StandardOutput;

        public StreamReader StandardError => _process.StandardError;

        public async Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            await _process.WaitForExitAsync(cancellationToken);
        }

        public async Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken)
        {
            if (HasExited)
            {
                return;
            }

            try
            {
                using var timeoutCts = new CancellationTokenSource(gracefulTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // Wait gracefully for process termination
                await _process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Graceful period expired or cancelled: force kill owned child process
                Kill();
            }
        }

        public void Kill()
        {
            try
            {
                if (!HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Kill();
            _process.Dispose();
        }
    }
}
