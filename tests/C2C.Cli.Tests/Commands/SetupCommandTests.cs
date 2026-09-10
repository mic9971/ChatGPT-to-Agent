using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Tests.Commands;

public sealed class SetupCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _workspaceDir;
    private readonly string _configFile;
    private readonly IWorkspaceConfigurator _configurator;

    public SetupCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_setup_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _workspaceDir = Path.Combine(_tempRoot, "my-workspace with spaces and ünicode");
        Directory.CreateDirectory(_workspaceDir);

        _configFile = Path.Combine(_tempRoot, "c2c.workspace.json");
        var pathResolver = new CanonicalPathResolver();
        var identityFactory = new Sha256WorkspaceIdentityFactory();
        var configStore = new JsonWorkspaceConfigStore(_configFile);

        _configurator = new WorkspaceConfigurator(pathResolver, identityFactory, configStore);
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
    public async Task SetupCommand_ConfiguresWorkspace_Successfully()
    {
        var command = new SetupCommand(_configurator);

        int exitCode = await command.ExecuteAsync(
            path: _workspaceDir,
            label: "Test Label",
            force: false,
            json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.True(File.Exists(_configFile));
    }

    [Fact]
    public async Task SetupCommand_IsIdempotent_ForSameDirectory()
    {
        var command = new SetupCommand(_configurator);

        int exitCode1 = await command.ExecuteAsync(_workspaceDir, "First", false, true);
        Assert.Equal(CliExitCode.Success, exitCode1);

        // Second setup with same directory succeeds idempotently
        int exitCode2 = await command.ExecuteAsync(_workspaceDir, "Second", false, true);
        Assert.Equal(CliExitCode.Success, exitCode2);
    }

    [Fact]
    public async Task SetupCommand_ConflictingDirectoryWithoutForce_ReturnsConflictExitCode()
    {
        var command = new SetupCommand(_configurator);

        int exit1 = await command.ExecuteAsync(_workspaceDir, "First", false, true);
        Assert.Equal(CliExitCode.Success, exit1);

        // Another directory without force -> conflict
        string anotherDir = Path.Combine(_tempRoot, "another-workspace");
        Directory.CreateDirectory(anotherDir);

        int exit2 = await command.ExecuteAsync(anotherDir, "Another", false, true);
        Assert.Equal(CliExitCode.Conflict, exit2);
    }
}
