using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class EnsureCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly FakeRuntimeEnsurer _ensurer;

    public EnsureCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_ensure_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceConfigStore = new JsonWorkspaceConfigStore(_configFile);
        _ensurer = new FakeRuntimeEnsurer();
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
    public async Task EnsureCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new EnsureCommand(_ensurer, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task EnsureCommand_WhenNeedPairing_ReturnsActionRequired_AndRunPair()
    {
        await SetupWorkspaceAsync();
        _ensurer.ResultToReturn = new RuntimeEnsureResult
        {
            Status = RuntimeStatus.NeedPairing,
            Bridge = "healthy",
            Tunnel = "healthy",
            Pairing = "need_pairing",
            PublicUrl = new Uri("https://test.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            ErrorCode = CommonErrorCodes.NotReady,
            Message = "Client pairing required."
        };

        var command = new EnsureCommand(_ensurer, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task EnsureCommand_WhenReady_ReturnsSuccess0()
    {
        await SetupWorkspaceAsync();
        _ensurer.ResultToReturn = new RuntimeEnsureResult
        {
            Status = RuntimeStatus.Ready,
            Bridge = "healthy",
            Tunnel = "healthy",
            Pairing = "paired",
            PublicUrl = new Uri("https://test.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000")
        };

        var command = new EnsureCommand(_ensurer, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task EnsureCommand_WhenConflict_ReturnsConflict6()
    {
        await SetupWorkspaceAsync();
        _ensurer.ResultToReturn = new RuntimeEnsureResult
        {
            Status = RuntimeStatus.Error,
            Bridge = "conflict",
            Tunnel = "stopped",
            Pairing = "unknown",
            ErrorCode = CommonErrorCodes.Conflict,
            Message = "Port occupied by foreign process."
        };

        var command = new EnsureCommand(_ensurer, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.Conflict, exitCode);
    }

    [Fact]
    public async Task EnsureCommand_WhenCancelled_ReturnsTimeoutOrCancelled7()
    {
        await SetupWorkspaceAsync();
        _ensurer.ThrowOnEnsure = new OperationCanceledException();

        var command = new EnsureCommand(_ensurer, _workspaceConfigStore);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await command.ExecuteAsync(json: true, cancellationToken: cts.Token);

        Assert.Equal(CliExitCode.TimeoutOrCancelled, exitCode);
    }

    private async Task SetupWorkspaceAsync()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-ensure"),
            CanonicalRoot = _tempRoot,
            Label = "Ensure Workspace",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workspaceConfigStore.SaveAsync(config);
    }

    private sealed class FakeRuntimeEnsurer : IRuntimeEnsurer
    {
        public RuntimeEnsureResult ResultToReturn { get; set; } = new()
        {
            Status = RuntimeStatus.Ready,
            Bridge = "healthy",
            Tunnel = "healthy",
            Pairing = "paired"
        };

        public Exception? ThrowOnEnsure { get; set; }

        public Task<RuntimeEnsureResult> EnsureAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnEnsure != null)
            {
                throw ThrowOnEnsure;
            }

            return Task.FromResult(ResultToReturn);
        }
    }
}
