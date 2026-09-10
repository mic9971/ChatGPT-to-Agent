using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class WorkspaceIdentityFactoryTests
{
    private readonly Sha256WorkspaceIdentityFactory _factory = new();

    [Fact]
    public void Create_SameCanonicalPath_ReturnsIdenticalWorkspaceId()
    {
        string path = "/Users/developer/project";
        var id1 = _factory.Create(path);
        var id2 = _factory.Create(path);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Create_DifferentPaths_ReturnDifferentWorkspaceIds()
    {
        var id1 = _factory.Create("/Users/developer/project_a");
        var id2 = _factory.Create("/Users/developer/project_b");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Create_ValidFormat_StartsWithPrefixAndDoesNotContainRawPath()
    {
        string rawPath = "/Secret/Company/Internal/Repo";
        var id = _factory.Create(rawPath);

        Assert.StartsWith("ws_", id.Value);
        Assert.False(id.Value.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.False(id.Value.Contains("Repo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(19, id.Value.Length); // "ws_" (3) + 16 hex chars = 19
    }
}
