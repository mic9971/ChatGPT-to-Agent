using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Authorization;
using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class CimdClientMetadataResolverTests
{
    [Fact]
    public async Task PreRegisteredClient_WithMatchingRedirect_Succeeds()
    {
        var resolver = new CimdClientMetadataResolver(
            preRegisteredClients: ["my-app"],
            preRegisteredRedirectUris: new Dictionary<string, string>
            {
                ["my-app"] = "https://app.example.com/callback"
            });

        ClientMetadataResult result = await resolver.ValidateClientAsync("my-app", "https://app.example.com/callback");

        Assert.True(result.IsValid);
        Assert.Equal("my-app", result.ClientId);
    }

    [Fact]
    public async Task PreRegisteredClient_WithMismatchedRedirect_Fails()
    {
        var resolver = new CimdClientMetadataResolver(
            preRegisteredClients: ["my-app"],
            preRegisteredRedirectUris: new Dictionary<string, string>
            {
                ["my-app"] = "https://app.example.com/callback"
            });

        ClientMetadataResult result = await resolver.ValidateClientAsync("my-app", "https://evil.example.com/callback");

        Assert.False(result.IsValid);
        Assert.Contains("does not match", result.ErrorMessage);
    }

    [Fact]
    public async Task CimdClient_WithHttpsScheme_Succeeds()
    {
        var resolver = new CimdClientMetadataResolver();

        ClientMetadataResult result = await resolver.ValidateClientAsync("https://client.example.com/metadata.json");

        Assert.True(result.IsValid);
        Assert.Equal("https://client.example.com/metadata.json", result.ClientId);
    }

    [Fact]
    public async Task CimdClient_WithHttpScheme_Fails()
    {
        var resolver = new CimdClientMetadataResolver();

        ClientMetadataResult result = await resolver.ValidateClientAsync("http://client.example.com/metadata.json");

        Assert.False(result.IsValid);
        Assert.Contains("HTTPS", result.ErrorMessage);
    }

    [Theory]
    [InlineData("https://localhost/metadata.json")]
    [InlineData("https://127.0.0.1/metadata.json")]
    [InlineData("https://10.0.0.1/metadata.json")]
    [InlineData("https://172.16.0.1/metadata.json")]
    [InlineData("https://192.168.1.1/metadata.json")]
    [InlineData("https://169.254.169.254/metadata.json")]
    [InlineData("https://[::1]/metadata.json")]
    public async Task CimdClient_WithSsrfForbiddenHost_Fails(string clientId)
    {
        var resolver = new CimdClientMetadataResolver();

        ClientMetadataResult result = await resolver.ValidateClientAsync(clientId);

        Assert.False(result.IsValid);
        Assert.Contains("SSRF defense", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateClientAsync_WithEmptyClientId_Fails()
    {
        var resolver = new CimdClientMetadataResolver();

        ClientMetadataResult result = await resolver.ValidateClientAsync("");

        Assert.False(result.IsValid);
    }
}
