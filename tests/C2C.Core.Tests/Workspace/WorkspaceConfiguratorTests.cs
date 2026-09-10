using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceConfiguratorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _configFilePath;
    private readonly JsonWorkspaceConfigStore _configStore;
    private readonly WorkspaceConfigurator _configurator;

    public WorkspaceConfiguratorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "c2c_cfg_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _configFilePath = Path.Combine(_testDirectory, "workspace.json");
        _configStore = new JsonWorkspaceConfigStore(_configFilePath);

        _configurator = new WorkspaceConfigurator(
            new CanonicalPathResolver(),
            new Sha256WorkspaceIdentityFactory(),
            _configStore);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task ConfigureAsync_ValidWorkspace_SucceedsAndPersistsConfig()
    {
        string wsDir = Path.Combine(_testDirectory, "my_repo");
        Directory.CreateDirectory(wsDir);

        var request = new WorkspaceConfigureRequest(wsDir, Label: "My Repo");
        var result = await _configurator.ConfigureAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(Path.GetFullPath(wsDir), result.Value.CanonicalRoot);
        Assert.StartsWith("ws_", result.Value.WorkspaceId.Value);

        // Verify file was saved atomically to disk
        Assert.True(File.Exists(_configFilePath));
        var loaded = await _configStore.LoadAsync();
        Assert.NotNull(loaded);
        Assert.Equal(result.Value.WorkspaceId, loaded.WorkspaceId);
        Assert.Equal(result.Value.CanonicalRoot, loaded.CanonicalRoot);
    }

    [Fact]
    public async Task ConfigureAsync_SameWorkspaceTwice_IsIdempotent()
    {
        string wsDir = Path.Combine(_testDirectory, "my_repo");
        Directory.CreateDirectory(wsDir);

        var request = new WorkspaceConfigureRequest(wsDir);
        var result1 = await _configurator.ConfigureAsync(request);
        var result2 = await _configurator.ConfigureAsync(request);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.Value!.WorkspaceId, result2.Value!.WorkspaceId);
    }

    [Fact]
    public async Task ConfigureAsync_DifferentWorkspaceWithoutForce_FailsWithConflict()
    {
        string repo1 = Path.Combine(_testDirectory, "repo1");
        string repo2 = Path.Combine(_testDirectory, "repo2");
        Directory.CreateDirectory(repo1);
        Directory.CreateDirectory(repo2);

        var result1 = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(repo1));
        Assert.True(result1.IsSuccess);

        var result2 = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(repo2));
        Assert.True(result2.IsFailure);
        Assert.Equal(CommonErrorCodes.Conflict, result2.Error!.Code);
    }

    [Fact]
    public async Task ConfigureAsync_DifferentWorkspaceWithForce_OverwritesExistingBinding()
    {
        string repo1 = Path.Combine(_testDirectory, "repo1");
        string repo2 = Path.Combine(_testDirectory, "repo2");
        Directory.CreateDirectory(repo1);
        Directory.CreateDirectory(repo2);

        var result1 = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(repo1));
        Assert.True(result1.IsSuccess);

        var result2 = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(repo2, Force: true));
        Assert.True(result2.IsSuccess);
        Assert.Equal(Path.GetFullPath(repo2), result2.Value!.CanonicalRoot);

        var loaded = await _configStore.LoadAsync();
        Assert.Equal(result2.Value.WorkspaceId, loaded!.WorkspaceId);
    }

    [Fact]
    public async Task ConfigureAsync_NonExistingDirectory_FailsWithItemNotFound()
    {
        string nonExisting = Path.Combine(_testDirectory, "ghost");
        var result = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(nonExisting));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspaceItemNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ConfigureAsync_FileAsWorkspace_FailsWithPathDenied()
    {
        string fileAsRoot = Path.Combine(_testDirectory, "file.txt");
        File.WriteAllText(fileAsRoot, "not a dir");

        var result = await _configurator.ConfigureAsync(new WorkspaceConfigureRequest(fileAsRoot));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }
}
