using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Runtime;

/// <summary>
/// Controls background host runtime lifecycle adhering to 05-UC-CLI-02-RUNTIME-LIFECYCLE.md.
/// </summary>
public interface IRuntimeController
{
    Task<OperationResult<RuntimeStartResult>> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> StopAsync(
        RuntimeStopRequest request,
        CancellationToken cancellationToken = default);
}
