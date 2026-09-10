using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceAccessPolicyTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WorkspaceContext _context;
    private readonly WorkspaceAccessPolicy _accessPolicy;

    public WorkspaceAccessPolicyTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_access_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _context = new WorkspaceContext(new WorkspaceId("ws_test123"), _tempWorkspace);
        _accessPolicy = new WorkspaceAccessPolicy(
            new CanonicalPathResolver(),
            new SensitivePathPolicy(),
            ctx => new IgnorePolicy(["ignored_dir/", "*.tmp"]));
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
    public void EvaluatePath_ValidFile_ReturnsSuccessWithCorrectMetadata()
    {
        string filePath = Path.Combine(_tempWorkspace, "sample.txt");
        File.WriteAllText(filePath, "test content");

        var result = _accessPolicy.EvaluatePath(_context, "sample.txt");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("sample.txt", result.Value.RelativePath);
        Assert.True(result.Value.Exists);
        Assert.False(result.Value.IsDirectory);
    }

    [Fact]
    public void EvaluatePath_ValidDirectory_ReturnsSuccessWithIsDirectoryTrue()
    {
        string dirPath = Path.Combine(_tempWorkspace, "docs");
        Directory.CreateDirectory(dirPath);

        var result = _accessPolicy.EvaluatePath(_context, "docs");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Exists);
        Assert.True(result.Value.IsDirectory);
    }

    [Fact]
    public void EvaluatePath_NonExistingFile_ReturnsSuccessWithExistsFalse()
    {
        var result = _accessPolicy.EvaluatePath(_context, "future.txt");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("future.txt", result.Value.RelativePath);
        Assert.False(result.Value.Exists);
    }

    [Fact]
    public void EvaluatePath_DirectoryTraversal_ReturnsOutsideRootError()
    {
        var result = _accessPolicy.EvaluatePath(_context, "../escape.txt");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public void EvaluatePath_SensitiveEnvFile_ReturnsSensitiveContentDenied()
    {
        string envPath = Path.Combine(_tempWorkspace, ".env");
        File.WriteAllText(envPath, "SECRET=123");

        var result = _accessPolicy.EvaluatePath(_context, ".env");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.SensitiveContentDenied, result.Error!.Code);
    }

    [Fact]
    public void EvaluatePath_SafeEnvExample_Succeeds()
    {
        string examplePath = Path.Combine(_tempWorkspace, ".env.example");
        File.WriteAllText(examplePath, "SECRET=sample");

        var result = _accessPolicy.EvaluatePath(_context, ".env.example");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(".env.example", result.Value.RelativePath);
    }

    [Fact]
    public void EvaluatePath_PrivateKey_ReturnsSensitiveContentDenied()
    {
        string keyPath = Path.Combine(_tempWorkspace, "server.key");
        File.WriteAllText(keyPath, "PRIVATE KEY");

        var result = _accessPolicy.EvaluatePath(_context, "server.key");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.SensitiveContentDenied, result.Error!.Code);
    }

    [Fact]
    public void EvaluatePath_IgnoredByPolicy_ReturnsWorkspacePathDenied()
    {
        string tmpFile = Path.Combine(_tempWorkspace, "test.tmp");
        File.WriteAllText(tmpFile, "temp");

        var result = _accessPolicy.EvaluatePath(_context, "test.tmp");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }
}
