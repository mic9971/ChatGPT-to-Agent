using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Service for recording execution evidence adhering to UC-EXE-01, BR-EXE-001, BR-CON-001, BR-CON-006, and BR-APP-004.
/// </summary>
public sealed class ExecutionRecorder : IExecutionRecorder
{
    private static readonly JsonSerializerOptions FingerprintJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IExecutionStore _executionStore;
    private readonly IArtifactSanitizer _artifactSanitizer;
    private readonly IWorkspaceAccessPolicy _workspaceAccessPolicy;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly TimeProvider _timeProvider;
    private readonly ExecutionOptions _options;

    public ExecutionRecorder(
        IExecutionStore executionStore,
        IArtifactSanitizer artifactSanitizer,
        IWorkspaceAccessPolicy workspaceAccessPolicy,
        IWorkspaceContext workspaceContext,
        TimeProvider? timeProvider = null,
        ExecutionOptions? options = null)
    {
        _executionStore = executionStore ?? throw new ArgumentNullException(nameof(executionStore));
        _artifactSanitizer = artifactSanitizer ?? throw new ArgumentNullException(nameof(artifactSanitizer));
        _workspaceAccessPolicy = workspaceAccessPolicy ?? throw new ArgumentNullException(nameof(workspaceAccessPolicy));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _options = options ?? new ExecutionOptions();
    }

    public async Task<OperationResult<ExecutionSummaryDto>> RecordAsync(
        ExecutionRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate required identifiers and bounds (BR-COM-006)
        if (string.IsNullOrWhiteSpace(request.WorkspaceId))
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "WorkspaceId cannot be null or whitespace.",
                nameof(request.WorkspaceId));
        }

        if (string.IsNullOrWhiteSpace(request.TaskId))
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "TaskId cannot be null or whitespace.",
                nameof(request.TaskId));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "IdempotencyKey cannot be null or whitespace.",
                nameof(request.IdempotencyKey));
        }

        if (request.Iteration < 1)
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Iteration must be 1 or greater.",
                nameof(request.Iteration));
        }

        if (request.ChangedFiles != null && request.ChangedFiles.Count > _options.MaxChangedFiles)
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.OutputLimitExceeded,
                $"Changed files count ({request.ChangedFiles.Count}) exceeds maximum allowed ({_options.MaxChangedFiles}).",
                nameof(request.ChangedFiles));
        }

        if (request.Artifacts != null && request.Artifacts.Count > _options.MaxArtifacts)
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.OutputLimitExceeded,
                $"Artifacts count ({request.Artifacts.Count}) exceeds maximum allowed ({_options.MaxArtifacts}).",
                nameof(request.Artifacts));
        }

        // 2. Compute payload fingerprint for idempotency check (BR-CON-001)
        string payloadFingerprint = ComputePayloadFingerprint(request);

        // 3. Check for existing record with same idempotency key (BR-CON-001)
        ExecutionRecord? existing = await _executionStore.FindByIdempotencyKeyAsync(
            request.WorkspaceId,
            request.TaskId,
            request.Iteration,
            request.IdempotencyKey,
            cancellationToken);

        if (existing != null)
        {
            if (string.Equals(existing.PayloadFingerprint, payloadFingerprint, StringComparison.Ordinal))
            {
                // Idempotent retry: return existing record summary
                return OperationResult<ExecutionSummaryDto>.Success(MapToSummaryDto(existing));
            }

            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.Conflict,
                $"An execution record with idempotency key '{request.IdempotencyKey}' already exists with a different payload.",
                request.IdempotencyKey);
        }

        // 4. Check monotonic iteration order (BR-CON-006)
        int? latestIteration = await _executionStore.GetLatestIterationAsync(
            request.WorkspaceId,
            request.TaskId,
            cancellationToken);

        if (latestIteration.HasValue && request.Iteration < latestIteration.Value)
        {
            return OperationResult<ExecutionSummaryDto>.Failure(
                CommonErrorCodes.Conflict,
                $"Cannot record iteration {request.Iteration} because newer iteration {latestIteration.Value} already exists for task '{request.TaskId}'.",
                request.TaskId);
        }

        // 5. Generate deterministic execution id
        string executionId = GenerateExecutionId(
            request.WorkspaceId,
            request.TaskId,
            request.Iteration,
            request.IdempotencyKey);

        // 6. Normalize changed files
        List<string> normalizedChangedFiles = new();
        if (request.ChangedFiles != null)
        {
            foreach (string file in request.ChangedFiles)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                string normalized = file.Trim().Replace('\\', '/');
                while (normalized.StartsWith("./", StringComparison.Ordinal))
                {
                    normalized = normalized[2..];
                }
                normalizedChangedFiles.Add(normalized);
            }
        }

        // 7. Sanitize and store artifacts
        List<ArtifactRef> artifactRefs = new();
        if (request.Artifacts != null)
        {
            foreach (ArtifactRecordRequest artifactReq in request.Artifacts)
            {
                SanitizedArtifactResult result = _artifactSanitizer.Sanitize(
                    artifactReq.Name,
                    artifactReq.Content);

                if (result.Classification == ArtifactClassification.Readable &&
                    !string.IsNullOrEmpty(result.SanitizedContent))
                {
                    await _executionStore.SaveSanitizedArtifactAsync(
                        request.WorkspaceId,
                        request.TaskId,
                        executionId,
                        artifactReq.ArtifactId,
                        result.SanitizedContent,
                        cancellationToken);
                }

                artifactRefs.Add(new ArtifactRef
                {
                    ArtifactId = artifactReq.ArtifactId,
                    Name = artifactReq.Name,
                    Classification = result.Classification,
                    SizeBytes = result.SizeBytes,
                    LineCount = result.LineCount,
                    ReasonCode = result.ReasonCode,
                    Sha256Fingerprint = result.Sha256Fingerprint
                });
            }
        }

        // 8. Bounded command text
        string? commandText = request.CommandText;
        if (commandText != null && commandText.Length > _options.MaxCommandTextLength)
        {
            commandText = commandText[.._options.MaxCommandTextLength];
        }

        // 9. Create and atomically save ExecutionRecord
        DateTimeOffset now = _timeProvider.GetUtcNow();
        ExecutionRecord record = new()
        {
            ExecutionId = executionId,
            WorkspaceId = request.WorkspaceId,
            TaskId = request.TaskId,
            Iteration = request.Iteration,
            IdempotencyKey = request.IdempotencyKey,
            ExecutorName = request.ExecutorName,
            ExecutorVersion = request.ExecutorVersion,
            StartedAt = request.StartedAt,
            FinishedAt = request.FinishedAt,
            ExitStatus = request.ExitStatus,
            CommandCategory = request.CommandCategory,
            CommandText = commandText,
            ChangedFiles = normalizedChangedFiles,
            TestSummary = request.TestSummary,
            ArtifactRefs = artifactRefs,
            CreatedAt = now,
            SchemaVersion = 1,
            PayloadFingerprint = payloadFingerprint
        };

        await _executionStore.SaveAsync(record, cancellationToken);

        return OperationResult<ExecutionSummaryDto>.Success(MapToSummaryDto(record));
    }

    private ExecutionSummaryDto MapToSummaryDto(ExecutionRecord record)
    {
        // Filter visible changed files using IWorkspaceAccessPolicy (BR-EXE-002)
        List<string> visibleFiles = new();
        foreach (string file in record.ChangedFiles)
        {
            var eval = _workspaceAccessPolicy.EvaluatePath(_workspaceContext, file);
            if (eval.IsSuccess)
            {
                visibleFiles.Add(file);
            }
        }

        return new ExecutionSummaryDto
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
    }

    private static string GenerateExecutionId(
        string workspaceId,
        string taskId,
        int iteration,
        string idempotencyKey)
    {
        string input = $"{workspaceId}:{taskId}:{iteration}:{idempotencyKey}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant()[..32];
    }

    private static string ComputePayloadFingerprint(ExecutionRecordRequest request)
    {
        var payload = new
        {
            request.ExitStatus,
            request.StartedAt,
            request.FinishedAt,
            request.CommandCategory,
            request.CommandText,
            request.ChangedFiles,
            request.TestSummary,
            Artifacts = request.Artifacts == null ? null : new List<object>(
                request.Artifacts.Select(a => new { a.ArtifactId, a.Name, a.ArtifactType, a.Content }))
        };

        string json = JsonSerializer.Serialize(payload, FingerprintJsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
