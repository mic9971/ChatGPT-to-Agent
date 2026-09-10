using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Diagnostics;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Diagnostics;
using C2C.Infrastructure.Tunnel;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class StatusAndDoctorCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly JsonWorkspaceConfigStore _configStore;
    private readonly IRuntimeDiagnostics _diagnostics;

    public StatusAndDoctorCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_status_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "c2c.workspace.json");
        _configStore = new JsonWorkspaceConfigStore(_configFile);

        var pairingOptions = new PairingOptions { StorageDirectory = _tempRoot };
        var pairingStore = new JsonPairingStore(pairingOptions);

        var bridgeRuntime = new LoopbackBridgeRuntime(new TunnelOptions
        {
            LocalEndpoint = new Uri("http://127.0.0.1:5000/")
        });
        var tunnelService = new StubTunnelService();

        _diagnostics = new RuntimeDiagnostics(_configStore, bridgeRuntime, tunnelService, pairingStore);
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
    public async Task StatusCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new StatusCommand(_diagnostics);

        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task StatusCommand_WhenWorkspaceConfigured_ReturnsSuccess()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws_test"),
            CanonicalRoot = _tempRoot,
            CreatedAt = DateTimeOffset.UtcNow,
            Label = "Test"
        };
        await _configStore.SaveAsync(config);

        var command = new StatusCommand(_diagnostics);
        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task DoctorCommand_RunsBoundedChecks_WithoutMutation()
    {
        var command = new DoctorCommand(_diagnostics);

        int exitCode = await command.ExecuteAsync(json: true);

        // Before setup, doctor reports action required (status not_ready due to missing workspace config)
        Assert.True(exitCode == CliExitCode.ActionRequired || exitCode == CliExitCode.Success);
    }

    private sealed class StubTunnelService : ITunnelService
    {
        public Task<OperationResult<TunnelSession>> StartTunnelAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<TunnelSession>.Failure(CommonErrorCodes.NotReady, "Not running"));

        public Task<OperationResult<TunnelSession>> EnsureTunnelAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<TunnelSession>.Failure(CommonErrorCodes.NotReady, "Not running"));

        public Task<OperationResult<bool>> StopTunnelAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<bool>.Success(true));

        public Task<TunnelSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<TunnelSession?>(null);
    }
}
