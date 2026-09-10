using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class UnpairCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _configFile;
    private readonly IWorkspaceConfigStore _workspaceConfigStore;
    private readonly IAuthorizationStateStore _authStore;
    private readonly IPairingStore _pairingStore;
    private readonly IRefreshFamilyStore _refreshFamilyStore;
    private readonly IAuthorizationRevoker _revoker;

    public UnpairCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_unpair_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceConfigStore = new JsonWorkspaceConfigStore(_configFile);

        var pairingOptions = new PairingOptions { StorageDirectory = _tempRoot };
        _authStore = new JsonAuthorizationStateStore(pairingOptions);
        _pairingStore = new JsonPairingStore(pairingOptions);
        _refreshFamilyStore = new JsonRefreshFamilyStore(pairingOptions);

        _revoker = new AuthorizationRevoker(
            _authStore,
            _refreshFamilyStore,
            _pairingStore);
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
    public async Task UnpairCommand_WhenWorkspaceNotConfigured_ReturnsActionRequired()
    {
        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(clientId: null, all: false, json: true);

        Assert.Equal(CliExitCode.ActionRequired, exitCode);
    }

    [Fact]
    public async Task UnpairCommand_WhenNoActiveClients_ReturnsSuccessIdempotently()
    {
        await SetupWorkspaceAsync();
        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);

        int exitCode = await command.ExecuteAsync(clientId: null, all: false, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task UnpairCommand_WhenSingleClient_UnpairsDeterministically()
    {
        await SetupWorkspaceAsync();
        await AddGrantAsync("grant-1", "client-alpha");

        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(clientId: null, all: false, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);

        var activeGrants = await _authStore.GetActiveGrantsAsync("ws-unpair");
        Assert.Empty(activeGrants);
    }

    [Fact]
    public async Task UnpairCommand_WithExplicitClientId_UnpairsTargetClient()
    {
        await SetupWorkspaceAsync();
        await AddGrantAsync("grant-1", "client-alpha");
        await AddGrantAsync("grant-2", "client-beta");

        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(clientId: "client-alpha", all: false, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);

        var activeGrants = await _authStore.GetActiveGrantsAsync("ws-unpair");
        Assert.Single(activeGrants);
        Assert.Equal("client-beta", activeGrants[0].ClientId);
    }

    [Fact]
    public async Task UnpairCommand_WithAllFlag_RevokesAllClientsAndClearsSession()
    {
        await SetupWorkspaceAsync();
        await AddGrantAsync("grant-1", "client-alpha");
        await AddGrantAsync("grant-2", "client-beta");

        // Save active pairing session
        await _pairingStore.SaveSessionAsync("ws-unpair", new PairingSession
        {
            PairingSessionId = "sess-1",
            WorkspaceId = "ws-unpair",
            CodeHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = PairingSessionStatus.Active
        });

        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(clientId: null, all: true, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);

        var activeGrants = await _authStore.GetActiveGrantsAsync("ws-unpair");
        Assert.Empty(activeGrants);

        var session = await _pairingStore.GetSessionAsync("ws-unpair");
        Assert.Null(session);
    }

    [Fact]
    public async Task UnpairCommand_WhenMultipleClientsWithoutSelector_ReturnsInvalidUsage2()
    {
        await SetupWorkspaceAsync();
        await AddGrantAsync("grant-1", "client-alpha");
        await AddGrantAsync("grant-2", "client-beta");

        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);
        int exitCode = await command.ExecuteAsync(clientId: null, all: false, json: true);

        Assert.Equal(CliExitCode.InvalidUsageOrConfig, exitCode);

        // Grants should remain untouched
        var activeGrants = await _authStore.GetActiveGrantsAsync("ws-unpair");
        Assert.Equal(2, activeGrants.Count);
    }

    [Fact]
    public async Task UnpairCommand_WhenCancelled_ReturnsTimeoutOrCancelled7()
    {
        await SetupWorkspaceAsync();
        var command = new UnpairCommand(_revoker, _authStore, _pairingStore, _workspaceConfigStore);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await command.ExecuteAsync(clientId: null, all: false, json: true, cancellationToken: cts.Token);

        Assert.Equal(CliExitCode.TimeoutOrCancelled, exitCode);
    }

    private async Task SetupWorkspaceAsync()
    {
        var config = new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-unpair"),
            CanonicalRoot = _tempRoot,
            Label = "Unpair Test Workspace",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _workspaceConfigStore.SaveAsync(config);
    }

    private async Task AddGrantAsync(string grantId, string clientId)
    {
        var grant = new AuthorizationGrant
        {
            GrantId = grantId,
            WorkspaceId = "ws-unpair",
            ClientId = clientId,
            Issuer = "http://127.0.0.1:5000",
            Resource = "http://127.0.0.1:5000",
            Scopes = [AuthorizationScopes.WorkspaceRead],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _authStore.SaveGrantAsync("ws-unpair", grant);
    }
}
