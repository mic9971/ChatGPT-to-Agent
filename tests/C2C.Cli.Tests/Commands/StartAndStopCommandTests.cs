using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Runtime;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Runtime;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class StartAndStopCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly IRuntimeOwnershipStore _ownershipStore;
    private readonly FakeOwnedProcessValidator _processValidator;
    private readonly FakeBridgeRuntime _bridgeRuntime;
    private readonly FakeTunnelService _tunnelService;
    private readonly FakeRuntimeHostLauncher _hostLauncher;
    private readonly IRuntimeController _runtimeController;

    public StartAndStopCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_startstop_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceConfigStore = new JsonWorkspaceConfigStore(_configFile);
        _ownershipStore = new JsonRuntimeOwnershipStore(_tempRoot);

        _processValidator = new FakeOwnedProcessValidator();
        _bridgeRuntime = new FakeBridgeRuntime();
        _tunnelService = new FakeTunnelService();
        _hostLauncher = new FakeRuntimeHostLauncher(_bridgeRuntime);

        var options = new TunnelOptions
        {
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            StorageDirectory = _tempRoot
        };

        _runtimeController = new RuntimeController(
            _ownershipStore,
            _processValidator,
            _bridgeRuntime,
            _tunnelService,
            _hostLauncher,
            options);
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
    public async Task StartCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new StartCommand(_runtimeController, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(tunnel: false, force: false, json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task StartCommand_StartsRuntimeSuccessfully_Returns0()
    {
        await SetupWorkspaceAsync();
        var command = new StartCommand(_runtimeController, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(tunnel: false, force: false, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
        var record = await _ownershipStore.LoadAsync("ws-test");
        Assert.NotNull(record);
        Assert.Equal(12345, record.ProcessId);
    }

    [Fact]
    public async Task StartCommand_WhenAlreadyRunningAndHealthy_IsIdempotent()
    {
        await SetupWorkspaceAsync();
        var command = new StartCommand(_runtimeController, _workspaceConfigStore);

        int exit1 = await command.ExecuteAsync(tunnel: false, force: false, json: true);
        Assert.Equal(CliExitCode.Success, exit1);

        // Second call should return success idempotently without launching a new process
        _processValidator.StatusToReturn = ProcessOwnershipStatus.OwnedAndActive;
        _bridgeRuntime.StatusToReturn = BridgeStatus.Healthy;

        int exit2 = await command.ExecuteAsync(tunnel: false, force: false, json: true);
        Assert.Equal(CliExitCode.Success, exit2);
        Assert.Equal(1, _hostLauncher.LaunchCount);
    }

    [Fact]
    public async Task StartCommand_WhenPortOccupiedByForeignProcess_ReturnsConflict()
    {
        await SetupWorkspaceAsync();
        _bridgeRuntime.StatusToReturn = BridgeStatus.Conflict;

        var command = new StartCommand(_runtimeController, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(tunnel: false, force: false, json: true);

        Assert.Equal(CliExitCode.Conflict, exitCode);
        Assert.Equal(0, _hostLauncher.LaunchCount);
    }

    [Fact]
    public async Task StopCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new StopCommand(_runtimeController, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(force: false, json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task StopCommand_WhenNoRuntimeRunning_ReturnsSuccessIdempotently()
    {
        await SetupWorkspaceAsync();
        var command = new StopCommand(_runtimeController, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(force: false, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task StopCommand_WhenForeignProcessDetected_ReturnsConflict_AndDoesNotKill()
    {
        await SetupWorkspaceAsync();

        // Persist an ownership record pointing to a PID
        await _ownershipStore.SaveAsync("ws-test", new RuntimeInstanceRecord
        {
            WorkspaceId = "ws-test",
            RuntimeInstanceId = "run-1",
            ProcessId = 9999,
            ProcessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            LocalEndpoint = "http://127.0.0.1:5000",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        });

        // Validator detects foreign or reused PID
        _processValidator.StatusToReturn = ProcessOwnershipStatus.ForeignOrReused;

        var command = new StopCommand(_runtimeController, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(force: false, json: true);

        Assert.Equal(CliExitCode.Conflict, exitCode);

        // Record must remain untouched so owner state is not corrupted
        var record = await _ownershipStore.LoadAsync("ws-test");
        Assert.NotNull(record);
    }

    private async Task SetupWorkspaceAsync()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-test"),
            CanonicalRoot = _tempRoot,
            Label = "Test Workspace",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workspaceConfigStore.SaveAsync(config);
    }

    private sealed class FakeOwnedProcessValidator : IOwnedProcessValidator
    {
        public ProcessOwnershipStatus StatusToReturn { get; set; } = ProcessOwnershipStatus.NotRunning;

        public ProcessOwnershipStatus ValidateOwnership(int processId, DateTimeOffset expectedStartTime, string expectedOwnershipMarker, string? expectedProcessName = null)
        {
            return StatusToReturn;
        }
    }

    private sealed class FakeBridgeRuntime : IBridgeRuntime
    {
        public BridgeStatus StatusToReturn { get; set; } = BridgeStatus.Stopped;

        public Task<BridgeHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BridgeHealth(StatusToReturn, new Uri("http://127.0.0.1:5000")));
        }

        public Task<OperationResult<Uri>> EnsureStartedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult<Uri>.Success(new Uri("http://127.0.0.1:5000")));
        }
    }

    private sealed class FakeTunnelService : ITunnelService
    {
        private TunnelSession? _session;

        public Task<OperationResult<TunnelSession>> StartTunnelAsync(CancellationToken cancellationToken = default)
        {
            return EnsureTunnelAsync(cancellationToken);
        }

        public Task<OperationResult<TunnelSession>> EnsureTunnelAsync(CancellationToken cancellationToken = default)
        {
            _session = new TunnelSession
            {
                WorkspaceId = "ws-test",
                Provider = "test",
                LocalEndpoint = new Uri("http://127.0.0.1:5000"),
                PublicUrl = new Uri("https://test.trycloudflare.com"),
                StartedAt = DateTimeOffset.UtcNow,
                OwnershipMarker = "test-marker"
            };
            return Task.FromResult(OperationResult<TunnelSession>.Success(_session));
        }

        public Task<OperationResult<bool>> StopTunnelAsync(CancellationToken cancellationToken = default)
        {
            _session = null;
            return Task.FromResult(OperationResult<bool>.Success(true));
        }

        public Task<TunnelSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_session);
        }
    }

    private sealed class FakeRuntimeHostLauncher : IRuntimeHostLauncher
    {
        private readonly FakeBridgeRuntime? _bridgeRuntime;

        public FakeRuntimeHostLauncher(FakeBridgeRuntime? bridgeRuntime = null)
        {
            _bridgeRuntime = bridgeRuntime;
        }

        public int LaunchCount { get; private set; }

        public Task<LaunchedProcessInfo> LaunchAsync(string workspaceId, Uri endpoint, CancellationToken cancellationToken = default)
        {
            LaunchCount++;
            if (_bridgeRuntime != null)
            {
                _bridgeRuntime.StatusToReturn = BridgeStatus.Healthy;
            }

            return Task.FromResult(new LaunchedProcessInfo(12345, DateTimeOffset.UtcNow, "/path/to/host"));
        }
    }
}
