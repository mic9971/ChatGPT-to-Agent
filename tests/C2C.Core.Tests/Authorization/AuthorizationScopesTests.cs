using System.Collections.Generic;

using Xunit;

using C2C.Core.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class AuthorizationScopesTests
{
    [Fact]
    public void PredefinedScopes_AreAllowed()
    {
        Assert.True(AuthorizationScopes.IsValid(AuthorizationScopes.WorkspaceRead));
        Assert.True(AuthorizationScopes.IsValid(AuthorizationScopes.WorkspaceSearch));
        Assert.True(AuthorizationScopes.IsValid(AuthorizationScopes.GitRead));
        Assert.True(AuthorizationScopes.IsValid(AuthorizationScopes.ExecutionRead));
        Assert.True(AuthorizationScopes.IsValid(AuthorizationScopes.OfflineAccess));
    }

    [Theory]
    [InlineData("workspace.write")]
    [InlineData("admin")]
    [InlineData("shell.exec")]
    [InlineData("root")]
    [InlineData("")]
    public void IllegalScopes_AreRejected(string invalidScope)
    {
        Assert.False(AuthorizationScopes.IsValid(invalidScope));
    }

    [Fact]
    public void ValidateAll_WithValidScopes_ReturnsTrue()
    {
        List<string> scopes = [AuthorizationScopes.WorkspaceRead, AuthorizationScopes.GitRead];
        bool result = AuthorizationScopes.ValidateAll(scopes, out string? invalid);

        Assert.True(result);
        Assert.Null(invalid);
    }

    [Fact]
    public void ValidateAll_WithInvalidScope_ReturnsFalseAndIdentifiesScope()
    {
        List<string> scopes = [AuthorizationScopes.WorkspaceRead, "workspace.mutate"];
        bool result = AuthorizationScopes.ValidateAll(scopes, out string? invalid);

        Assert.False(result);
        Assert.Equal("workspace.mutate", invalid);
    }
}
