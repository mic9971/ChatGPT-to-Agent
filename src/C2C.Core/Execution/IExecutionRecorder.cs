using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Execution;

/// <summary>
/// Service contract for recording execution evidence adhering to UC-EXE-01 and BR-EXE-006.
/// </summary>
public interface IExecutionRecorder
{
    Task<OperationResult<ExecutionSummaryDto>> RecordAsync(
        ExecutionRecordRequest request,
        CancellationToken cancellationToken = default);
}
