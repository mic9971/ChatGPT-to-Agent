using System;
using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Runtime;

/// <summary>
/// Information about a newly launched host process.
/// </summary>
public sealed record LaunchedProcessInfo(int ProcessId, DateTimeOffset StartTime, string ExecutablePath);

/// <summary>
/// Abstraction for launching the background C2C.Host process adhering to 05-UC-CLI-02-RUNTIME-LIFECYCLE.md.
/// </summary>
public interface IRuntimeHostLauncher
{
    Task<LaunchedProcessInfo> LaunchAsync(
        string workspaceId,
        Uri endpoint,
        CancellationToken cancellationToken = default);
}
