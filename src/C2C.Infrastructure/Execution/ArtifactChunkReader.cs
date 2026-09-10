using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Common;
using C2C.Core.Execution;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Service for bounded chunk retrieval of sanitized execution artifacts adhering to UC-EXE-04, BR-EXE-004, BR-EXE-005, and BR-SEC-009.
/// </summary>
public sealed class ArtifactChunkReader : IExecutionArtifactReader
{
    private readonly IExecutionStore _executionStore;
    private readonly ExecutionOptions _options;

    public ArtifactChunkReader(
        IExecutionStore executionStore,
        ExecutionOptions? options = null)
    {
        _executionStore = executionStore ?? throw new ArgumentNullException(nameof(executionStore));
        _options = options ?? new ExecutionOptions();
    }

    public async Task<OperationResult<ArtifactChunkDto>> GetChunkAsync(
        string workspaceId,
        ArtifactChunkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ExecutionId))
        {
            return OperationResult<ArtifactChunkDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "ExecutionId must be provided.",
                nameof(request.ExecutionId));
        }

        if (string.IsNullOrWhiteSpace(request.ArtifactId))
        {
            return OperationResult<ArtifactChunkDto>.Failure(
                CommonErrorCodes.InvalidArgument,
                "ArtifactId must be provided.",
                nameof(request.ArtifactId));
        }

        // 1. Retrieve execution record
        ExecutionRecord? record = await _executionStore.GetAsync(workspaceId, request.ExecutionId, cancellationToken);
        if (record == null)
        {
            return OperationResult<ArtifactChunkDto>.Failure(
                CommonErrorCodes.ExecutionNotFound,
                $"Execution record '{request.ExecutionId}' was not found in workspace '{workspaceId}'.",
                request.ExecutionId);
        }

        // 2. Locate artifact reference
        ArtifactRef? artifactRef = null;
        foreach (ArtifactRef ar in record.ArtifactRefs)
        {
            if (string.Equals(ar.ArtifactId, request.ArtifactId, StringComparison.OrdinalIgnoreCase))
            {
                artifactRef = ar;
                break;
            }
        }

        if (artifactRef == null)
        {
            return OperationResult<ArtifactChunkDto>.Failure(
                CommonErrorCodes.WorkspaceItemNotFound,
                $"Artifact '{request.ArtifactId}' was not found on execution record '{request.ExecutionId}'.",
                request.ArtifactId);
        }

        // 3. Enforce Restricted / Missing / Expired artifact policies (BR-EXE-005, BR-SEC-009)
        if (artifactRef.Classification == ArtifactClassification.Restricted)
        {
            return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
            {
                ExecutionId = record.ExecutionId,
                ArtifactId = artifactRef.ArtifactId,
                Classification = ArtifactClassification.Restricted,
                Content = null, // Strictly no body exposed remotely
                StartLine = 0,
                EndLine = 0,
                IsEof = true,
                NextCursor = null,
                ReasonCode = artifactRef.ReasonCode ?? "SENSITIVE_CONTENT"
            });
        }

        if (artifactRef.Classification == ArtifactClassification.Missing)
        {
            return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
            {
                ExecutionId = record.ExecutionId,
                ArtifactId = artifactRef.ArtifactId,
                Classification = ArtifactClassification.Missing,
                Content = null,
                StartLine = 0,
                EndLine = 0,
                IsEof = true,
                NextCursor = null,
                ReasonCode = "MISSING_CONTENT"
            });
        }

        if (artifactRef.Classification == ArtifactClassification.Expired)
        {
            return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
            {
                ExecutionId = record.ExecutionId,
                ArtifactId = artifactRef.ArtifactId,
                Classification = ArtifactClassification.Expired,
                Content = null,
                StartLine = 0,
                EndLine = 0,
                IsEof = true,
                NextCursor = null,
                ReasonCode = "EXPIRED"
            });
        }

        // 4. Retrieve sanitized content for Readable artifact
        string? content = await _executionStore.GetSanitizedArtifactAsync(
            workspaceId,
            record.TaskId,
            record.ExecutionId,
            artifactRef.ArtifactId,
            cancellationToken);

        if (content == null)
        {
            return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
            {
                ExecutionId = record.ExecutionId,
                ArtifactId = artifactRef.ArtifactId,
                Classification = ArtifactClassification.Missing,
                Content = null,
                StartLine = 0,
                EndLine = 0,
                IsEof = true,
                NextCursor = null,
                ReasonCode = "MISSING_CONTENT"
            });
        }

        // 5. Decode cursor and determine limits
        int startLine = 1;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Cursor));
                if (int.TryParse(decoded, out int parsedLine) && parsedLine >= 1)
                {
                    startLine = parsedLine;
                }
            }
            catch (FormatException)
            {
                return OperationResult<ArtifactChunkDto>.Failure(
                    CommonErrorCodes.InvalidArgument,
                    "Invalid cursor format.",
                    nameof(request.Cursor));
            }
        }

        int maxLines = Math.Min(request.LimitLines ?? _options.DefaultChunkLines, _options.MaxChunkLines);
        int maxBytes = Math.Min(request.LimitBytes ?? _options.DefaultChunkBytes, _options.MaxChunkBytes);

        // 6. Read lines bounded by limits
        string[] allLines = content.Split('\n');
        int totalLines = allLines.Length;

        if (startLine > totalLines)
        {
            return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
            {
                ExecutionId = record.ExecutionId,
                ArtifactId = artifactRef.ArtifactId,
                Classification = ArtifactClassification.Readable,
                Content = string.Empty,
                StartLine = startLine,
                EndLine = startLine,
                IsEof = true,
                NextCursor = null,
                ReasonCode = null
            });
        }

        StringBuilder chunkBuilder = new();
        int currentLine = startLine;
        int currentBytes = 0;
        int endLine = startLine;

        while (currentLine <= totalLines && (currentLine - startLine) < maxLines)
        {
            string lineText = allLines[currentLine - 1];
            byte[] lineBytes = Encoding.UTF8.GetBytes(lineText + "\n");

            if (chunkBuilder.Length > 0 && currentBytes + lineBytes.Length > maxBytes)
            {
                break;
            }

            chunkBuilder.Append(lineText).Append('\n');
            currentBytes += lineBytes.Length;
            endLine = currentLine;
            currentLine++;
        }

        bool isEof = currentLine > totalLines;
        string? nextCursor = isEof ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(currentLine.ToString()));

        return OperationResult<ArtifactChunkDto>.Success(new ArtifactChunkDto
        {
            ExecutionId = record.ExecutionId,
            ArtifactId = artifactRef.ArtifactId,
            Classification = ArtifactClassification.Readable,
            Content = chunkBuilder.ToString(),
            StartLine = startLine,
            EndLine = endLine,
            IsEof = isEof,
            NextCursor = nextCursor,
            ReasonCode = null
        });
    }
}
