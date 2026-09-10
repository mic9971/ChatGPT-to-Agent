using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Execution;

/// <summary>
/// Persistence contract for execution evidence records and sanitized artifacts adhering to BR-CON-001, BR-CON-002, and 11-DATA-MODEL.md.
/// </summary>
public interface IExecutionStore
{
    Task SaveAsync(ExecutionRecord record, CancellationToken cancellationToken = default);

    Task<ExecutionRecord?> GetAsync(string workspaceId, string executionId, CancellationToken cancellationToken = default);

    Task<ExecutionRecord?> FindByIdempotencyKeyAsync(
        string workspaceId,
        string taskId,
        int iteration,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<int?> GetLatestIterationAsync(
        string workspaceId,
        string taskId,
        CancellationToken cancellationToken = default);

    Task SaveSanitizedArtifactAsync(
        string workspaceId,
        string taskId,
        string executionId,
        string artifactId,
        string content,
        CancellationToken cancellationToken = default);

    Task<string?> GetSanitizedArtifactAsync(
        string workspaceId,
        string taskId,
        string executionId,
        string artifactId,
        CancellationToken cancellationToken = default);
}
