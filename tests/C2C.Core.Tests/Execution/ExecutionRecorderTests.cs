using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;
using C2C.Infrastructure.Execution;
using C2C.Infrastructure.Workspace;

namespace C2C.Core.Tests.Execution;

public sealed class ExecutionRecorderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _workspaceDir;
    private readonly WorkspaceContext _context;
    private readonly JsonExecutionStore _store;
    private readonly ArtifactSanitizer _sanitizer;
    private readonly ExecutionRecorder _recorder;

    public ExecutionRecorderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_exec_test_" + Guid.NewGuid().ToString("N"));
        _workspaceDir = Path.Combine(_tempDir, "workspace");
        Directory.CreateDirectory(_workspaceDir);

        _context = new WorkspaceContext(new WorkspaceId("ws_recorder_test"), _workspaceDir);

        ExecutionOptions options = new()
        {
            StorageDirectory = _tempDir,
            MaxChangedFiles = 5,
            MaxArtifacts = 3
        };

        _store = new JsonExecutionStore(options);
        _sanitizer = new ArtifactSanitizer(options);

        var canonicalResolver = new CanonicalPathResolver();
        var sensitivePolicy = new SensitivePathPolicy();
        var accessPolicy = new WorkspaceAccessPolicy(
            canonicalResolver,
            sensitivePolicy,
            ctx => new IgnorePolicy(["*.tmp"]));

        _recorder = new ExecutionRecorder(
            _store,
            _sanitizer,
            accessPolicy,
            _context,
            TimeProvider.System,
            options);
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
            // Best-effort cleanup
        }
    }

    [Fact]
    public async Task RecordAsync_ValidRequest_PersistsAndReturnsSummary()
    {
        ExecutionRecordRequest request = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-101",
            Iteration = 1,
            IdempotencyKey = "key-1",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            CommandCategory = "test",
            CommandText = "dotnet test",
            ChangedFiles = ["src/Foo.cs", "tests/FooTests.cs"],
            TestSummary = new TestSummaryDto
            {
                Total = 10,
                Passed = 10,
                Failed = 0,
                Skipped = 0,
                Suites = 1
            },
            Artifacts =
            [
                new ArtifactRecordRequest
                {
                    ArtifactId = "log-1",
                    Name = "test.log",
                    ArtifactType = "log",
                    Content = "All tests passed.\n"
                }
            ]
        };

        var result = await _recorder.RecordAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.ExecutionId);
        Assert.Equal("task-101", result.Value.TaskId);
        Assert.Equal(1, result.Value.Iteration);
        Assert.Equal(2, result.Value.VisibleChangedFiles.Count);
        Assert.Single(result.Value.Artifacts);
        Assert.Equal(ArtifactClassification.Readable, result.Value.Artifacts[0].Classification);

        // Verify persisted record in store
        ExecutionRecord? persisted = await _store.GetAsync(_context.Id.Value, result.Value.ExecutionId);
        Assert.NotNull(persisted);
        Assert.Equal(1, persisted.SchemaVersion);
    }

    [Fact]
    public async Task RecordAsync_IdempotentDuplicate_ReturnsExistingExecutionId()
    {
        ExecutionRecordRequest request = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-idempotent",
            Iteration = 1,
            IdempotencyKey = "key-idem",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            CommandCategory = "build"
        };

        var first = await _recorder.RecordAsync(request);
        var second = await _recorder.RecordAsync(request);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.ExecutionId, second.Value!.ExecutionId);
    }

    [Fact]
    public async Task RecordAsync_ConflictingPayload_ReturnsConflict()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ExecutionRecordRequest request1 = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-conflict",
            Iteration = 1,
            IdempotencyKey = "key-conflict",
            StartedAt = now.AddMinutes(-2),
            FinishedAt = now,
            ExitStatus = 0
        };

        ExecutionRecordRequest request2 = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-conflict",
            Iteration = 1,
            IdempotencyKey = "key-conflict",
            StartedAt = now.AddMinutes(-2),
            FinishedAt = now,
            ExitStatus = 1 // Different exit status!
        };

        var first = await _recorder.RecordAsync(request1);
        var second = await _recorder.RecordAsync(request2);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CommonErrorCodes.Conflict, second.Error!.Code);
    }

    [Fact]
    public async Task RecordAsync_OlderIteration_ReturnsConflict()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ExecutionRecordRequest iter2 = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-iter",
            Iteration = 2,
            IdempotencyKey = "key-iter-2",
            StartedAt = now.AddMinutes(-1),
            FinishedAt = now,
            ExitStatus = 0
        };

        ExecutionRecordRequest iter1 = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-iter",
            Iteration = 1, // Older iteration!
            IdempotencyKey = "key-iter-1",
            StartedAt = now.AddMinutes(-2),
            FinishedAt = now.AddMinutes(-1),
            ExitStatus = 0
        };

        var first = await _recorder.RecordAsync(iter2);
        var second = await _recorder.RecordAsync(iter1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(CommonErrorCodes.Conflict, second.Error!.Code);
    }

    [Fact]
    public async Task RecordAsync_ExceedsLimits_ReturnsOutputLimitExceeded()
    {
        ExecutionRecordRequest request = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-limits",
            Iteration = 1,
            IdempotencyKey = "key-limits",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            ChangedFiles = ["f1", "f2", "f3", "f4", "f5", "f6"] // Cap is 5
        };

        var result = await _recorder.RecordAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.OutputLimitExceeded, result.Error!.Code);
    }

    [Fact]
    public async Task RecordAsync_SensitiveChangedFile_FilteredInSummary()
    {
        ExecutionRecordRequest request = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-sensitive",
            Iteration = 1,
            IdempotencyKey = "key-sensitive",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            ChangedFiles = ["src/Normal.cs", ".env", "secrets/key.pem"]
        };

        var result = await _recorder.RecordAsync(request);

        Assert.True(result.IsSuccess);
        // Sensitive files .env and secrets/key.pem must NOT appear in VisibleChangedFiles
        Assert.Single(result.Value!.VisibleChangedFiles);
        Assert.Equal("src/Normal.cs", result.Value.VisibleChangedFiles[0]);
    }
}
