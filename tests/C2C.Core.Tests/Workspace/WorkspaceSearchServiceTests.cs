using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceSearchServiceTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly WorkspaceAccessPolicy _accessPolicy;
    private readonly ManagedSearchBackend _backend;
    private readonly WorkspaceSearchService _service;

    public WorkspaceSearchServiceTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_search_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_searchtest"), _tempWorkspace);
        _accessPolicy = new WorkspaceAccessPolicy(
            new CanonicalPathResolver(),
            new SensitivePathPolicy(),
            ctx => new IgnorePolicy(["*.ignored", "ignored_dir/"]));

        _backend = new ManagedSearchBackend(_accessPolicy);
        _service = new WorkspaceSearchService(_accessPolicy, _backend, new WorkspaceSearchOptions
        {
            DefaultLimit = 5,
            MaxLimit = 20,
            MaxSnippetLength = 100,
            Timeout = TimeSpan.FromSeconds(5)
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
    public async Task SearchAsync_MatchingQuery_ReturnsMatchesWithLineAndSnippet()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "alpha.txt"), "first line\nneedle in alpha\nthird line");
        File.WriteAllText(Path.Combine(_tempWorkspace, "beta.txt"), "first line\nsecond line\nneedle in beta");

        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("needle"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Count);

        var match1 = result.Value.Items[0];
        Assert.Equal("alpha.txt", match1.RelativePath);
        Assert.Equal(2, match1.LineNumber);
        Assert.Equal("needle in alpha", match1.Snippet);

        var match2 = result.Value.Items[1];
        Assert.Equal("beta.txt", match2.RelativePath);
        Assert.Equal(3, match2.LineNumber);
        Assert.Equal("needle in beta", match2.Snippet);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_EmptyOrWhitespaceQuery_FailsWithInvalidArgument(string query)
    {
        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest(query));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.InvalidArgument, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_QueryExceedingMaxLength_FailsWithInvalidArgument()
    {
        string longQuery = new('x', 300);
        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest(longQuery));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.InvalidArgument, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_PathScope_RestrictsMatchesToScope()
    {
        string subDir = Path.Combine(_tempWorkspace, "src");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "inside.txt"), "target word inside scope");
        File.WriteAllText(Path.Combine(_tempWorkspace, "outside.txt"), "target word outside scope");

        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("target word", PathScope: "src"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("src/inside.txt", result.Value.Items[0].RelativePath);
    }

    [Fact]
    public async Task SearchAsync_PreDisclosureFiltering_NeverSearchesSensitiveOrIgnoredFiles()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "allowed.txt"), "SECRET_TOKEN inside allowed file");
        File.WriteAllText(Path.Combine(_tempWorkspace, ".env"), "SECRET_TOKEN inside env");
        File.WriteAllText(Path.Combine(_tempWorkspace, "server.key"), "SECRET_TOKEN inside key");
        File.WriteAllText(Path.Combine(_tempWorkspace, "temp.ignored"), "SECRET_TOKEN inside ignored");

        Directory.CreateDirectory(Path.Combine(_tempWorkspace, ".ssh"));
        File.WriteAllText(Path.Combine(_tempWorkspace, ".ssh", "id_rsa"), "SECRET_TOKEN inside ssh");

        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("SECRET_TOKEN"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("allowed.txt", result.Value.Items[0].RelativePath);
    }

    [Fact]
    public async Task SearchAsync_SymlinkEscape_NeverSearchedOrMatched()
    {
        string outsideDir = Path.Combine(Path.GetTempPath(), "c2c_outside_search_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        string outsideFile = Path.Combine(outsideDir, "leak.txt");
        File.WriteAllText(outsideFile, "findme in outside file");

        File.WriteAllText(Path.Combine(_tempWorkspace, "regular.txt"), "findme in regular file");

        try
        {
            string symlinkPath = Path.Combine(_tempWorkspace, "escape_link.txt");
            File.CreateSymbolicLink(symlinkPath, outsideFile);

            var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("findme"));

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.Items);
            Assert.Equal("regular.txt", result.Value.Items[0].RelativePath);
        }
        catch (IOException)
        {
            // If system privileges prevent symlink creation
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
    public async Task SearchAsync_BinaryFile_SafelySkipped()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "text.txt"), "binary_target in clean text");

        // Binary file with NUL byte and the search string
        byte[] binaryData = [0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x62, 0x69, 0x6E, 0x61, 0x72, 0x79, 0x5F, 0x74, 0x61, 0x72, 0x67, 0x65, 0x74];
        File.WriteAllBytes(Path.Combine(_tempWorkspace, "binary.dat"), binaryData);

        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("binary_target"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);
        Assert.Equal("text.txt", result.Value.Items[0].RelativePath);
    }

    [Fact]
    public async Task SearchAsync_DeterministicSorting_OrdersByPathAndLineNumber()
    {
        File.WriteAllText(Path.Combine(_tempWorkspace, "z.txt"), "line 1 match\nline 2 match");
        File.WriteAllText(Path.Combine(_tempWorkspace, "a.txt"), "line 1 match\nline 2 match");

        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("match"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(4, result.Value.Items.Count);

        Assert.Equal("a.txt", result.Value.Items[0].RelativePath);
        Assert.Equal(1, result.Value.Items[0].LineNumber);

        Assert.Equal("a.txt", result.Value.Items[1].RelativePath);
        Assert.Equal(2, result.Value.Items[1].LineNumber);

        Assert.Equal("z.txt", result.Value.Items[2].RelativePath);
        Assert.Equal(1, result.Value.Items[2].LineNumber);

        Assert.Equal("z.txt", result.Value.Items[3].RelativePath);
        Assert.Equal(2, result.Value.Items[3].LineNumber);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsSlicedMatchesAndNextCursor()
    {
        List<string> lines = [];
        for (int i = 1; i <= 9; i++)
        {
            lines.Add($"keyword line {i:D2}");
        }
        File.WriteAllLines(Path.Combine(_tempWorkspace, "data.txt"), lines);

        // Page 1: limit 4
        var page1 = await _service.SearchAsync(_context, new WorkspaceSearchRequest("keyword", Limit: 4));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value);
        Assert.Equal(4, page1.Value.Items.Count);
        Assert.NotNull(page1.Value.NextCursor);

        // Page 2: limit 4 with cursor
        var page2 = await _service.SearchAsync(_context, new WorkspaceSearchRequest("keyword", Cursor: page1.Value.NextCursor, Limit: 4));
        Assert.True(page2.IsSuccess);
        Assert.NotNull(page2.Value);
        Assert.Equal(4, page2.Value.Items.Count);
        Assert.NotNull(page2.Value.NextCursor);

        // Page 3: final page
        var page3 = await _service.SearchAsync(_context, new WorkspaceSearchRequest("keyword", Cursor: page2.Value.NextCursor, Limit: 4));
        Assert.True(page3.IsSuccess);
        Assert.NotNull(page3.Value);
        Assert.Single(page3.Value.Items);
        Assert.Null(page3.Value.NextCursor);

        // Verify all 9 lines covered without overlap
        var allLines = page1.Value.Items
            .Concat(page2.Value.Items)
            .Concat(page3.Value.Items)
            .Select(m => m.LineNumber)
            .ToList();
        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8, 9], allLines);
    }

    [Fact]
    public async Task SearchAsync_PathTraversalScope_FailsWithOutsideRoot()
    {
        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("query", PathScope: "../outside"));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public async Task SearchAsync_NonExistentScope_FailsWithItemNotFound()
    {
        var result = await _service.SearchAsync(_context, new WorkspaceSearchRequest("query", PathScope: "missing_dir"));

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspaceItemNotFound, result.Error!.Code);
    }
}
