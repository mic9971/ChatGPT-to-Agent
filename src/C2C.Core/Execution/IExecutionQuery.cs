using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;

namespace C2C.Core.Execution;

/// <summary>
/// Query contract for execution summary and normalized test status adhering to UC-EXE-02, UC-EXE-03, and BR-COM-003.
/// </summary>
public interface IExecutionQuery
{
    Task<OperationResult<ExecutionSummaryDto>> GetSummaryAsync(
        string workspaceId,
        string executionId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<TestStatusDto>> GetTestStatusAsync(
        string workspaceId,
        string executionId,
        CancellationToken cancellationToken = default);
}
