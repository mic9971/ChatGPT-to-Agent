using System.Diagnostics;

using C2C.Core.Common;
using C2C.Core.Git;
using C2C.Core.Workspace;
using C2C.Infrastructure.Git;
using C2C.Infrastructure.Workspace;

using Xunit;

namespace C2C.Core.Tests.Git;

/// <summary>
/// Unit tests for <see cref="GitReader"/> policy filtering (UC-GIT-01 acceptance criteria).
/// Uses a real temp Git repository initialized by the test setup.
/// </summary>
public sealed class GitReaderTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly GitReader _reader;

    public GitReaderTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_git_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_git_test"), _tempWorkspace);

        var canonicalResolver = new CanonicalPathResolver();
        var sensitivePolicy = new SensitivePathPolicy();
        var accessPolicy = new WorkspaceAccessPolicy(canonicalResolver, sensitivePolicy);

        var gitProcess = new GitProcess();
        _reader = new GitReader(gitProcess, accessPolicy);

        // Initialize a real git repo in the temp workspace
        RunGit("init");
        RunGit("config", "user.email", "test@c2c.test");
        RunGit("config", "user.name", "C2C Test");
    }

    [Fact]
    public async Task GetStatusAsync_NoGitRepo_ReturnsGitNotAvailable()
    {
        // Use a fresh directory with no git repo
        string noGitDir = Path.Combine(Path.GetTempPath(), "c2c_nogit_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(noGitDir);
        try
        {
            var noGitContext = new WorkspaceContext(new WorkspaceId("ws_nogit"), noGitDir);
            var result = await _reader.GetStatusAsync(noGitContext, new GitStatusRequest());

            Assert.True(result.IsFailure);
            Assert.Equal(CommonErrorCodes.GitNotAvailable, result.Error!.Code);
        }
        finally
        {
            Directory.Delete(noGitDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetStatusAsync_ModifiedFile_ReturnsEntry()
    {
        // Create initial commit then modify file
        string file = Path.Combine(_tempWorkspace, "hello.txt");
        await File.WriteAllTextAsync(file, "initial");
        RunGit("add", ".");
        RunGit("commit", "-m", "init");

        await File.WriteAllTextAsync(file, "modified");

        var result = await _reader.GetStatusAsync(_context, new GitStatusRequest());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Entries, e => e.RelativePath == "hello.txt");
    }

    [Fact]
    public async Task GetStatusAsync_SensitivePath_IsExcludedFromEntries()
    {
        // Create initial commit
        string safeFile = Path.Combine(_tempWorkspace, "safe.txt");
        await File.WriteAllTextAsync(safeFile, "safe");
        RunGit("add", ".");
        RunGit("commit", "-m", "init");

        // Modify safe file and create .env (sensitive)
        await File.WriteAllTextAsync(safeFile, "modified safe");
        string envFile = Path.Combine(_tempWorkspace, ".env");
        await File.WriteAllTextAsync(envFile, "SECRET=abc123");

        var result = await _reader.GetStatusAsync(_context, new GitStatusRequest());

        Assert.True(result.IsSuccess);

        // safe.txt must appear
        Assert.Contains(result.Value!.Entries, e => e.RelativePath == "safe.txt");

        // .env must NOT appear (sensitive path policy)
        Assert.DoesNotContain(result.Value!.Entries, e => e.RelativePath == ".env");

        // Denied count must be at least 1
        Assert.True(result.Value!.DeniedCount >= 1);
    }

    [Fact]
    public async Task GetStatusAsync_IgnoredFileViaC2CIgnore_IsExcluded()
    {
        // Create initial commit with .c2cignore
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, ".c2cignore"), "*.log\n");
        string safeFile = Path.Combine(_tempWorkspace, "code.cs");
        await File.WriteAllTextAsync(safeFile, "// init");
        RunGit("add", ".");
        RunGit("commit", "-m", "init");

        // Modify code.cs and create a .log file
        await File.WriteAllTextAsync(safeFile, "// changed");
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "output.log"), "log data");

        var result = await _reader.GetStatusAsync(_context, new GitStatusRequest());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Entries, e => e.RelativePath == "code.cs");
        Assert.DoesNotContain(result.Value!.Entries, e => e.RelativePath == "output.log");
    }

    [Fact]
    public async Task GetStatusAsync_EntriesAreSortedByPath()
    {
        RunGit("commit", "--allow-empty", "-m", "init");

        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "z_last.txt"), "z");
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "a_first.txt"), "a");

        var result = await _reader.GetStatusAsync(_context, new GitStatusRequest());

        Assert.True(result.IsSuccess);
        var paths = result.Value!.Entries.Select(e => e.RelativePath).ToList();
        var sorted = paths.OrderBy(p => p, StringComparer.Ordinal).ToList();
        Assert.Equal(sorted, paths);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempWorkspace, recursive: true); } catch { /* best-effort */ }
    }

    private void RunGit(params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = _tempWorkspace,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var p = Process.Start(psi)!;
        p.WaitForExit();
    }
}
