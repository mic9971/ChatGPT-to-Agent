using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using C2C.Core.Authorization;
using C2C.Core.Workspace;
using C2C.Host;
using C2C.Host.Auth;
using C2C.Infrastructure.Authorization;

namespace C2C.IntegrationTests.Authorization;

/// <summary>
/// End-to-end integration tests for UC-AUTH-05 (Revoke / Unpair Client) adhering to
/// BR-AUTH-006, BR-SEC-006, BR-COM-009, and 09-UC-AUTH-05-IMPLEMENTATION.md.
/// </summary>
[Collection("AuthorizationIntegration")]
public sealed class RevocationAndUnpairEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestClientId = "c2c-unpair-client";
    private const string TestRedirectUri = "https://app.example.com/callback";

    private readonly string _storageDir;
    private readonly WebApplicationFactory<Program> _factory;

    public RevocationAndUnpairEndpointTests(WebApplicationFactory<Program> factory)
    {
        _storageDir = Path.Combine(Path.GetTempPath(), "c2c-test-unpair-" + Guid.NewGuid().ToString("N"));
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:DisableTransportSecurityRequirement", "true");
            builder.UseSetting("Auth:StorageDirectory", _storageDir);
            builder.ConfigureServices(services =>
            {
                var resolver = new CimdClientMetadataResolver(
                    preRegisteredClients: [TestClientId],
                    preRegisteredRedirectUris: new Dictionary<string, string>
                    {
                        [TestClientId] = TestRedirectUri
                    });

                services.AddSingleton<IClientMetadataResolver>(resolver);
            });
        });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_storageDir))
            {
                Directory.Delete(_storageDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://127.0.0.1:5000")
        });
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/event-stream");
        return client;
    }

    private static (string Verifier, string Challenge) GeneratePkce()
    {
        byte[] verifierBytes = RandomNumberGenerator.GetBytes(32);
        string verifier = Convert.ToBase64String(verifierBytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        string challenge = Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return (verifier, challenge);
    }

    private static readonly System.Threading.SemaphoreSlim AuthorizeLock = new(1, 1);

    private async Task<(string Code, string Verifier)> AuthorizeAndGetCodeAsync(string scope)
    {
        await AuthorizeLock.WaitAsync();
        try
        {
            using var svcScope = _factory.Services.CreateScope();
            var workspaceContext = svcScope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
            var pairingService = svcScope.ServiceProvider.GetRequiredService<IPairingService>();

            var pairing = await pairingService.CreateSessionAsync(workspaceContext.Id.Value);
            var (verifier, challenge) = GeneratePkce();

            string query = $"/connect/authorize?" +
                $"response_type=code&" +
                $"client_id={TestClientId}&" +
                $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
                $"scope={Uri.EscapeDataString(scope)}&" +
                $"state=teststate&" +
                $"code_challenge={challenge}&" +
                $"code_challenge_method=S256&" +
                $"pairing_code={pairing.DisplayCode}";

            var client = CreateTestClient();
            HttpResponseMessage response = await client.GetAsync(query);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            Uri? location = response.Headers.Location;
            Assert.NotNull(location);

            var parsed = HttpUtility.ParseQueryString(location.Query);
            string code = parsed["code"] ?? throw new InvalidOperationException("Missing code in redirect.");
            return (code, verifier);
        }
        finally
        {
            AuthorizeLock.Release();
        }
    }

    private async Task<(string AccessToken, string? RefreshToken)> ObtainTokensAsync(string scope)
    {
        var (code, verifier) = await AuthorizeAndGetCodeAsync(scope);
        var client = CreateTestClient();

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        }));

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        string body = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        string accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        string? refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rf) ? rf.GetString() : null;

        return (accessToken, refreshToken);
    }

    [Fact]
    public async Task RevokeEndpoint_WithToken_RevokesGrantAndSubsequentMcpFails401Immediately()
    {
        // Arrange: Obtain valid access token
        var (accessToken, _) = await ObtainTokensAsync(AuthorizationScopes.WorkspaceRead);
        var client = CreateTestClient();

        // 1. Initial MCP request succeeds
        var mcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":1}", Encoding.UTF8, "application/json")
        };
        mcpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var initialResponse = await client.SendAsync(mcpRequest);
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);

        // 2. Call RFC 7009 revocation endpoint
        var revokeResponse = await client.PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["token_type_hint"] = "access_token"
        }));
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        // 3. Subsequent MCP request with the same token FAILS IMMEDIATELY (401 Unauthorized)
        var nextMcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":2}", Encoding.UTF8, "application/json")
        };
        nextMcpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var nextMcpResponse = await client.SendAsync(nextMcpRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, nextMcpResponse.StatusCode);
        var wwwAuth = nextMcpResponse.Headers.WwwAuthenticate.ToString();
        Assert.Contains("invalid_token", wwwAuth);
    }

    [Fact]
    public async Task RevokeEndpoint_RepeatedRevoke_IsIdempotent()
    {
        var (accessToken, _) = await ObtainTokensAsync(AuthorizationScopes.WorkspaceRead);
        var client = CreateTestClient();

        // First revoke
        var r1 = await client.PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = accessToken
        }));
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        // Second revoke of same token -> 200 OK (idempotent per RFC 7009)
        var r2 = await client.PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = accessToken
        }));
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    [Fact]
    public async Task RevokeEndpoint_WithGrantId_CausesRefreshToFail()
    {
        string scopes = $"{AuthorizationScopes.WorkspaceRead} {AuthorizationScopes.OfflineAccess}";
        var (accessToken, refreshToken) = await ObtainTokensAsync(scopes);
        Assert.NotNull(refreshToken);

        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IAuthorizationStateStore>();
        var activeGrants = await grantStore.GetActiveGrantsAsync(workspaceContext.Id.Value);
        Assert.NotEmpty(activeGrants);
        string grantId = activeGrants[0].GrantId;

        var client = CreateTestClient();

        // Revoke via grant_id
        var revokeResponse = await client.PostAsync("/connect/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_id"] = grantId
        }));
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        // Attempt to refresh token after grant revoked -> 400 invalid_grant
        var refreshResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = TestClientId
        }));

        Assert.Equal(HttpStatusCode.BadRequest, refreshResponse.StatusCode);
        string refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        Assert.Contains("invalid_grant", refreshBody);
    }

    [Fact]
    public async Task UnpairEndpoint_WithClientId_RevokesAllGrantsAndPairingSession()
    {
        var (accessToken, _) = await ObtainTokensAsync(AuthorizationScopes.WorkspaceRead);
        var client = CreateTestClient();

        // Verify MCP works
        var mcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":1}", Encoding.UTF8, "application/json")
        };
        mcpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var mcpRes = await client.SendAsync(mcpRequest);
        Assert.Equal(HttpStatusCode.OK, mcpRes.StatusCode);

        // Call /connect/unpair
        var unpairResponse = await client.PostAsync("/connect/unpair", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = TestClientId
        }));
        Assert.Equal(HttpStatusCode.OK, unpairResponse.StatusCode);
        string unpairBody = await unpairResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(unpairBody);
        Assert.True(doc.RootElement.GetProperty("grantsRevoked").GetInt32() >= 1);

        // MCP calls fail immediately
        var mcpAfterUnpair = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":2}", Encoding.UTF8, "application/json")
        };
        mcpAfterUnpair.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var mcpAfterRes = await client.SendAsync(mcpAfterUnpair);
        Assert.Equal(HttpStatusCode.Unauthorized, mcpAfterRes.StatusCode);
    }

    [Fact]
    public async Task UnpairEndpoint_WithoutClientId_WhenBearerAuthenticated_UnpairsCurrentClient()
    {
        var (accessToken, _) = await ObtainTokensAsync(AuthorizationScopes.WorkspaceRead);
        var client = CreateTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/unpair");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(TestClientId, doc.RootElement.GetProperty("clientId").GetString());
        Assert.True(doc.RootElement.GetProperty("grantsRevoked").GetInt32() >= 1);
    }
}
