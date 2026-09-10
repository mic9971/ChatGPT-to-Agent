using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Tunnel;

/// <summary>
/// Represents a managed child process owned by the bridge runtime adhering to BR-CON-008.
/// </summary>
public interface IOwnedProcess : IDisposable
{
    int Id { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    string OwnershipMarker { get; }

    DateTimeOffset StartTime { get; }

    StreamReader StandardOutput { get; }

    StreamReader StandardError { get; }

    Task WaitForExitAsync(CancellationToken cancellationToken);

    Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken);

    void Kill();
}
