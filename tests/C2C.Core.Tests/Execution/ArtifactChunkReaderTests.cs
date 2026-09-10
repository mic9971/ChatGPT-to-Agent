using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Infrastructure.Execution;

namespace C2C.Core.Tests.Execution;

public sealed class ArtifactChunkReaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonExecutionStore _store;
    private readonly ArtifactChunkReader _reader;

    public ArtifactChunkReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_chunk_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        ExecutionOptions options = new()
        {
            StorageDirectory = _tempDir,
            DefaultChunkLines = 2,
            MaxChunkLines = 10,
            DefaultChunkBytes = 1024,
            MaxChunkBytes = 4096
        };

        _store = new JsonExecutionStore(options);
        _reader = new ArtifactChunkReader(_store, options);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task GetChunkAsync_ReadableArtifact_ReturnsPagedChunks()
    {
        string workspaceId = "ws-chunk";
        string taskId = "task-chunk";
        string executionId = "exec-chunk-1";

        ExecutionRecord record = new()
        {
            ExecutionId = executionId,
            WorkspaceId = workspaceId,
            TaskId = taskId,
            Iteration = 1,
            IdempotencyKey = "key-chunk",
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs =
            [
                new ArtifactRef
                {
                    ArtifactId = "art-log",
                    Name = "build.log",
                    Classification = ArtifactClassification.Readable,
                    SizeBytes = 50,
                    LineCount = 5
                }
            ]
        };

        await _store.SaveAsync(record);
        await _store.SaveSanitizedArtifactAsync(
            workspaceId,
            taskId,
            executionId,
            "art-log",
            "line1\nline2\nline3\nline4\nline5");

        // First page (default limit is 2 lines)
        var firstChunk = await _reader.GetChunkAsync(workspaceId, new ArtifactChunkRequest
        {
            ExecutionId = executionId,
            ArtifactId = "art-log"
        });

        Assert.True(firstChunk.IsSuccess);
        Assert.Equal(ArtifactClassification.Readable, firstChunk.Value!.Classification);
        Assert.Equal("line1\nline2\n", firstChunk.Value.Content);
        Assert.Equal(1, firstChunk.Value.StartLine);
        Assert.Equal(2, firstChunk.Value.EndLine);
        Assert.False(firstChunk.Value.IsEof);
        Assert.NotNull(firstChunk.Value.NextCursor);

        // Second page using cursor
        var secondChunk = await _reader.GetChunkAsync(workspaceId, new ArtifactChunkRequest
        {
            ExecutionId = executionId,
            ArtifactId = "art-log",
            Cursor = firstChunk.Value.NextCursor
        });

        Assert.True(secondChunk.IsSuccess);
        Assert.Equal("line3\nline4\n", secondChunk.Value!.Content);
        Assert.Equal(3, secondChunk.Value.StartLine);
        Assert.Equal(4, secondChunk.Value.EndLine);
        Assert.False(secondChunk.Value.IsEof);
        Assert.NotNull(secondChunk.Value.NextCursor);

        // Third page
        var thirdChunk = await _reader.GetChunkAsync(workspaceId, new ArtifactChunkRequest
        {
            ExecutionId = executionId,
            ArtifactId = "art-log",
            Cursor = secondChunk.Value.NextCursor
        });

        Assert.True(thirdChunk.IsSuccess);
        Assert.Equal("line5\n", thirdChunk.Value!.Content);
        Assert.True(thirdChunk.Value.IsEof);
        Assert.Null(thirdChunk.Value.NextCursor);
    }

    [Fact]
    public async Task GetChunkAsync_RestrictedArtifact_ReturnsMetadataOnlyAndNeverBody()
    {
        string workspaceId = "ws-chunk";
        string taskId = "task-chunk";
        string executionId = "exec-restricted-1";

        ExecutionRecord record = new()
        {
            ExecutionId = executionId,
            WorkspaceId = workspaceId,
            TaskId = taskId,
            Iteration = 1,
            IdempotencyKey = "key-restr",
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs =
            [
                new ArtifactRef
                {
                    ArtifactId = "secret-log",
                    Name = "auth.log",
                    Classification = ArtifactClassification.Restricted,
                    SizeBytes = 200,
                    LineCount = 10,
                    ReasonCode = "SENSITIVE_CONTENT"
                }
            ]
        };

        await _store.SaveAsync(record);

        var chunk = await _reader.GetChunkAsync(workspaceId, new ArtifactChunkRequest
        {
            ExecutionId = executionId,
            ArtifactId = "secret-log"
        });

        Assert.True(chunk.IsSuccess);
        Assert.Equal(ArtifactClassification.Restricted, chunk.Value!.Classification);
        Assert.Equal("SENSITIVE_CONTENT", chunk.Value.ReasonCode);
        // CRITICAL INVARIANT (BR-EXE-005, BR-SEC-009): Body is strictly null!
        Assert.Null(chunk.Value.Content);
        Assert.True(chunk.Value.IsEof);
    }

    [Fact]
    public async Task GetChunkAsync_ExecutionNotFound_ReturnsError()
    {
        var result = await _reader.GetChunkAsync("ws-chunk", new ArtifactChunkRequest
        {
            ExecutionId = "non-existent",
            ArtifactId = "art-1"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.ExecutionNotFound, result.Error!.Code);
    }
}
