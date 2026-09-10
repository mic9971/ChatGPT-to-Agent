using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class RecordCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly FakeExecutionRecorder _recorder;

    public RecordCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_record_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceConfigStore = new JsonWorkspaceConfigStore(_configFile);
        _recorder = new FakeExecutionRecorder();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task RecordCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(
            inputFilePath: null,
            taskId: "task-1",
            iteration: 1,
            exitStatus: 0,
            category: "test",
            command: "dotnet test",
            idempotencyKey: null,
            json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task RecordCommand_WithFlags_RecordsEvidenceSuccessfully()
    {
        await SetupWorkspaceAsync();
        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(
            inputFilePath: null,
            taskId: "task-100",
            iteration: 1,
            exitStatus: 0,
            category: "build",
            command: "dotnet build",
            idempotencyKey: "key-100",
            json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.NotNull(_recorder.LastRequest);
        Assert.Equal("task-100", _recorder.LastRequest.TaskId);
        Assert.Equal("key-100", _recorder.LastRequest.IdempotencyKey);
        Assert.Equal("ws-record", _recorder.LastRequest.WorkspaceId);
    }

    [Fact]
    public async Task RecordCommand_WithInputFile_RecordsSuccessfully()
    {
        await SetupWorkspaceAsync();

        string inputPath = Path.Combine(_tempRoot, "evidence.json");
        var req = new
        {
            taskId = "task-file",
            iteration = 2,
            idempotencyKey = "key-file",
            exitStatus = 0,
            commandCategory = "test",
            commandText = "dotnet test"
        };
        await File.WriteAllTextAsync(inputPath, JsonSerializer.Serialize(req));

        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(
            inputFilePath: inputPath,
            taskId: null,
            iteration: 1,
            exitStatus: 0,
            category: null,
            command: null,
            idempotencyKey: null,
            json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.NotNull(_recorder.LastRequest);
        Assert.Equal("task-file", _recorder.LastRequest.TaskId);
        Assert.Equal("key-file", _recorder.LastRequest.IdempotencyKey);
    }

    [Fact]
    public async Task RecordCommand_WithNonExistentInputFile_ReturnsInvalidUsage2()
    {
        await SetupWorkspaceAsync();
        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(
            inputFilePath: Path.Combine(_tempRoot, "non-existent.json"),
            taskId: null,
            iteration: 1,
            exitStatus: 0,
            category: null,
            command: null,
            idempotencyKey: null,
            json: true);

        Assert.Equal(CliExitCode.InvalidUsageOrConfig, exitCode);
    }

    [Fact]
    public async Task RecordCommand_WithOversizedInputFile_ReturnsOutputLimitExceeded()
    {
        await SetupWorkspaceAsync();

        string oversizedPath = Path.Combine(_tempRoot, "oversized.json");
        // Create file larger than 1MB
        byte[] largeData = new byte[1024 * 1024 + 100];
        Array.Fill<byte>(largeData, (byte)'A');
        await File.WriteAllBytesAsync(oversizedPath, largeData);

        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(
            inputFilePath: oversizedPath,
            taskId: null,
            iteration: 1,
            exitStatus: 0,
            category: null,
            command: null,
            idempotencyKey: null,
            json: true);

        Assert.Equal(CliExitCode.InvalidUsageOrConfig, exitCode);
    }

    [Fact]
    public async Task RecordCommand_WhenCancelled_ReturnsTimeoutOrCancelled7()
    {
        await SetupWorkspaceAsync();
        var command = new RecordCommand(_recorder, _workspaceConfigStore);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await command.ExecuteAsync(
            inputFilePath: null,
            taskId: "task-cancel",
            iteration: 1,
            exitStatus: 0,
            category: null,
            command: null,
            idempotencyKey: null,
            json: true,
            cancellationToken: cts.Token);

        Assert.Equal(CliExitCode.TimeoutOrCancelled, exitCode);
    }

    private async Task SetupWorkspaceAsync()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-record"),
            CanonicalRoot = _tempRoot,
            Label = "Record Test Workspace",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workspaceConfigStore.SaveAsync(config);
    }

    private sealed class FakeExecutionRecorder : IExecutionRecorder
    {
        public ExecutionRecordRequest? LastRequest { get; private set; }

        public OperationResult<ExecutionSummaryDto> ResultToReturn { get; set; } =
            OperationResult<ExecutionSummaryDto>.Success(new ExecutionSummaryDto
            {
                ExecutionId = "exe_test_123",
                WorkspaceId = "ws-record",
                TaskId = "task-1",
                Iteration = 1
            });

        public Task<OperationResult<ExecutionSummaryDto>> RecordAsync(
            ExecutionRecordRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(ResultToReturn);
        }
    }
}
