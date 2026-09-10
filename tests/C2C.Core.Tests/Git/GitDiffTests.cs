using System.Diagnostics;

using C2C.Core.Common;
using C2C.Core.Git;
using C2C.Core.Workspace;
using C2C.Infrastructure.Git;
using C2C.Infrastructure.Workspace;

using Xunit;

namespace C2C.Core.Tests.Git;

/// <summary>
/// Tests for <see cref="GitReader.GetDiffAsync"/> — UC-GIT-02.
/// Verifies the two-stage design: paths are policy-filtered before diff bodies are fetched.
/// </summary>
public sealed class GitDiffTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly GitReader _reader;

    public GitDiffTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_diff_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_diff_test"), _tempWorkspace);

        var canonicalResolver = new CanonicalPathResolver();
        var sensitivePolicy = new SensitivePathPolicy();
        var accessPolicy = new WorkspaceAccessPolicy(canonicalResolver, sensitivePolicy);
        var gitProcess = new GitProcess();

        _reader = new GitReader(gitProcess, accessPolicy);

        RunGit("init");
        RunGit("config", "user.email", "test@c2c.test");
        RunGit("config", "user.name", "C2C Test");
    }

    [Fact]
    public async Task GetDiffAsync_NoChanges_ReturnsEmptyEntries()
    {
        RunGit("commit", "--allow-empty", "-m", "init");

        var result = await _reader.GetDiffAsync(_context, new GitDiffRequest());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Entries);
        Assert.Null(result.Value!.NextCursor);
    }

    [Fact]
    public async Task GetDiffAsync_AllowedFile_ReturnsDiffBody()
    {
        string file = Path.Combine(_tempWorkspace, "source.cs");
        await File.WriteAllTextAsync(file, "// before\n");
        RunGit("add", ".");
        RunGit("commit", "-m", "init");

        await File.WriteAllTextAsync(file, "// after\n");

        var result = await _reader.GetDiffAsync(_context, new GitDiffRequest());

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(result.Value!.Entries);
        Assert.Equal("source.cs", entry.RelativePath);
        Assert.Contains("after", entry.DiffBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDiffAsync_SensitiveFilePlusAllowedFile_SensitiveBodyNeverReturned()
    {
        // Adversarial test: .env + normal file changed; .env must not appear in response
        string safeFile = Path.Combine(_tempWorkspace, "safe.cs");
        string envFile = Path.Combine(_tempWorkspace, ".env");

        await File.WriteAllTextAsync(safeFile, "// v1\n");
        RunGit("add", "safe.cs");
        RunGit("commit", "-m", "init");

        // Modify both files
        await File.WriteAllTextAsync(safeFile, "// v2\n");
        await File.WriteAllTextAsync(envFile, "SECRET=NEW_VALUE\n");

        var result = await _reader.GetDiffAsync(_context, new GitDiffRequest());

        Assert.True(result.IsSuccess);

        // safe.cs must have a diff body
        var safeEntry = Assert.Single(result.Value!.Entries);
        Assert.Equal("safe.cs", safeEntry.RelativePath);

        // No entry for .env — body is never fetched for denied paths
        Assert.DoesNotContain(result.Value!.Entries, e =>
            e.RelativePath.EndsWith(".env", StringComparison.OrdinalIgnoreCase));

        // Denied count must reflect .env exclusion
        Assert.True(result.Value!.DeniedCount >= 1);
    }

    [Fact]
    public async Task GetDiffAsync_Pagination_ReturnsCorrectPages()
    {
        // Create and commit 3 files, then modify them so they appear as changed in git diff
        for (int i = 1; i <= 3; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_tempWorkspace, $"file{i}.txt"),
                $"initial {i}\n");
        }

        RunGit("add", ".");
        RunGit("commit", "-m", "init");

        // Modify all 3 files
        for (int i = 1; i <= 3; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_tempWorkspace, $"file{i}.txt"),
                $"changed {i}\n");
        }

        // Page 1: limit=2
        var page1 = await _reader.GetDiffAsync(_context, new GitDiffRequest { Limit = 2 });
        Assert.True(page1.IsSuccess);
        Assert.Equal(2, page1.Value!.Entries.Count);
        Assert.NotNull(page1.Value.NextCursor);

        // Page 2 using cursor
        var page2 = await _reader.GetDiffAsync(_context, new GitDiffRequest
        {
            Limit = 2,
            Cursor = page1.Value.NextCursor
        });
        Assert.True(page2.IsSuccess);
        Assert.Single(page2.Value!.Entries);
        Assert.Null(page2.Value.NextCursor);
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
