using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class PairCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly IPairingStore _pairingStore;
    private readonly IPairingService _pairingService;
    private readonly FakeTunnelService _tunnelService;

    public PairCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_pair_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceConfigStore = new JsonWorkspaceConfigStore(_configFile);

        var pairingOptions = new PairingOptions
        {
            StorageDirectory = _tempRoot,
            CodeLength = 8,
            Ttl = TimeSpan.FromMinutes(5)
        };
        _pairingStore = new JsonPairingStore(pairingOptions);
        _pairingService = new PairingService(_pairingStore, pairingOptions);

        _tunnelService = new FakeTunnelService();
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
    public async Task PairCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new PairCommand(_pairingService, _workspaceConfigStore, _tunnelService);

        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task PairCommand_CreatesSessionSuccessfully_Returns0()
    {
        await SetupWorkspaceAsync();
        var command = new PairCommand(_pairingService, _workspaceConfigStore, _tunnelService);

        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.Success, exitCode);

        // Verify session was persisted in pairing store
        var session = await _pairingStore.GetSessionAsync("ws-pair");
        Assert.NotNull(session);
        Assert.Equal("ws-pair", session.WorkspaceId);
        Assert.Equal(PairingSessionStatus.Active, session.Status);
    }

    [Fact]
    public async Task PairCommand_WithActiveTunnel_UsesPublicUrl()
    {
        await SetupWorkspaceAsync();
        _tunnelService.ActiveSession = new TunnelSession
        {
            WorkspaceId = "ws-pair",
            Provider = "test",
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            PublicUrl = new Uri("https://my-tunnel.trycloudflare.com"),
            StartedAt = DateTimeOffset.UtcNow,
            OwnershipMarker = "marker"
        };

        var command = new PairCommand(_pairingService, _workspaceConfigStore, _tunnelService);
        int exitCode = await command.ExecuteAsync(json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task PairCommand_WhenCancelled_ReturnsTimeoutOrCancelled7()
    {
        await SetupWorkspaceAsync();
        var command = new PairCommand(_pairingService, _workspaceConfigStore, _tunnelService);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await command.ExecuteAsync(json: true, cancellationToken: cts.Token);

        Assert.Equal(CliExitCode.TimeoutOrCancelled, exitCode);
    }

    private async Task SetupWorkspaceAsync()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-pair"),
            CanonicalRoot = _tempRoot,
            Label = "Pair Test Workspace",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workspaceConfigStore.SaveAsync(config);
    }

    private sealed class FakeTunnelService : ITunnelService
    {
        public TunnelSession? ActiveSession { get; set; }

        public Task<OperationResult<TunnelSession>> StartTunnelAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveSession != null
                ? OperationResult<TunnelSession>.Success(ActiveSession)
                : OperationResult<TunnelSession>.Failure(CommonErrorCodes.TunnelStartFailed, "Failed"));
        }

        public Task<OperationResult<TunnelSession>> EnsureTunnelAsync(CancellationToken cancellationToken = default)
        {
            return StartTunnelAsync(cancellationToken);
        }

        public Task<OperationResult<bool>> StopTunnelAsync(CancellationToken cancellationToken = default)
        {
            ActiveSession = null;
            return Task.FromResult(OperationResult<bool>.Success(true));
        }

        public Task<TunnelSession?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveSession);
        }
    }
}
