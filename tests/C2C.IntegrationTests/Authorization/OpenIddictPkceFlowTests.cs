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

public sealed class OpenIddictPkceFlowTests : IClassFixture<OpenIddictSpikeFixture>
{
    private readonly OpenIddictSpikeFixture _fixture;

    public OpenIddictPkceFlowTests(OpenIddictSpikeFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FullAuthorizationCodeFlow_WithPkceS256_SucceedsAndAccessesProtectedEndpoint()
    {
        using var client = _fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Generate PKCE S256 code verifier and code challenge
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = ComputeCodeChallenge(codeVerifier);

        // 2. Request authorization code
        string authUrl = $"/connect/authorize?response_type=code" +
                         $"&client_id={OpenIddictSpikeFixture.TestClientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(OpenIddictSpikeFixture.TestRedirectUri)}" +
                         $"&code_challenge={codeChallenge}" +
                         $"&code_challenge_method=S256" +
                         $"&scope=mcp:read";

        var authResponse = await client.GetAsync(authUrl);

        Assert.Equal(HttpStatusCode.Redirect, authResponse.StatusCode);
        var redirectLocation = authResponse.Headers.Location;
        Assert.NotNull(redirectLocation);

        // Verify RFC 9207 issuer parameter returned in redirect URL
        string query = redirectLocation.Query;
        var queryParams = System.Web.HttpUtility.ParseQueryString(query);
        string? authCode = queryParams["code"];
        string? iss = queryParams["iss"];

        Assert.False(string.IsNullOrWhiteSpace(authCode), "Authorization code must be present in redirect.");
        Assert.Equal(OpenIddictSpikeFixture.ExpectedIssuer, iss);

        // 3. Exchange authorization code + code_verifier for access token
        var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OpenIddictSpikeFixture.TestClientId,
            ["redirect_uri"] = OpenIddictSpikeFixture.TestRedirectUri,
            ["code"] = authCode!,
            ["code_verifier"] = codeVerifier
        });

        var tokenResponse = await client.PostAsync("/connect/token", tokenRequestContent);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        string json = await tokenResponse.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(json);
        string? accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
        string? tokenType = jsonDoc.RootElement.GetProperty("token_type").GetString();

        Assert.NotNull(accessToken);
        Assert.Equal("Bearer", tokenType);

        // 4. Access protected MCP endpoint with the issued token
        using var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/mcp-protected");
        protectedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var protectedResponse = await client.SendAsync(protectedRequest);
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);

        // 5. Test single-use constraint: replaying the same authorization code must fail
        var replayResponse = await client.PostAsync("/connect/token", tokenRequestContent);
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);
    }

    [Fact]
    public async Task TokenExchange_WithInvalidCodeVerifier_FailsPkceCheck()
    {
        using var client = _fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = ComputeCodeChallenge(codeVerifier);

        string authUrl = $"/connect/authorize?response_type=code" +
                         $"&client_id={OpenIddictSpikeFixture.TestClientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(OpenIddictSpikeFixture.TestRedirectUri)}" +
                         $"&code_challenge={codeChallenge}" +
                         $"&code_challenge_method=S256";

        var authResponse = await client.GetAsync(authUrl);
        var redirectLocation = authResponse.Headers.Location!;
        var queryParams = System.Web.HttpUtility.ParseQueryString(redirectLocation.Query);
        string authCode = queryParams["code"]!;

        // Attempt token exchange with an incorrect code_verifier
        var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OpenIddictSpikeFixture.TestClientId,
            ["redirect_uri"] = OpenIddictSpikeFixture.TestRedirectUri,
            ["code"] = authCode,
            ["code_verifier"] = "this_verifier_is_completely_wrong_and_must_fail_0123456789"
        });

        var tokenResponse = await client.PostAsync("/connect/token", tokenRequestContent);

        // OpenIddict server built-in PKCE validation rejects the request
        Assert.Equal(HttpStatusCode.BadRequest, tokenResponse.StatusCode);
    }

    [Fact]
    public async Task AuthorizeRequest_WithoutPkceChallenge_IsRejected()
    {
        using var client = _fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Request without code_challenge
        string authUrl = $"/connect/authorize?response_type=code" +
                         $"&client_id={OpenIddictSpikeFixture.TestClientId}" +
                         $"&redirect_uri={Uri.EscapeDataString(OpenIddictSpikeFixture.TestRedirectUri)}";

        var authResponse = await client.GetAsync(authUrl);

        // When RequireProofKeyForCodeExchange() is enabled, missing PKCE is rejected
        Assert.Equal(HttpStatusCode.BadRequest, authResponse.StatusCode);
    }

    private static string GenerateCodeVerifier()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
