using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Tunnel;

/// <summary>
/// Service contract for converging bridge and tunnel to a ready state adhering to UC-TUN-03 and BR-CON-004.
/// </summary>
public interface IRuntimeEnsurer
{
    /// <summary>
    /// Evaluates bridge and tunnel runtime health, repairs stale state if necessary, and returns machine-readable readiness.
    /// </summary>
    Task<RuntimeEnsureResult> EnsureAsync(CancellationToken cancellationToken = default);
}
