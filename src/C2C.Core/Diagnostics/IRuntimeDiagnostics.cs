using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Diagnostics;

/// <summary>
/// Domain contract for non-mutating runtime status snapshot and deep diagnostics (UC-CLI-03).
/// </summary>
public interface IRuntimeDiagnostics
{
    Task<RuntimeStatusSnapshot> GetStatusAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default);

    Task<DoctorReport> RunDoctorAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default);
}
