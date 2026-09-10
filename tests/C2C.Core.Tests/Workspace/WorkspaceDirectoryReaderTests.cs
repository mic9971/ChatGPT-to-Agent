using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceDirectoryReaderTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly WorkspaceAccessPolicy _accessPolicy;
    private readonly WorkspaceDirectoryReader _reader;

    public WorkspaceDirectoryReaderTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_dir_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_dirtest"), _tempWorkspace);
        _accessPolicy = new WorkspaceAccessPolicy(
            new CanonicalPathResolver(),
            new SensitivePathPolicy(),
            ctx => new IgnorePolicy(["*.ignored", "ignored_folder/"]));

        _reader = new WorkspaceDirectoryReader(_accessPolicy, new WorkspaceDirectoryReaderOptions
        {
            DefaultLimit = 10,
            MaxLimit = 50
        });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempWorkspace))
            {
                Directory.Delete(_tempWorkspace, recursive: true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public async Task ListAsync_RootDirectory_ReturnsAllDirectChildren()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "file1.txt"), "hello");
        File.WriteAllText(Path.Combine(_tempWorkspace, "file2.txt"), "world 123");
        Directory.CreateDirectory(Path.Combine(_tempWorkspace, "subdir"));

        var result = await _reader.ListAsync(_context, ".");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Items.Count);

        var file1 = result.Value.Items.FirstOrDefault(e => e.Name == "file1.txt");
        Assert.NotNull(file1);
        Assert.False(file1.IsDirectory);
        Assert.Equal("file1.txt", file1.RelativePath);
        Assert.Equal(5, file1.SizeBytes);

        var subdir = result.Value.Items.FirstOrDefault(e => e.Name == "subdir");
        Assert.NotNull(subdir);
        Assert.True(subdir.IsDirectory);
        Assert.Equal("subdir", subdir.RelativePath);
        Assert.Null(subdir.SizeBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/")]
    [InlineData(".")]
    [InlineData(null)]
    public async Task ListAsync_EmptyOrNullOrSlashPath_TreatsAsWorkspaceRoot(string? path)
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "test.txt"), "content");

        var result = await _reader.ListAsync(_context, path);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("test.txt", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task ListAsync_SubDirectory_ReturnsSubdirectoryDirectChildrenOnly()
    {
        string subPath = Path.Combine(_tempWorkspace, "sub");
        Directory.CreateDirectory(subPath);
        File.WriteAllText(Path.Combine(subPath, "child.txt"), "child data");
        File.WriteAllText(Path.Combine(_tempWorkspace, "root.txt"), "root data");

        var result = await _reader.ListAsync(_context, "sub");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("child.txt", result.Value.Items[0].Name);
        Assert.Equal("sub/child.txt", result.Value.Items[0].RelativePath);
    }

    [Fact]
    public async Task ListAsync_PreDisclosureFiltering_ExcludesSensitiveAndIgnoredFiles()
    {
        // Safe files
        File.WriteAllText(Path.Combine(_tempWorkspace, "allowed.txt"), "ok");
        File.WriteAllText(Path.Combine(_tempWorkspace, ".env.example"), "ALLOWED=1");

        // Sensitive files & dirs
        File.WriteAllText(Path.Combine(_tempWorkspace, ".env"), "SECRET=true");
        File.WriteAllText(Path.Combine(_tempWorkspace, ".env.local"), "SECRET=true");
        File.WriteAllText(Path.Combine(_tempWorkspace, "server.key"), "PRIVATE KEY");
        File.WriteAllText(Path.Combine(_tempWorkspace, "id_rsa"), "KEY");
        Directory.CreateDirectory(Path.Combine(_tempWorkspace, ".ssh"));

        // Ignored files & dirs
        File.WriteAllText(Path.Combine(_tempWorkspace, "temp.ignored"), "ignore me");
        Directory.CreateDirectory(Path.Combine(_tempWorkspace, "ignored_folder"));

        var result = await _reader.ListAsync(_context, ".");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var names = result.Value.Items.Select(e => e.Name).ToHashSet();
        Assert.Contains("allowed.txt", names);
        Assert.Contains(".env.example", names);

        // Pre-disclosure filtering: denied names MUST NEVER appear
        Assert.DoesNotContain(".env", names);
        Assert.DoesNotContain(".env.local", names);
        Assert.DoesNotContain("server.key", names);
        Assert.DoesNotContain("id_rsa", names);
        Assert.DoesNotContain(".ssh", names);
        Assert.DoesNotContain("temp.ignored", names);
        Assert.DoesNotContain("ignored_folder", names);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task ListAsync_SymlinkEscape_SilentlyFiltered()
    {
        string outsideDir = Path.Combine(Path.GetTempPath(), "c2c_outside_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        string outsideFile = Path.Combine(outsideDir, "secret_outside.txt");
        File.WriteAllText(outsideFile, "secret");

        File.WriteAllText(Path.Combine(_tempWorkspace, "regular.txt"), "regular");

        try
        {
            string symlinkPath = Path.Combine(_tempWorkspace, "escape_link");
            File.CreateSymbolicLink(symlinkPath, outsideFile);

            var result = await _reader.ListAsync(_context, ".");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);

            var names = result.Value.Items.Select(e => e.Name).ToList();
            Assert.Contains("regular.txt", names);
            Assert.DoesNotContain("escape_link", names);
            Assert.Single(result.Value.Items);
        }
        catch (IOException)
        {
            // If system permissions prevent creating symlinks, test degrades gracefully
        }
        finally
        {
            if (Directory.Exists(outsideDir))
            {
                Directory.Delete(outsideDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ListAsync_InternalSymlink_IncludedInListing()
    {
        string targetFile = Path.Combine(_tempWorkspace, "target.txt");
        File.WriteAllText(targetFile, "target content");

        try
        {
            string symlinkPath = Path.Combine(_tempWorkspace, "link_to_target.txt");
            File.CreateSymbolicLink(symlinkPath, targetFile);

            var result = await _reader.ListAsync(_context, ".");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);

            var names = result.Value.Items.Select(e => e.Name).ToHashSet();
            Assert.Contains("target.txt", names);
            Assert.Contains("link_to_target.txt", names);
            Assert.Equal(2, result.Value.Items.Count);
        }
        catch (IOException)
        {
            // If system permissions prevent creating symlinks, test degrades gracefully
        }
    }

    [Fact]
    public async Task ListAsync_DeterministicSorting_OrdersByRelativePathOrdinalIgnoreCase()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "zebra.txt"), "z");
        File.WriteAllText(Path.Combine(_tempWorkspace, "Apple.txt"), "a");
        File.WriteAllText(Path.Combine(_tempWorkspace, "banana.txt"), "b");

        var result = await _reader.ListAsync(_context, ".");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal("Apple.txt", result.Value.Items[0].Name);
        Assert.Equal("banana.txt", result.Value.Items[1].Name);
        Assert.Equal("zebra.txt", result.Value.Items[2].Name);
    }

    [Fact]
    public async Task ListAsync_Pagination_ReturnsCorrectPagesAndNextCursor()
    {
        for (int i = 1; i <= 15; i++)
        {
            // Pad numbers so alphabetical sort matches numerical order: item_01.txt .. item_15.txt
            File.WriteAllText(Path.Combine(_tempWorkspace, $"item_{i:D2}.txt"), $"content {i}");
        }

        // Page 1: limit 5
        var page1Result = await _reader.ListAsync(_context, ".", new PageRequest(Cursor: null, Limit: 5));
        Assert.True(page1Result.IsSuccess);
        Assert.NotNull(page1Result.Value);
        Assert.Equal(5, page1Result.Value.Items.Count);
        Assert.Equal("item_01.txt", page1Result.Value.Items[0].Name);
        Assert.Equal("item_05.txt", page1Result.Value.Items[4].Name);
        Assert.NotNull(page1Result.Value.NextCursor);

        // Page 2: limit 5 with cursor
        var page2Result = await _reader.ListAsync(_context, ".", new PageRequest(Cursor: page1Result.Value.NextCursor, Limit: 5));
        Assert.True(page2Result.IsSuccess);
        Assert.NotNull(page2Result.Value);
        Assert.Equal(5, page2Result.Value.Items.Count);
        Assert.Equal("item_06.txt", page2Result.Value.Items[0].Name);
        Assert.Equal("item_10.txt", page2Result.Value.Items[4].Name);
        Assert.NotNull(page2Result.Value.NextCursor);

        // Page 3: limit 5 with cursor (final page)
        var page3Result = await _reader.ListAsync(_context, ".", new PageRequest(Cursor: page2Result.Value.NextCursor, Limit: 5));
        Assert.True(page3Result.IsSuccess);
        Assert.NotNull(page3Result.Value);
        Assert.Equal(5, page3Result.Value.Items.Count);
        Assert.Equal("item_11.txt", page3Result.Value.Items[0].Name);
        Assert.Equal("item_15.txt", page3Result.Value.Items[4].Name);
        Assert.Null(page3Result.Value.NextCursor);

        // Page 4: past the end
        var page4Result = await _reader.ListAsync(_context, ".", new PageRequest(Cursor: "cur_eyJvZmZzZXQiOjE1fQ", Limit: 5));
        Assert.True(page4Result.IsSuccess);
        Assert.NotNull(page4Result.Value);
        Assert.Empty(page4Result.Value.Items);
        Assert.Null(page4Result.Value.NextCursor);
    }

    [Fact]
    public async Task ListAsync_CorruptedCursor_FailsWithInvalidArgument()
    {
        var result = await _reader.ListAsync(_context, ".", new PageRequest(Cursor: "not_a_valid_cursor", Limit: 5));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.InvalidArgument, result.Error!.Code);
    }

    [Fact]
    public async Task ListAsync_PathTraversal_FailsWithOutsideRoot()
    {
        var result = await _reader.ListAsync(_context, "../outside_dir");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public async Task ListAsync_NonExistentDirectory_FailsWithItemNotFound()
    {
        var result = await _reader.ListAsync(_context, "does_not_exist");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspaceItemNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ListAsync_FilePassedAsDirectory_FailsWithWorkspacePathDenied()
    {
        string filePath = Path.Combine(_tempWorkspace, "regular_file.txt");
        File.WriteAllText(filePath, "content");

        var result = await _reader.ListAsync(_context, "regular_file.txt");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
        Assert.Contains("regular file", result.Error!.Message);
    }
}
