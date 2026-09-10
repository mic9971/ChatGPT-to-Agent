using Xunit;

using C2C.Core.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class McpToolScopesTests
{
    [Theory]
    [InlineData("workspace_info", AuthorizationScopes.WorkspaceRead)]
    [InlineData("read_file", AuthorizationScopes.WorkspaceRead)]
    [InlineData("list_directory", AuthorizationScopes.WorkspaceRead)]
    [InlineData("search_workspace", AuthorizationScopes.WorkspaceSearch)]
    [InlineData("git_status", AuthorizationScopes.GitRead)]
    [InlineData("git_diff", AuthorizationScopes.GitRead)]
    [InlineData("execution_summary", AuthorizationScopes.ExecutionRead)]
    [InlineData("test_status", AuthorizationScopes.ExecutionRead)]
    [InlineData("execution_output", AuthorizationScopes.ExecutionRead)]
    public void GetRequiredScope_MapsAllNineToolsAccurately(string toolName, string expectedScope)
    {
        string? actualScope = McpToolScopes.GetRequiredScope(toolName);
        Assert.Equal(expectedScope, actualScope);
    }

    [Fact]
    public void GetRequiredScope_ForUnknownTool_ReturnsNull()
    {
        string? scope = McpToolScopes.GetRequiredScope("unknown_tool");
        Assert.Null(scope);
    }

    [Fact]
    public void AllMappings_ContainsExactlyNineApprovedTools()
    {
        Assert.Equal(9, McpToolScopes.AllMappings.Count);
    }
}
