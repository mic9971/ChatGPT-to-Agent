using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class PairingServiceTests : IDisposable
{
    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public TestTimeProvider(DateTimeOffset initialTime)
        {
            _now = initialTime;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }

    private readonly string _testDir;
    private readonly JsonPairingStore _store;
    private readonly TestTimeProvider _timeProvider;
    private readonly PairingOptions _options;
    private readonly PairingService _service;

    public PairingServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "c2c_pairing_svc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _options = new PairingOptions
        {
            StorageDirectory = _testDir,
            CodeLength = 8,
            Ttl = TimeSpan.FromMinutes(5),
            MaxAttempts = 3
        };

        _store = new JsonPairingStore(_options);
        _timeProvider = new TestTimeProvider(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
        _service = new PairingService(_store, _options, _timeProvider);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignored in cleanup
            }
        }
    }

    [Fact]
    public async Task CreateSessionAsync_GeneratesCodeWithConfiguredLengthAndEntropy()
    {
        string workspaceId = "ws-entropy";
        HashSet<string> generatedCodes = [];

        for (int i = 0; i < 50; i++)
        {
            PairingCreateResult result = await _service.CreateSessionAsync(workspaceId);
            Assert.Equal(8, result.DisplayCode.Length);
            Assert.Matches("^[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]{8}$", result.DisplayCode);
            Assert.False(generatedCodes.Contains(result.DisplayCode), $"Collision detected on iteration {i}");
            generatedCodes.Add(result.DisplayCode);
        }
    }

    [Fact]
    public async Task CreateSessionAsync_PersistsHashedCodeNotPlaintext()
    {
        string workspaceId = "ws-sec-008";
        PairingCreateResult result = await _service.CreateSessionAsync(workspaceId);

        string filePath = Path.Combine(_testDir, "workspaces", workspaceId, "auth", "pairing.json");
        string json = await File.ReadAllTextAsync(filePath);

        // Assert plaintext display code is NOT anywhere in the persisted JSON
        Assert.DoesNotContain(result.DisplayCode, json);

        // Assert CodeHash is present and is 64 hex characters (SHA-256)
        PairingSession? session = await _store.GetSessionAsync(workspaceId);
        Assert.NotNull(session);
        Assert.Matches("^[A-F0-9]{64}$", session.CodeHash);
    }

    [Fact]
    public async Task ValidateAndConsumeCodeAsync_WithValidCode_SucceedsAndMarksConsumed()
    {
        string workspaceId = "ws-valid";
        PairingCreateResult created = await _service.CreateSessionAsync(workspaceId);

        PairingValidationResult result = await _service.ValidateAndConsumeCodeAsync(
            workspaceId,
            created.DisplayCode,
            clientId: "test-client-1");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Session);
        Assert.Equal(PairingSessionStatus.Consumed, result.Session.Status);
        Assert.Equal("test-client-1", result.Session.ApprovedClientId);

        // Replay of same code is rejected (Single-use per BR-AUTH-007)
        PairingValidationResult replay = await _service.ValidateAndConsumeCodeAsync(
            workspaceId,
            created.DisplayCode,
            clientId: "test-client-1");

        Assert.False(replay.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingInvalid, replay.ErrorCode);
    }

    [Fact]
    public async Task ValidateAndConsumeCodeAsync_WithWrongCode_DecrementsAttemptsAndFails()
    {
        string workspaceId = "ws-wrong-code";
        await _service.CreateSessionAsync(workspaceId);

        PairingValidationResult result = await _service.ValidateAndConsumeCodeAsync(workspaceId, "WRONGCOD");

        Assert.False(result.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingInvalid, result.ErrorCode);

        PairingSession? session = await _service.GetCurrentSessionAsync(workspaceId);
        Assert.NotNull(session);
        Assert.Equal(2, session.AttemptsRemaining);
        Assert.Equal(PairingSessionStatus.Active, session.Status);
    }

    [Fact]
    public async Task ValidateAndConsumeCodeAsync_ExhaustingAttempts_LocksSession()
    {
        string workspaceId = "ws-lockout";
        PairingCreateResult created = await _service.CreateSessionAsync(workspaceId);

        // Attempt 1
        var r1 = await _service.ValidateAndConsumeCodeAsync(workspaceId, "WRONG001");
        Assert.False(r1.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingInvalid, r1.ErrorCode);

        // Attempt 2
        var r2 = await _service.ValidateAndConsumeCodeAsync(workspaceId, "WRONG002");
        Assert.False(r2.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingInvalid, r2.ErrorCode);

        // Attempt 3 (locks session)
        var r3 = await _service.ValidateAndConsumeCodeAsync(workspaceId, "WRONG003");
        Assert.False(r3.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingLocked, r3.ErrorCode);

        // Attempt 4 even with correct code fails because session is locked
        var r4 = await _service.ValidateAndConsumeCodeAsync(workspaceId, created.DisplayCode);
        Assert.False(r4.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingLocked, r4.ErrorCode);

        PairingSession? session = await _service.GetCurrentSessionAsync(workspaceId);
        Assert.NotNull(session);
        Assert.Equal(PairingSessionStatus.Locked, session.Status);
        Assert.Equal(0, session.AttemptsRemaining);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_And_Validate_WhenTtlExpired_MarksExpired()
    {
        string workspaceId = "ws-expiry";
        PairingCreateResult created = await _service.CreateSessionAsync(workspaceId);

        // Advance time by 5 minutes and 1 second
        _timeProvider.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        PairingSession? session = await _service.GetCurrentSessionAsync(workspaceId);
        Assert.NotNull(session);
        Assert.Equal(PairingSessionStatus.Expired, session.Status);

        PairingValidationResult result = await _service.ValidateAndConsumeCodeAsync(workspaceId, created.DisplayCode);
        Assert.False(result.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingExpired, result.ErrorCode);
    }

    [Fact]
    public async Task CreateSessionAsync_RotatesPriorActiveSession()
    {
        string workspaceId = "ws-rotate";
        PairingCreateResult s1 = await _service.CreateSessionAsync(workspaceId);
        PairingCreateResult s2 = await _service.CreateSessionAsync(workspaceId);

        Assert.NotEqual(s1.PairingSessionId, s2.PairingSessionId);
        Assert.NotEqual(s1.DisplayCode, s2.DisplayCode);

        // Code from s1 is no longer valid
        var r1 = await _service.ValidateAndConsumeCodeAsync(workspaceId, s1.DisplayCode);
        Assert.False(r1.IsSuccess);

        // Code from s2 is valid
        var r2 = await _service.ValidateAndConsumeCodeAsync(workspaceId, s2.DisplayCode);
        Assert.True(r2.IsSuccess);
    }

    [Fact]
    public async Task WorkspaceIsolation_PairingSessionForWorkspaceA_CannotBeValidatedForWorkspaceB()
    {
        string wsA = "ws-a";
        string wsB = "ws-b";

        PairingCreateResult createdA = await _service.CreateSessionAsync(wsA);

        var result = await _service.ValidateAndConsumeCodeAsync(wsB, createdA.DisplayCode);
        Assert.False(result.IsSuccess);
        Assert.Equal(CommonErrorCodes.AuthPairingInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task ConcurrentValidation_OnlyOneSucceeds()
    {
        string workspaceId = "ws-concurrent";
        PairingCreateResult created = await _service.CreateSessionAsync(workspaceId);

        Task<PairingValidationResult>[] tasks = Enumerable.Range(0, 5)
            .Select(i => _service.ValidateAndConsumeCodeAsync(workspaceId, created.DisplayCode, $"client-{i}"))
            .ToArray();

        PairingValidationResult[] results = await Task.WhenAll(tasks);

        int successCount = results.Count(r => r.IsSuccess);
        int failureCount = results.Count(r => !r.IsSuccess);

        Assert.Equal(1, successCount);
        Assert.Equal(4, failureCount);
    }
}
