using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;
using C2C.Infrastructure.Execution;
using C2C.Infrastructure.Workspace;

namespace C2C.Core.Tests.Execution;

public sealed class ExecutionQueryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _workspaceDir;
    private readonly WorkspaceContext _context;
    private readonly JsonExecutionStore _store;
    private readonly ExecutionQuery _query;

    public ExecutionQueryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_query_test_" + Guid.NewGuid().ToString("N"));
        _workspaceDir = Path.Combine(_tempDir, "workspace");
        Directory.CreateDirectory(_workspaceDir);

        _context = new WorkspaceContext(new WorkspaceId("ws_query_test"), _workspaceDir);

        ExecutionOptions options = new()
        {
            StorageDirectory = _tempDir,
            MaxFailingTestNames = 2
        };

        _store = new JsonExecutionStore(options);

        var canonicalResolver = new CanonicalPathResolver();
        var sensitivePolicy = new SensitivePathPolicy();
        var accessPolicy = new WorkspaceAccessPolicy(
            canonicalResolver,
            sensitivePolicy,
            ctx => new IgnorePolicy(["*.tmp"]));

        _query = new ExecutionQuery(_store, accessPolicy, _context, options);
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
    public async Task GetSummaryAsync_CrossWorkspace_ReturnsExecutionNotFound()
    {
        // Record saved under a different workspace
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-other-ws",
            WorkspaceId = "other-workspace",
            TaskId = "task-1",
            Iteration = 1,
            IdempotencyKey = "key-1",
            StartedAt = DateTimeOffset.UtcNow,
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs = []
        };

        await _store.SaveAsync(record);

        var result = await _query.GetSummaryAsync("other-workspace", "exec-other-ws");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.ExecutionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetSummaryAsync_ValidExecution_ReturnsSafeSummaryWithoutBodies()
    {
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-summary-1",
            WorkspaceId = _context.Id.Value,
            TaskId = "task-summary",
            Iteration = 1,
            IdempotencyKey = "key-sum",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            CommandCategory = "test",
            ChangedFiles = ["src/Code.cs", ".env"],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs =
            [
                new ArtifactRef
                {
                    ArtifactId = "art-1",
                    Name = "build.log",
                    Classification = ArtifactClassification.Readable,
                    SizeBytes = 1024,
                    LineCount = 20
                }
            ]
        };

        await _store.SaveAsync(record);

        var result = await _query.GetSummaryAsync(_context.Id.Value, "exec-summary-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("exec-summary-1", result.Value!.ExecutionId);
        // Sensitive .env filtered out
        Assert.Single(result.Value.VisibleChangedFiles);
        Assert.Equal("src/Code.cs", result.Value.VisibleChangedFiles[0]);
        // Artifact metadata present, safe summary
        Assert.Single(result.Value.Artifacts);
        Assert.Equal("build.log", result.Value.Artifacts[0].Name);
    }

    [Fact]
    public async Task GetTestStatusAsync_Exit0WithoutTestMetadata_ReturnsNotRun()
    {
        // CRITICAL INVARIANT: BR-EXE-007
        // Exit code 0 with NO test metadata MUST return NotRun, NEVER Passed!
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-no-tests",
            WorkspaceId = _context.Id.Value,
            TaskId = "task-not-run",
            Iteration = 1,
            IdempotencyKey = "key-no-tests",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            TestSummary = null, // No test summary!
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs = []
        };

        await _store.SaveAsync(record);

        var result = await _query.GetTestStatusAsync(_context.Id.Value, "exec-no-tests");

        Assert.True(result.IsSuccess);
        Assert.Equal(TestStatus.NotRun, result.Value!.Status);
        Assert.Null(result.Value.Summary);
    }

    [Fact]
    public async Task GetTestStatusAsync_Exit1WithoutTestMetadata_ReturnsUnknown()
    {
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-fail-no-tests",
            WorkspaceId = _context.Id.Value,
            TaskId = "task-unknown",
            Iteration = 1,
            IdempotencyKey = "key-fail-no-tests",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 1,
            TestSummary = null,
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs = []
        };

        await _store.SaveAsync(record);

        var result = await _query.GetTestStatusAsync(_context.Id.Value, "exec-fail-no-tests");

        Assert.True(result.IsSuccess);
        Assert.Equal(TestStatus.Unknown, result.Value!.Status);
    }

    [Fact]
    public async Task GetTestStatusAsync_PassedSummary_ReturnsPassed()
    {
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-passed",
            WorkspaceId = _context.Id.Value,
            TaskId = "task-passed",
            Iteration = 1,
            IdempotencyKey = "key-passed",
            StartedAt = DateTimeOffset.UtcNow.AddSeconds(-30),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            TestSummary = new TestSummaryDto
            {
                Total = 5,
                Passed = 5,
                Failed = 0,
                Skipped = 0,
                Suites = 1
            },
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs = []
        };

        await _store.SaveAsync(record);

        var result = await _query.GetTestStatusAsync(_context.Id.Value, "exec-passed");

        Assert.True(result.IsSuccess);
        Assert.Equal(TestStatus.Passed, result.Value!.Status);
        Assert.Equal(5, result.Value.Summary!.Passed);
    }

    [Fact]
    public async Task GetTestStatusAsync_FailedSummary_ReturnsFailedWithSanitizedNames()
    {
        ExecutionRecord record = new()
        {
            ExecutionId = "exec-failed",
            WorkspaceId = _context.Id.Value,
            TaskId = "task-failed",
            Iteration = 1,
            IdempotencyKey = "key-failed",
            StartedAt = DateTimeOffset.UtcNow.AddSeconds(-30),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 1,
            TestSummary = new TestSummaryDto
            {
                Total = 3,
                Passed = 1,
                Failed = 2,
                Skipped = 0,
                Suites = 1,
                FailingTests = ["TestA\0\r\nBadChar", "TestB", "TestC_Extra"]
            },
            ChangedFiles = [],
            CreatedAt = DateTimeOffset.UtcNow,
            PayloadFingerprint = "fp",
            ArtifactRefs = []
        };

        await _store.SaveAsync(record);

        var result = await _query.GetTestStatusAsync(_context.Id.Value, "exec-failed");

        Assert.True(result.IsSuccess);
        Assert.Equal(TestStatus.Failed, result.Value!.Status);
        // MaxFailingTestNames configured to 2 in constructor, so only 2 returned
        Assert.Equal(2, result.Value.Summary!.FailingTests.Count);
        // Control characters stripped
        Assert.DoesNotContain('\0', result.Value.Summary.FailingTests[0]);
    }
}
