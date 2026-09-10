using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Tunnel;
using C2C.Infrastructure.Tunnel.Cloudflare;

namespace C2C.Core.Tests.Tunnel;

public sealed class TunnelStopTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WorkspaceContext _workspaceContext;
    private readonly JsonTunnelSessionStore _sessionStore;

    public TunnelStopTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_stop_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _workspaceContext = new WorkspaceContext(new WorkspaceId("ws_stop_test"), _tempDir);
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
    public async Task StopTunnelAsync_NoSessionExists_ReturnsSuccessIdempotently()
    {
        var fakeProvider = new TrackingFakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        var result = await service.StopTunnelAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(0, fakeProvider.StopCallCount); // Provider stop not needed when no session exists
    }

    [Fact]
    public async Task StopTunnelAsync_RepeatedCalls_SucceedsIdempotently()
    {
        var fakeProvider = new TrackingFakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // Pre-populate a session
        TunnelSession session = CreateSampleSession(12345, "marker-1");
        await _sessionStore.SaveSessionAsync(_workspaceContext.Id.Value, session);

        // First stop
        var stop1 = await service.StopTunnelAsync();
        Assert.True(stop1.IsSuccess);
        Assert.Equal(1, fakeProvider.StopCallCount);

        var storedAfterFirst = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.Null(storedAfterFirst);

        // Second stop (idempotent)
        var stop2 = await service.StopTunnelAsync();
        Assert.True(stop2.IsSuccess);
        Assert.Equal(1, fakeProvider.StopCallCount); // Did not increment because session is already gone
    }

    [Fact]
    public async Task StopTunnelAsync_ActiveOwnedProcess_StopsProcessAndClearsSession()
    {
        var fakeProvider = new TrackingFakeTunnelProvider();
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        // Start tunnel first
        var startResult = await service.StartTunnelAsync();
        Assert.True(startResult.IsSuccess);

        var activeStored = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.NotNull(activeStored);

        // Stop tunnel
        var stopResult = await service.StopTunnelAsync();
        Assert.True(stopResult.IsSuccess);
        Assert.Equal(1, fakeProvider.StopCallCount);

        // Session cleared
        var storedAfterStop = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.Null(storedAfterStop);
    }

    [Fact]
    public async Task StopAsync_ForeignPidReused_NeverKillsForeignProcessAndClearsStaleSession()
    {
        // Setup: Current test process is alive, but tunnel session recorded a different start time
        int currentPid = Process.GetCurrentProcess().Id;
        DateTimeOffset mismatchedStartTime = DateTimeOffset.UtcNow.AddHours(-5);

        var fakeValidator = new MockOwnedProcessValidator(ProcessOwnershipStatus.ForeignOrReused);
        var fakeRunner = new TrackingProcessRunner();
        var provider = new CloudflareQuickTunnelProvider(
            fakeRunner,
            _workspaceContext,
            TimeProvider.System,
            new TunnelOptions(),
            fakeValidator);

        var service = new TunnelService(provider, _sessionStore, _workspaceContext);

        // Persist stale session pointing to current PID but foreign identity
        TunnelSession staleSession = CreateSampleSession(currentPid, "old-marker", mismatchedStartTime);
        await _sessionStore.SaveSessionAsync(_workspaceContext.Id.Value, staleSession);

        // Stop tunnel
        var stopResult = await service.StopTunnelAsync();
        Assert.True(stopResult.IsSuccess);

        // Session cleared from store
        var storedAfterStop = await _sessionStore.GetSessionAsync(_workspaceContext.Id.Value);
        Assert.Null(storedAfterStop);

        // Current process must still be running (not killed!)
        using var currentProc = Process.GetProcessById(currentPid);
        Assert.False(currentProc.HasExited);
    }

    [Fact]
    public void SystemOwnedProcessValidator_ReusedPidOrMismatch_ReturnsForeignOrReused()
    {
        var validator = new SystemOwnedProcessValidator();
        int currentPid = Process.GetCurrentProcess().Id;

        // Mismatched start time by more than tolerance (5 hours ago)
        var result = validator.ValidateOwnership(
            currentPid,
            DateTimeOffset.UtcNow.AddHours(-5),
            "any-marker",
            "cloudflared");

        Assert.Equal(ProcessOwnershipStatus.ForeignOrReused, result);
    }

    [Fact]
    public void SystemOwnedProcessValidator_NonExistentPid_ReturnsNotRunning()
    {
        var validator = new SystemOwnedProcessValidator();

        // 9999999 typically does not exist
        var result = validator.ValidateOwnership(
            9999999,
            DateTimeOffset.UtcNow,
            "any-marker");

        Assert.Equal(ProcessOwnershipStatus.NotRunning, result);
    }

    [Fact]
    public async Task StopTunnelAsync_ConcurrentWithStart_SerializesViaLifecycleLock()
    {
        var fakeProvider = new TrackingFakeTunnelProvider { DelayMs = 50 };
        var service = new TunnelService(fakeProvider, _sessionStore, _workspaceContext);

        Task<OperationResult<TunnelSession>> startTask = Task.Run(() => service.StartTunnelAsync());
        Task<OperationResult<bool>> stopTask = Task.Run(async () =>
        {
            await Task.Delay(10);
            return await service.StopTunnelAsync();
        });

        await Task.WhenAll(startTask, stopTask);

        var startResult = await startTask;
        var stopResult = await stopTask;

        Assert.True(startResult.IsSuccess);
        Assert.True(stopResult.IsSuccess);
    }

    private TunnelSession CreateSampleSession(
        int processId,
        string ownershipMarker,
        DateTimeOffset? startedAt = null)
    {
        return new TunnelSession
        {
            WorkspaceId = _workspaceContext.Id.Value,
            Provider = "cloudflare-quick",
            PublicUrl = new Uri("https://sample-test.trycloudflare.com"),
            LocalEndpoint = new Uri("http://127.0.0.1:5000"),
            ProcessId = processId,
            StartedAt = startedAt ?? DateTimeOffset.UtcNow,
            OwnershipMarker = ownershipMarker,
            SchemaVersion = 1
        };
    }

    private sealed class TrackingFakeTunnelProvider : ITunnelProvider
    {
        public string ProviderName => "tracking-fake";
        public int StartCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public int DelayMs { get; set; }

        public async Task<OperationResult<TunnelSession>> StartAsync(Uri localEndpoint, CancellationToken cancellationToken)
        {
            StartCallCount++;
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            return OperationResult<TunnelSession>.Success(new TunnelSession
            {
                WorkspaceId = "ws_stop_test",
                Provider = ProviderName,
                PublicUrl = new Uri("https://tracking-test.trycloudflare.com"),
                LocalEndpoint = localEndpoint,
                ProcessId = 5555,
                StartedAt = DateTimeOffset.UtcNow,
                OwnershipMarker = "marker-fake",
                SchemaVersion = 1
            });
        }

        public Task<TunnelHealth> GetHealthAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            return Task.FromResult(TunnelHealth.Healthy(DateTimeOffset.UtcNow));
        }

        public async Task StopAsync(TunnelSession session, CancellationToken cancellationToken)
        {
            StopCallCount++;
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }
        }
    }

    private sealed class MockOwnedProcessValidator : IOwnedProcessValidator
    {
        private readonly ProcessOwnershipStatus _statusToReturn;

        public MockOwnedProcessValidator(ProcessOwnershipStatus statusToReturn)
        {
            _statusToReturn = statusToReturn;
        }

        public ProcessOwnershipStatus ValidateOwnership(
            int processId,
            DateTimeOffset expectedStartTime,
            string expectedOwnershipMarker,
            string? expectedProcessName = null)
        {
            return _statusToReturn;
        }
    }

    private sealed class TrackingProcessRunner : IOwnedProcessRunner
    {
        public IOwnedProcess Start(
            string executablePath,
            System.Collections.Generic.IReadOnlyList<string> arguments,
            string workingDirectory,
            string ownershipMarker)
        {
            throw new NotSupportedException();
        }
    }
}
