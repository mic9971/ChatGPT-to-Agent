using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Query service for execution evidence summary and normalized test status adhering to UC-EXE-02, UC-EXE-03, BR-COM-001, and BR-EXE-007.
/// </summary>
public sealed class ExecutionQuery : IExecutionQuery
{
    private readonly IExecutionStore _executionStore;
    private readonly IWorkspaceAccessPolicy _workspaceAccessPolicy;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ExecutionOptions _options;

    public ExecutionQuery(
        IExecutionStore executionStore,
        IWorkspaceAccessPolicy workspaceAccessPolicy,
        IWorkspaceContext workspaceContext,
        ExecutionOptions? options = null)
    {
        _executionStore = executionStore ?? throw new ArgumentNullException(nameof(executionStore));
        _workspaceAccessPolicy = workspaceAccessPolicy ?? throw new ArgumentNullException(nameof(workspaceAccessPolicy));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _options = options ?? new ExecutionOptions();
    }

    public async Task<OperationResult<ExecutionSummaryDto>> GetSummaryAsync(
        string workspaceId,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(executionId);

        // 1. Cross-workspace check: fail closed (BR-COM-001, BR-SEC-006)
        if (!string.Equals(_workspaceContext.Id.Value, workspaceId, StringComparison.Ordinal))
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.ExecutionNotFound,
                $"Execution '{executionId}' not found.",
                executionId);
        }

        // 2. Load execution record
        ExecutionRecord? record = await _executionStore.GetAsync(workspaceId, executionId, cancellationToken);
        if (record == null || !string.Equals(record.WorkspaceId, workspaceId, StringComparison.Ordinal))
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.ExecutionNotFound,
                $"Execution '{executionId}' not found.",
                executionId);
        }

        // 3. Filter visible changed files (BR-EXE-002)
        List<string> visibleFiles = new();
        foreach (string file in record.ChangedFiles)
        {
            var eval = _workspaceAccessPolicy.EvaluatePath(_workspaceContext, file);
            if (eval.IsSuccess)
            {
                visibleFiles.Add(file);
            }
        }

        // 4. Return safe summary without raw artifact bodies (BR-EXE-005)
        ExecutionSummaryDto dto = new()
        {
            ExecutionId = record.ExecutionId,
            WorkspaceId = record.WorkspaceId,
            TaskId = record.TaskId,
            Iteration = record.Iteration,
            StartedAt = record.StartedAt,
            FinishedAt = record.FinishedAt,
            ExitStatus = record.ExitStatus,
            CommandCategory = record.CommandCategory,
            VisibleChangedFiles = visibleFiles,
            TestSummary = record.TestSummary,
            Artifacts = record.ArtifactRefs,
            SchemaVersion = record.SchemaVersion
        };

        return OperationResult<ExecutionSummaryDto>.Success(dto);
    }

    public async Task<OperationResult<TestStatusDto>> GetTestStatusAsync(
        string workspaceId,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(executionId);

        // 1. Cross-workspace check: fail closed (BR-COM-001, BR-SEC-006)
        if (!string.Equals(_workspaceContext.Id.Value, workspaceId, StringComparison.Ordinal))
        {
            return OperationResult<TestStatusDto>.Failure(
                CommonErrorCodes.ExecutionNotFound,
                $"Execution '{executionId}' not found.",
                executionId);
        }

        // 2. Load execution record
        ExecutionRecord? record = await _executionStore.GetAsync(workspaceId, executionId, cancellationToken);
        if (record == null || !string.Equals(record.WorkspaceId, workspaceId, StringComparison.Ordinal))
        {
            return OperationResult<TestStatusDto>.Failure(
                CommonErrorCodes.ExecutionNotFound,
                $"Execution '{executionId}' not found.",
                executionId);
        }

        // 3. Normalize test status (BR-EXE-003, BR-EXE-007)
        TestStatus status;
        TestSummaryDto? summary = record.TestSummary;

        if (summary != null)
        {
            if (summary.Failed > 0)
            {
                status = TestStatus.Failed;
            }
            else if (summary.Passed > 0)
            {
                status = TestStatus.Passed;
            }
            else if (summary.Skipped > 0)
            {
                status = TestStatus.Partial;
            }
            else
            {
                status = TestStatus.NotRun;
            }

            // Sanitize and bound failing tests names
            if (summary.FailingTests.Count > 0)
            {
                List<string> sanitizedFailures = new();
                int limit = Math.Min(summary.FailingTests.Count, _options.MaxFailingTestNames);
                for (int i = 0; i < limit; i++)
                {
                    string name = summary.FailingTests[i];
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        sanitizedFailures.Add(SanitizeTestName(name));
                    }
                }

                summary = new TestSummaryDto
                {
                    Total = summary.Total,
                    Passed = summary.Passed,
                    Failed = summary.Failed,
                    Skipped = summary.Skipped,
                    Suites = summary.Suites,
                    FailingTests = sanitizedFailures
                };
            }
        }
        else
        {
            // CRITICAL INVARIANT: exit code 0 alone does NOT imply Passed (BR-EXE-007)
            status = record.ExitStatus == 0 ? TestStatus.NotRun : TestStatus.Unknown;
        }

        long? durationMs = null;
        if (record.FinishedAt >= record.StartedAt)
        {
            durationMs = (long)(record.FinishedAt - record.StartedAt).TotalMilliseconds;
        }

        TestStatusDto result = new()
        {
            ExecutionId = record.ExecutionId,
            Status = status,
            Summary = summary,
            DurationMs = durationMs,
            SchemaVersion = record.SchemaVersion
        };

        return OperationResult<TestStatusDto>.Success(result);
    }

    private static string SanitizeTestName(string testName)
    {
        char[] chars = testName.Trim().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsControl(chars[i]))
            {
                chars[i] = ' ';
            }
        }

        string sanitized = new(chars);
        if (sanitized.Length > 256)
        {
            sanitized = sanitized[..256];
        }

        return sanitized;
    }
}
