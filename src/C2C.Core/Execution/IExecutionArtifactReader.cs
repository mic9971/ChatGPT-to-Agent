using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Execution;

/// <summary>
/// Service contract for bounded, sanitized execution artifact chunk retrieval adhering to UC-EXE-04, BR-EXE-004, and BR-EXE-005.
/// </summary>
public interface IExecutionArtifactReader
{
    Task<OperationResult<ArtifactChunkDto>> GetChunkAsync(
        string workspaceId,
        ArtifactChunkRequest request,
        CancellationToken cancellationToken = default);
}
