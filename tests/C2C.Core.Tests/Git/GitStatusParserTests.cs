using C2C.Core.Git;
using C2C.Infrastructure.Git;

using Xunit;

namespace C2C.Core.Tests.Git;

/// <summary>
/// Unit tests for <see cref="GitStatusParser"/> covering porcelain v1 -z format.
/// </summary>
public sealed class GitStatusParserTests
{
    [Fact]
    public void Parse_EmptyOutput_ReturnsEmptyList()
    {
        var result = GitStatusParser.Parse(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_ModifiedFile_ReturnsModifiedEntry()
    {
        // Porcelain v1 -z: " M src/Foo.cs\0"
        string raw = " M src/Foo.cs\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal(' ', entry.IndexStatus);
        Assert.Equal('M', entry.WorktreeStatus);
        Assert.Equal("src/Foo.cs", entry.RelativePath);
        Assert.Equal("modified", entry.StatusLabel);
        Assert.Null(entry.OriginalPath);
    }

    [Fact]
    public void Parse_AddedStagedFile_ReturnsAddedEntry()
    {
        string raw = "A  src/New.cs\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal('A', entry.IndexStatus);
        Assert.Equal("added", entry.StatusLabel);
    }

    [Fact]
    public void Parse_DeletedFile_ReturnsDeletedEntry()
    {
        string raw = "D  src/Old.cs\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal('D', entry.IndexStatus);
        Assert.Equal("deleted", entry.StatusLabel);
    }

    [Fact]
    public void Parse_UntrackedFile_ReturnsUntrackedEntry()
    {
        string raw = "?? untracked.txt\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal('?', entry.IndexStatus);
        Assert.Equal('?', entry.WorktreeStatus);
        Assert.Equal("untracked", entry.StatusLabel);
    }

    [Fact]
    public void Parse_RenamedFile_ReturnsRenameEntryWithOriginal()
    {
        // Porcelain v1 -z rename: "R  new/path.cs\0old/path.cs\0"
        string raw = "R  new/path.cs\0old/path.cs\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal('R', entry.IndexStatus);
        Assert.Equal("new/path.cs", entry.RelativePath);
        Assert.Equal("old/path.cs", entry.OriginalPath);
        Assert.Equal("renamed", entry.StatusLabel);
    }

    [Fact]
    public void Parse_MultipleEntries_ReturnsAllEntries()
    {
        string raw = " M src/A.cs\0A  src/B.cs\0D  src/C.cs\0";

        var entries = GitStatusParser.Parse(raw);

        Assert.Equal(3, entries.Count);
        Assert.Equal("src/A.cs", entries[0].RelativePath);
        Assert.Equal("src/B.cs", entries[1].RelativePath);
        Assert.Equal("src/C.cs", entries[2].RelativePath);
    }

    [Fact]
    public void Parse_MalformedTooShort_SkipsSafelyWithNoException()
    {
        // Malformed: too short to have XY + space + path
        string raw = "AB\0 M valid.cs\0";

        var entries = GitStatusParser.Parse(raw);

        // Only the valid entry should be included
        var entry = Assert.Single(entries);
        Assert.Equal("valid.cs", entry.RelativePath);
    }

    [Fact]
    public void Parse_FileNameWithSpaces_ParsesCorrectly()
    {
        string raw = " M path with spaces/file name.cs\0";

        var entries = GitStatusParser.Parse(raw);

        var entry = Assert.Single(entries);
        Assert.Equal("path with spaces/file name.cs", entry.RelativePath);
    }

    [Fact]
    public void Parse_NullOutput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GitStatusParser.Parse(null!));
    }
}
