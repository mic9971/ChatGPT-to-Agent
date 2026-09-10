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

public sealed class TunnelServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceContext _workspaceContext;
    private readonly JsonTunnelSessionStore _sessionStore;

    public TunnelServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_service_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _workspaceContext = new WorkspaceContext(new WorkspaceId("ws_service_test"), _tempDir);
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
            // Best effort
        }
    }

    [Fact]
    public async Task StartTunnelAsync_FirstCall_StartsAndPersistsSession()
    {
        var fakeProvider = new FakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var result = await service.StartTunnelAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, fakeProvider.StartCallCount);

        // Verify persisted in store
        var stored = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.NotNull(stored);
        Assert.Equal(result.Value.PublicUrl, stored.PublicUrl);
    }

    [Fact]
    public async Task StartTunnelAsync_HealthyExistingSession_ReusesWithoutStartingNew()
    {
        var fakeProvider = new FakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // First start
        var first = await service.StartTunnelAsync();
        Assert.True(first.IsSuccess);
        Assert.Equal(1, fakeProvider.StartCallCount);

        // Second call should reuse existing healthy session
        var second = await service.StartTunnelAsync();
        Assert.True(second.IsSuccess);
        Assert.Equal(1, fakeProvider.StartCallCount); // Still 1! Not called again!
        Assert.Equal(first.Value!.PublicUrl, second.Value!.PublicUrl);
    }

    [Fact]
    public async Task StartTunnelAsync_DeadExistingSession_ClearsAndStartsNew()
    {
        var fakeProvider = new FakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // First start
        var first = await service.StartTunnelAsync();
        Assert.True(first.IsSuccess);

        // Mark provider health as failed (simulating process exit)
        fakeProvider.HealthToReturn = TunnelHealth.Failed("EXITED", "Process exited", DateTimeOffset.UtcNow);

        // Second call should detect dead session and start new
        var second = await service.StartTunnelAsync();
        Assert.True(second.IsSuccess);
        Assert.Equal(2, fakeProvider.StartCallCount); // Called again!
    }

    [Fact]
    public async Task StartTunnelAsync_ConcurrentCalls_ConvergesToSingleSession()
    {
        var fakeProvider = new FakeTunnelProvider { DelayMs = 50 };
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // Launch 5 concurrent StartTunnel calls (BR-CON-004)
        var tasks = new Task<OperationResult<TunnelSession>>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = service.StartTunnelAsync();
        }

        var results = await Task.WhenAll(tasks);

        foreach (var res in results)
        {
            Assert.True(res.IsSuccess);
            Assert.Equal(results[0].Value!.PublicUrl, res.Value!.PublicUrl);
        }

        // Must have only started ONCE
        Assert.Equal(1, fakeProvider.StartCallCount);
    }

    [Fact]
    public async Task StartTunnelAsync_ProviderFailure_PropagatesError()
    {
        var fakeProvider = new FakeTunnelProvider
        {
            StartResultToReturn = OperationResult<TunnelSession>.Failure(
                CommonErrorCodes.TunnelStartFailed,
                "Simulated provider failure")
        };
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var result = await service.StartTunnelAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.TunnelStartFailed, result.Error!.Code);
    }

    private sealed class FakeTunnelProvider : ITunnelProvider
    {
        public string ProviderName => "fake-provider";
        public int StartCallCount { get; private set; }
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

            return OperationResult<TunnelSession>.Success(new TunnelSession
            {
                WorkspaceId = "ws_service_test",
                Provider = ProviderName,
                PublicUrl = new Uri($"https://fake-{StartCallCount}.trycloudflare.com"),
                LocalEndpoint = localEndpoint,
                ProcessId = 1000 + StartCallCount,
                StartedAt = DateTimeOffset.UtcNow,
                OwnershipMarker = "fake-marker",
                SchemaVersion = 1
            });
        }

        public Task<TunnelHealth> GetHealthAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            return Task.FromResult(HealthToReturn);
        }

        public Task StopAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
