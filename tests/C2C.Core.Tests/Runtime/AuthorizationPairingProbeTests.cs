using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Runtime;
using C2C.Infrastructure.Workspace;

namespace C2C.Core.Tests.Runtime;

public sealed class AuthorizationPairingProbeTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly IWorkspaceConfigStore _workspaceStore;
    private readonly IAuthorizationStateStore _authStore;
    private readonly AuthorizationPairingProbe _probe;

    public AuthorizationPairingProbeTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_pairingprobe_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        string configFile = Path.Combine(_tempRoot, "workspace.json");
        _workspaceStore = new JsonWorkspaceConfigStore(configFile);
        _authStore = new JsonAuthorizationStateStore(new PairingOptions { StorageDirectory = _tempRoot });
        _probe = new AuthorizationPairingProbe(_workspaceStore, _authStore);
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
    public async Task CheckPairingAsync_WhenWorkspaceNotConfigured_ReturnsNeedPairing()
    {
        var status = await _probe.CheckPairingAsync();
        Assert.Equal(PairingStatus.NeedPairing, status);
    }

    [Fact]
    public async Task CheckPairingAsync_WhenNoActiveGrants_ReturnsNeedPairing()
    {
        await _workspaceStore.SaveAsync(new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-probe"),
            CanonicalRoot = _tempRoot,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var status = await _probe.CheckPairingAsync();
        Assert.Equal(PairingStatus.NeedPairing, status);
    }

    [Fact]
    public async Task CheckPairingAsync_WhenActiveGrantExists_ReturnsPaired()
    {
        await _workspaceStore.SaveAsync(new WorkspaceConfig
        {
            WorkspaceId = new WorkspaceId("ws-probe"),
            CanonicalRoot = _tempRoot,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var grant = new AuthorizationGrant
        {
            GrantId = "grant-1",
            WorkspaceId = "ws-probe",
            ClientId = "client-1",
            Issuer = "http://127.0.0.1:5000",
            Resource = "http://127.0.0.1:5000",
            Scopes = [AuthorizationScopes.WorkspaceRead],
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _authStore.SaveGrantAsync("ws-probe", grant);

        var status = await _probe.CheckPairingAsync();
        Assert.Equal(PairingStatus.Paired, status);
    }
}
