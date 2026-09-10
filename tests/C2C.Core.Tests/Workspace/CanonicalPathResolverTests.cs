using C2C.Core.Common;
using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class CanonicalPathResolverTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly CanonicalPathResolver _resolver = new();

    public CanonicalPathResolverTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_test_ws_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);
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
            // Ignore cleanup errors in tests
        }
    }

    [Fact]
    public void ResolveCanonicalRoot_ValidDirectory_Succeeds()
    {
        var result = _resolver.ResolveCanonicalRoot(_tempWorkspace);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.EndsWith('/') || result.Value.EndsWith('\\'));
    }

    [Fact]
    public void ResolveCanonicalRoot_NonExistingDirectory_FailsWithItemNotFound()
    {
        string nonExisting = Path.Combine(_tempWorkspace, "does_not_exist");
        var result = _resolver.ResolveCanonicalRoot(nonExisting);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspaceItemNotFound, result.Error!.Code);
    }

    [Fact]
    public void ResolveCanonicalRoot_FileAsRoot_FailsWithPathDenied()
    {
        string filePath = Path.Combine(_tempWorkspace, "some_file.txt");
        File.WriteAllText(filePath, "content");

        var result = _resolver.ResolveCanonicalRoot(filePath);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_ValidFile_Succeeds()
    {
        string filePath = Path.Combine(_tempWorkspace, "hello.txt");
        File.WriteAllText(filePath, "hello world");

        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "hello.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.GetFullPath(filePath), result.Value);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_NestedValidFile_Succeeds()
    {
        string subDir = Path.Combine(_tempWorkspace, "src", "nested");
        Directory.CreateDirectory(subDir);
        string filePath = Path.Combine(subDir, "code.cs");
        File.WriteAllText(filePath, "// code");

        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "src/nested/code.cs");

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.GetFullPath(filePath), result.Value);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_InTreeDotDot_SucceedsWithinRoot()
    {
        string subDir = Path.Combine(_tempWorkspace, "src");
        Directory.CreateDirectory(subDir);
        string rootFile = Path.Combine(_tempWorkspace, "root.txt");
        File.WriteAllText(rootFile, "root content");

        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "src/../root.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.GetFullPath(rootFile), result.Value);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_DirectoryTraversalEscape_FailsWithOutsideRoot()
    {
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "../outside.txt");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_DeepDirectoryTraversalEscape_FailsWithOutsideRoot()
    {
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "a/b/../../../../outside.txt");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_AbsolutePathOutsideWorkspace_FailsWithOutsideRoot()
    {
        string outside = OperatingSystem.IsWindows() ? @"C:\Windows\System32\drivers\etc\hosts" : "/etc/passwd";
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, outside);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_NullByte_FailsWithPathDenied()
    {
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "file.txt\0.other");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_UriScheme_FailsWithPathDenied()
    {
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "file:///etc/passwd");

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.WorkspacePathDenied, result.Error!.Code);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_SymlinkEscapingWorkspace_FailsWithOutsideRoot()
    {
        // Create an outside target directory/file
        string outsideDir = Path.Combine(Path.GetTempPath(), "c2c_outside_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outsideDir);
        string secretFile = Path.Combine(outsideDir, "secret.txt");
        File.WriteAllText(secretFile, "classified");

        try
        {
            // Create a symlink inside the workspace pointing outside
            string linkPath = Path.Combine(_tempWorkspace, "leak_symlink");
            try
            {
                File.CreateSymbolicLink(linkPath, secretFile);
            }
            catch (Exception)
            {
                // If the OS environment prevents symlink creation (e.g. non-admin Windows without developer mode),
                // skip this specific test gracefully
                return;
            }

            var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "leak_symlink");

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
    public void ResolveWorkspaceRelativePath_SymlinkInsideWorkspace_Succeeds()
    {
        string targetFile = Path.Combine(_tempWorkspace, "real_target.txt");
        File.WriteAllText(targetFile, "target content");

        string linkPath = Path.Combine(_tempWorkspace, "internal_link.txt");
        try
        {
            File.CreateSymbolicLink(linkPath, targetFile);
        }
        catch (Exception)
        {
            return;
        }

        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "internal_link.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.GetFullPath(targetFile), result.Value);
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_PrefixMatchCollision_DeniedOutsideBoundary()
    {
        // For example, if workspace is /tmp/workspace, candidate is /tmp/workspace_fake/secret.txt
        string parentDir = Path.GetDirectoryName(_tempWorkspace)!;
        string fakeDir = _tempWorkspace + "_fake";
        Directory.CreateDirectory(fakeDir);
        string fileInFake = Path.Combine(fakeDir, "secret.txt");
        File.WriteAllText(fileInFake, "leak");

        try
        {
            // Try relative traversal that targets the prefix-colliding directory
            string relativeAttempt = "../" + Path.GetFileName(fakeDir) + "/secret.txt";
            var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, relativeAttempt);

            Assert.True(result.IsFailure);
            Assert.Equal(CommonErrorCodes.WorkspacePathOutsideRoot, result.Error!.Code);
        }
        finally
        {
            if (Directory.Exists(fakeDir))
            {
                Directory.Delete(fakeDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveWorkspaceRelativePath_NonExistingFile_ResolvesNormalizedPathInsideWorkspace()
    {
        var result = _resolver.ResolveWorkspaceRelativePath(_tempWorkspace, "new_folder/future_file.txt");

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(_tempWorkspace, "new_folder", "future_file.txt"), result.Value);
    }
}
