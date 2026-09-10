using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;

namespace C2C.IntegrationTests.Authorization;

public sealed class OpenIddictResourceBindingTests : IClassFixture<OpenIddictSpikeFixture>
{
    private readonly OpenIddictSpikeFixture _fixture;

    public OpenIddictResourceBindingTests(OpenIddictSpikeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RefreshTokenFlow_WhenOfflineAccessGranted_IssuesNewTokens()
    {
        using var client = _fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Authorize with offline_access scope
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = ComputeCodeChallenge(codeVerifier);

        string authUrl = $"/connect/authorize?response_type=code" +
                         $"&client_id={OpenIddictSpikeFixture.TestClientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(OpenIddictSpikeFixture.TestRedirectUri)}" +
                         $"&code_challenge={codeChallenge}" +
                         $"&code_challenge_method=S256" +
                         $"&scope=mcp:read%20offline_access";

        var authResponse = await client.GetAsync(authUrl);
        Assert.Equal(HttpStatusCode.Redirect, authResponse.StatusCode);

        var redirectLocation = authResponse.Headers.Location!;
        var queryParams = System.Web.HttpUtility.ParseQueryString(redirectLocation.Query);
        string authCode = queryParams["code"]!;

        // 2. Exchange code for access_token + refresh_token
        var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OpenIddictSpikeFixture.TestClientId,
            ["redirect_uri"] = OpenIddictSpikeFixture.TestRedirectUri,
            ["code"] = authCode,
            ["code_verifier"] = codeVerifier
        });

        var tokenResponse = await client.PostAsync("/connect/token", tokenRequestContent);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        string json = await tokenResponse.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(json);
        string? accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
        string? refreshToken = jsonDoc.RootElement.TryGetProperty("refresh_token", out var refreshProp) ? refreshProp.GetString() : null;

        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);

        // 3. Redeem refresh token
        var refreshRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = OpenIddictSpikeFixture.TestClientId,
            ["refresh_token"] = refreshToken!
        });

        var refreshResponse = await client.PostAsync("/connect/token", refreshRequestContent);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        string refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        using var refreshDoc = JsonDocument.Parse(refreshJson);
        string? newAccessToken = refreshDoc.RootElement.GetProperty("access_token").GetString();

        Assert.NotNull(newAccessToken);

        // 4. Access protected endpoint with new token
        using var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/mcp-protected");
        protectedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

        var protectedResponse = await client.SendAsync(protectedRequest);
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    private static string GenerateCodeVerifier()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
