using System.Text;
using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceFileReaderTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly WorkspaceFileReader _reader;

    public WorkspaceFileReaderTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_read_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_reader_test"), _tempWorkspace);

        var canonicalResolver = new CanonicalPathResolver();
        var sensitivePolicy = new SensitivePathPolicy();
        var accessPolicy = new WorkspaceAccessPolicy(
            canonicalResolver,
            sensitivePolicy,
            ctx => new IgnorePolicy(["ignored_dir/", "*.tmp"]));

        _reader = new WorkspaceFileReader(
            accessPolicy,
            new WorkspaceFileReaderOptions
            {
                DefaultMaxLines = 100,
                AbsoluteMaxLines = 1000,
                DefaultMaxBytes = 4096,
                AbsoluteMaxBytes = 16384
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
    public async Task ReadTextAsync_SmallUtf8File_ReturnsFullContentAndMetadata()
    {
        string filePath = Path.Combine(_tempWorkspace, "small.txt");
        await File.WriteAllTextAsync(filePath, "Line 1\nLine 2\nLine 3");

        var request = new FileReadRequest("small.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("small.txt", result.Value.RelativePath);
        Assert.Equal("Line 1\r\nLine 2\r\nLine 3", result.Value.Content.Replace("\r\n", "\n").Replace("\n", "\r\n"));
        Assert.Equal(1, result.Value.StartLine);
        Assert.Equal(3, result.Value.EndLine);
        Assert.Equal(3, result.Value.TotalLinesRead);
        Assert.False(result.Value.IsTruncated);
        Assert.Null(result.Value.NextCursor);
        Assert.StartsWith("fp_", result.Value.Fingerprint);
    }

    [Fact]
    public async Task ReadTextAsync_UnicodeFile_CorrectlyPreservesMultiByteCharacters()
    {
        string filePath = Path.Combine(_tempWorkspace, "unicode.txt");
        string unicodeContent = "Hello 世界 🌍! C2C.NET est génial. 🚀\nSecond line with special characters: å, ö, ñ, ç";
        await File.WriteAllTextAsync(filePath, unicodeContent, Encoding.UTF8);

        var request = new FileReadRequest("unicode.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalLinesRead);
        Assert.Contains("世界 🌍", result.Value.Content);
        Assert.Contains("génial. 🚀", result.Value.Content);
        Assert.Contains("å, ö, ñ, ç", result.Value.Content);
    }

    [Fact]
    public async Task ReadTextAsync_EmptyFile_ReturnsZeroLinesAndNotTruncated()
    {
        string filePath = Path.Combine(_tempWorkspace, "empty.txt");
        await File.WriteAllTextAsync(filePath, string.Empty);

        var request = new FileReadRequest("empty.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(string.Empty, result.Value.Content);
        Assert.Equal(0, result.Value.TotalLinesRead);
        Assert.False(result.Value.IsTruncated);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task ReadTextAsync_NestedFile_Succeeds()
    {
        string dir = Path.Combine(_tempWorkspace, "src", "deep", "nested");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "nested.txt");
        await File.WriteAllTextAsync(filePath, "deep content");

        var request = new FileReadRequest("src/deep/nested/nested.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("deep content", result.Value.Content);
        Assert.Equal("src/deep/nested/nested.txt", result.Value.RelativePath);
    }

    [Fact]
    public async Task ReadTextAsync_MissingFile_ReturnsItemNotFound()
    {
        var request = new FileReadRequest("does_not_exist.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspaceItemNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_DirectoryPassedAsFile_ReturnsPathDenied()
    {
        string subDir = Path.Combine(_tempWorkspace, "some_dir");
        Directory.CreateDirectory(subDir);

        var request = new FileReadRequest("some_dir");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_RequestedLineWindow_ReturnsBoundedSlice()
    {
        string filePath = Path.Combine(_tempWorkspace, "lines.txt");
        var lines = Enumerable.Range(1, 20).Select(i => $"Line {i}");
        await File.WriteAllLinesAsync(filePath, lines);

        // Request starting from line 5, max 5 lines
        var request = new FileReadRequest("lines.txt", StartLine: 5, MaxLines: 5);
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.StartLine);
        Assert.Equal(9, result.Value.EndLine);
        Assert.Equal(5, result.Value.TotalLinesRead);
        Assert.True(result.Value.IsTruncated);
        Assert.NotNull(result.Value.NextCursor);
        Assert.Contains("Line 5", result.Value.Content);
        Assert.Contains("Line 9", result.Value.Content);
        Assert.DoesNotContain("Line 4", result.Value.Content);
        Assert.DoesNotContain("Line 10", result.Value.Content);
    }

    [Fact]
    public async Task ReadTextAsync_RequestedByteWindow_TruncatesWhenByteLimitReached()
    {
        string filePath = Path.Combine(_tempWorkspace, "bytes.txt");
        // Create 10 lines of 50 characters each
        var lines = Enumerable.Range(1, 10).Select(i => new string('A', 50));
        await File.WriteAllLinesAsync(filePath, lines);

        // MaxBytes = 120 (should fit ~2 lines)
        var request = new FileReadRequest("bytes.txt", MaxBytes: 120);
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsTruncated);
        Assert.NotNull(result.Value.NextCursor);
        Assert.True(result.Value.TotalLinesRead < 10);
    }

    [Fact]
    public async Task ReadTextAsync_OversizedHardLimit_ReturnsOutputLimitExceeded()
    {
        string filePath = Path.Combine(_tempWorkspace, "sample.txt");
        await File.WriteAllTextAsync(filePath, "content");

        var request = new FileReadRequest("sample.txt", MaxLines: 99999);
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.OutputLimitExceeded, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_BinaryFile_ReturnsBinaryContentDenied()
    {
        string filePath = Path.Combine(_tempWorkspace, "binary.dat");
        byte[] binaryData = [0x7F, 0x45, 0x4C, 0x46, 0x00, 0x01, 0x01, 0x00]; // ELF header with NUL
        await File.WriteAllBytesAsync(filePath, binaryData);

        var request = new FileReadRequest("binary.dat");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.BinaryContentDenied, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_Cancellation_PropagatesCancellation()
    {
        string filePath = Path.Combine(_tempWorkspace, "sample.txt");
        await File.WriteAllTextAsync(filePath, "content");

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _reader.ReadTextAsync(_context, new FileReadRequest("sample.txt"), cts.Token));
    }

    [Fact]
    public async Task ReadTextAsync_FileChangedBetweenReads_FailsWithConflict()
    {
        string filePath = Path.Combine(_tempWorkspace, "dynamic.txt");
        await File.WriteAllTextAsync(filePath, "Initial content line 1\nLine 2");

        var firstResult = await _reader.ReadTextAsync(_context, new FileReadRequest("dynamic.txt"));
        Assert.True(firstResult.IsSuccess);
        string initialFingerprint = firstResult.Value!.Fingerprint;

        // Modify file
        await Task.Delay(50); // Ensure timestamp advances
        await File.AppendAllTextAsync(filePath, "\nAppended line 3");

        // Try reading with previous fingerprint expectation
        var nextRequest = new FileReadRequest("dynamic.txt", StartLine: 2, ExpectedFingerprint: initialFingerprint);
        var nextResult = await _reader.ReadTextAsync(_context, nextRequest);

        Assert.True(nextResult.IsFailure);
        Assert.Equal(CommonErrorCodes.Conflict, nextResult.Error!.Code);
    }

    #region Security Regressions

    [Fact]
    public async Task ReadTextAsync_SecurityRegression_DotDotEscape_FailsWithOutsideRoot()
    {
        var request = new FileReadRequest("../secret.txt");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_SecurityRegression_SymlinkEscape_FailsWithOutsideRoot()
    {
        string outsideDir = Path.Combine(Path.GetTempPath(), "c2c_outside_reader_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        string outsideFile = Path.Combine(outsideDir, "secret.txt");
        await File.WriteAllTextAsync(outsideFile, "secret");

        try
        {
            string linkPath = Path.Combine(_tempWorkspace, "symlink_leak");
            try
            {
                File.CreateSymbolicLink(linkPath, outsideFile);
            }
            catch
            {
                return; // Symlink creation not supported
            }

            var request = new FileReadRequest("symlink_leak");
            var result = await _reader.ReadTextAsync(_context, request);

            Assert.True(result.IsFailure);
            Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
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
    public async Task ReadTextAsync_SecurityRegression_SensitiveEnv_ReturnsSensitiveContentDenied()
    {
        string envPath = Path.Combine(_tempWorkspace, ".env");
        await File.WriteAllTextAsync(envPath, "SECRET=123");

        var request = new FileReadRequest(".env");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.SensitiveContentDenied, result.Error!.Code);
    }

    [Fact]
    public async Task ReadTextAsync_SecurityRegression_AllowedEnvExample_Succeeds()
    {
        string envExamplePath = Path.Combine(_tempWorkspace, ".env.example");
        await File.WriteAllTextAsync(envExamplePath, "SECRET=sample_example");

        var request = new FileReadRequest(".env.example");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("SECRET=sample_example", result.Value.Content);
    }

    [Fact]
    public async Task ReadTextAsync_SecurityRegression_IgnoredFile_ReturnsWorkspacePathDenied()
    {
        string ignoredFile = Path.Combine(_tempWorkspace, "test.tmp");
        await File.WriteAllTextAsync(ignoredFile, "temporary content");

        var request = new FileReadRequest("test.tmp");
        var result = await _reader.ReadTextAsync(_context, request);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }

    #endregion
}
