using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Tunnel;

namespace C2C.Core.Tests.Tunnel;

public sealed class RuntimeEnsurerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceContext _workspaceContext;
    private readonly JsonTunnelSessionStore _sessionStore;

    public RuntimeEnsurerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_ensure_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _workspaceContext = new WorkspaceContext(new WorkspaceId("ws_ensure_test"), _tempDir);
        _sessionStore = new JsonTunnelSessionStore(new TunnelOptions { StorageDirectory = _tempDir });
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
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task EnsureAsync_AllComponentsHealthy_ReturnsReady()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider();
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);
        var pairingProbe = new DefaultPairingProbe(PairingStatus.Paired);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService, pairingProbe);

        var result = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.Ready, result.Status);
        Assert.Equal("healthy", result.Bridge);
        Assert.Equal("healthy", result.Tunnel);
        Assert.Equal("paired", result.Pairing);
        Assert.NotNull(result.PublicUrl);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public async Task EnsureAsync_BridgePortConflict_ReturnsErrorWithConflictCode()
    {
        var fakeBridge = new FakeBridgeRuntime(
            BridgeStatus.Conflict,
            "Port 5000 occupied by foreign process.");
        var fakeProvider = new EnsureFakeTunnelProvider();
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService);

        var result = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.Error, result.Status);
        Assert.Equal("conflict", result.Bridge);
        Assert.Equal("stopped", result.Tunnel);
        Assert.Equal(CommonErrorCodes.Conflict, result.ErrorCode);
        Assert.Contains("occupied by foreign process", result.Message!);
        Assert.Equal(0, fakeProvider.StartCallCount); // Tunnel never started on conflict
    }

    [Fact]
    public async Task EnsureAsync_StaleTunnelSession_RepairsStateAndReturnsReady()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider
        {
            HealthToReturn = TunnelHealth.Degraded("PROCESS_DEAD", "Process dead", DateTimeOffset.UtcNow)
        };
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // Pre-populate stale session
        var staleSession = new TunnelSession
        {
            WorkspaceId = _workspaceContext.Id.Value,
            Provider = "cloudflare-quick",
            PublicUrl = new Uri("https://stale.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            ProcessId = 9999,
            StartedAt = DateTimeOffset.UtcNow.AddHours(-1),
            OwnershipMarker = "old-marker",
            SchemaVersion = 1
        };
        await _sessionStore.SaveSessionAsync(_workspaceContext.Id.Value, staleSession);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService);

        var result = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.Ready, result.Status);
        Assert.Equal(1, fakeProvider.StopCallCount); // Stale process was stopped
        Assert.Equal(1, fakeProvider.StartCallCount); // Fresh tunnel was started

        // Verified new public URL in store
        var stored = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.NotNull(stored);
        Assert.NotEqual("https://stale.trycloudflare.com/", stored.PublicUrl.ToString());
    }

    [Fact]
    public async Task EnsureAsync_MissingPairing_ReturnsNeedPairingWithoutFakingSuccess()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider();
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);
        var pairingProbe = new DefaultPairingProbe(PairingStatus.NeedPairing);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService, pairingProbe);

        var result = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.NeedPairing, result.Status);
        Assert.Equal("healthy", result.Bridge);
        Assert.Equal("healthy", result.Tunnel);
        Assert.Equal("need_pairing", result.Pairing);
        Assert.Equal(CommonErrorCodes.NotReady, result.ErrorCode);
        Assert.NotNull(result.PublicUrl); // Public URL is ready for pairing bootstrap
    }

    [Fact]
    public async Task EnsureAsync_TunnelStartFails_ReturnsErrorWithTunnelStartFailed()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider
        {
            StartResultToReturn = OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.TunnelStartFailed,
                "cloudflared process terminated unexpectedly.")
        };
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService);

        var result = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.Error, result.Status);
        Assert.Equal("healthy", result.Bridge);
        Assert.Equal("failed", result.Tunnel);
        Assert.Equal(CommonErrorCodes.TunnelStartFailed, result.ErrorCode);
    }

    [Fact]
    public async Task EnsureAsync_ConcurrentCalls_ConvergeToOneLiveTunnel()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider { DelayMs = 30 };
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService);

        Task<RuntimeEnsureResult> task1 = Task.Run(() => ensurer.EnsureAsync());
        Task<RuntimeEnsureResult> task2 = Task.Run(() => ensurer.EnsureAsync());
        Task<RuntimeEnsureResult> task3 = Task.Run(() => ensurer.EnsureAsync());

        await Task.WhenAll(task1, task2, task3);

        var r1 = await task1;
        var r2 = await task2;
        var r3 = await task3;

        Assert.Equal(RuntimeStatus.Ready, r1.Status);
        Assert.Equal(RuntimeStatus.Ready, r2.Status);
        Assert.Equal(RuntimeStatus.Ready, r3.Status);

        // All 3 converge to the EXACT same public URL
        Assert.Equal(r1.PublicUrl, r2.PublicUrl);
        Assert.Equal(r2.PublicUrl, r3.PublicUrl);

        // Provider started only once
        Assert.Equal(1, fakeProvider.StartCallCount);
    }

    [Fact]
    public async Task EnsureAsync_RepeatedCalls_AreSafeAndIdempotent()
    {
        var fakeBridge = new FakeBridgeRuntime(BridgeStatus.Healthy);
        var fakeProvider = new EnsureFakeTunnelProvider();
        var tunnelService = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var ensurer = new RuntimeEnsurer(fakeBridge, tunnelService);

        var first = await ensurer.EnsureAsync();
        var second = await ensurer.EnsureAsync();

        Assert.Equal(RuntimeStatus.Ready, first.Status);
        Assert.Equal(RuntimeStatus.Ready, second.Status);
        Assert.Equal(first.PublicUrl, second.PublicUrl);
        Assert.Equal(1, fakeProvider.StartCallCount); // Reused healthy session
    }

    private sealed class FakeBridgeRuntime : IBridgeRuntime
    {
        private readonly BridgeStatus _status;
        private readonly string? _message;

        public FakeBridgeRuntime(BridgeStatus status, string? message = null)
        {
            _status = status;
            _message = message;
        }

        public Task<BridgeHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BridgeHealth(_status, new Uri("http://127.0.0.1:5000"), _message));
        }

        public Task<OperationResult<Uri>> EnsureStartedAsync(CancellationToken cancellationToken = default)
        {
            if (_status == BridgeStatus.Conflict || _status == BridgeStatus.Failed)
            {
                return Task.FromResult(OperationResult<Uri>.Failure(
                    _status == BridgeStatus.Conflict ? CommonErrorCodes.Conflict : CommonErrorCodes.NotReady,
                    _message ?? "Bridge startup failed."));
            }

            return Task.FromResult(OperationResult<Uri>.Success(new Uri("http://127.0.0.1:5000")));
        }
    }

    private sealed class EnsureFakeTunnelProvider : ITunnelProvider
    {
        public string ProviderName => "ensure-fake";
        public int StartCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public int DelayMs { get; set; }
        public TunnelHealth HealthToReturn { get; set; } = TunnelHealth.Healthy(DateTimeOffset.UtcNow);
        public OperationResult<TunnelSession>? StartResultToReturn { get; set; }

        public async Task<OperationResult<TunnelSession>> StartAsync(Uri localEndpoint, CancellationToken cancellationToken)
        {
            StartCallCount++;
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            if (StartResultToReturn != null)
            {
                return StartResultToReturn;
            }

            // Once started, mark healthy
            HealthToReturn = TunnelHealth.Healthy(DateTimeOffset.UtcNow);

            return OperationResult<TunnelSession>.Success(new TunnelSession
            {
                WorkspaceId = "ws_ensure_test",
                Provider = ProviderName,
                PublicUrl = new Uri($"https://ensure-{StartCallCount}.trycloudflare.com"),
                LocalEndpoint = localEndpoint,
                ProcessId = 7000 + StartCallCount,
                StartedAt = DateTimeOffset.UtcNow,
                OwnershipMarker = "ensure-marker",
                SchemaVersion = 1
            });
        }

        public Task<TunnelHealth> GetHealthAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            return Task.FromResult(HealthToReturn);
        }

        public Task StopAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            StopCallCount++;
            return Task.CompletedTask;
        }
    }
}
