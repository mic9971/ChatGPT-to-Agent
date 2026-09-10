using System;
using System.Collections.Generic;
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

using System.IO;
using C2C.Core.Authorization;
using C2C.Core.Workspace;
using C2C.Host;
using C2C.Host.Auth;
using C2C.Infrastructure.Authorization;

namespace C2C.IntegrationTests.Authorization;

[Collection("AuthorizationIntegration")]
public sealed class TokenEndpointAndProtectedMcpTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestClientId = "c2c-token-client";
    private const string TestRedirectUri = "https://app.example.com/callback";

    private readonly string _storageDir;
    private readonly WebApplicationFactory<Program> _factory;

    public TokenEndpointAndProtectedMcpTests(WebApplicationFactory<Program> factory)
    {
        _storageDir = Path.Combine(Path.GetTempPath(), "c2c-test-token-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public async Task TokenEndpoint_WithValidAuthorizationCode_IssuesAccessToken()
    {
        var (code, verifier) = await AuthorizeAndGetCodeAsync(AuthorizationScopes.WorkspaceRead);

        var client = CreateTestClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        });

        HttpResponseMessage response = await client.PostAsync("/connect/token", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK but got {response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        string? accessToken = doc.RootElement.GetProperty("access_token").GetString();
        string? tokenType = doc.RootElement.GetProperty("token_type").GetString();

        Assert.False(string.IsNullOrEmpty(accessToken));
        Assert.Equal("Bearer", tokenType);
    }

    [Fact]
    public async Task TokenEndpoint_ReplayingConsumedAuthorizationCode_IsRejected()
    {
        var (code, verifier) = await AuthorizeAndGetCodeAsync(AuthorizationScopes.WorkspaceRead);

        var client = CreateTestClient();
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        };

        // First redemption -> success
        var r1 = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);

        // Replay attempt -> 400 Bad Request
        var r2 = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        string body = await r2.Content.ReadAsStringAsync();
        Assert.Contains("invalid_grant", body);
    }

    [Fact]
    public async Task TokenEndpoint_WithOfflineAccessScope_IssuesRefreshToken()
    {
        string scopes = $"{AuthorizationScopes.WorkspaceRead} {AuthorizationScopes.OfflineAccess}";
        var (code, verifier) = await AuthorizeAndGetCodeAsync(scopes);

        var client = CreateTestClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        });

        HttpResponseMessage response = await client.PostAsync("/connect/token", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("refresh_token", out var refreshTokenProp));
        Assert.False(string.IsNullOrEmpty(refreshTokenProp.GetString()));
    }

    [Fact]
    public async Task McpEndpoint_WithoutToken_Returns401WithResourceMetadataChallenge()
    {
        var client = CreateTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":1}", Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var wwwAuth = response.Headers.WwwAuthenticate.ToString();
        Assert.Contains("Bearer", wwwAuth);
        Assert.Contains("invalid_token", wwwAuth);
        Assert.Contains("resource_metadata", wwwAuth);
    }

    [Fact]
    public async Task McpEndpoint_WithRevokedGrant_Returns401()
    {
        var (code, verifier) = await AuthorizeAndGetCodeAsync(AuthorizationScopes.WorkspaceRead);

        var client = CreateTestClient();
        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        }));

        string body = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        string accessToken = doc.RootElement.GetProperty("access_token").GetString()!;

        // Revoke all grants for the workspace
        using (var scope = _factory.Services.CreateScope())
        {
            var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
            var grantStore = scope.ServiceProvider.GetRequiredService<IAuthorizationStateStore>();
            var grants = await grantStore.GetActiveGrantsAsync(workspaceContext.Id.Value);
            foreach (var g in grants)
            {
                await grantStore.RevokeGrantAsync(workspaceContext.Id.Value, g.GrantId);
            }
        }

        // Call /mcp with revoked token
        var mcpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"tools/list\",\"id\":1}", Encoding.UTF8, "application/json")
        };
        mcpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage mcpResponse = await client.SendAsync(mcpRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, mcpResponse.StatusCode);
    }

    [Fact]
    public async Task McpTool_WithInsufficientScope_Returns403BeforeHandlerExecution()
    {
        // Issue token with ONLY workspace.read scope
        var (code, verifier) = await AuthorizeAndGetCodeAsync(AuthorizationScopes.WorkspaceRead);

        var client = CreateTestClient();
        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        }));

        string tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokenBody);
        string accessToken = doc.RootElement.GetProperty("access_token").GetString()!;

        // 1. workspace_info requires workspace.read -> ALLOW
        var wsInfoRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"workspace_info\",\"arguments\":{}},\"id\":1}",
                Encoding.UTF8, "application/json")
        };
        wsInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage wsInfoResponse = await client.SendAsync(wsInfoRequest);
        Assert.Equal(HttpStatusCode.OK, wsInfoResponse.StatusCode);
        string wsInfoBody = await wsInfoResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("INSUFFICIENT_SCOPE", wsInfoBody);

        // 2. search_workspace requires workspace.search -> DENY (403)
        var searchRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                "{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{\"name\":\"search_workspace\",\"arguments\":{\"query\":\"test\"}},\"id\":2}",
                Encoding.UTF8, "application/json")
        };
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage searchResponse = await client.SendAsync(searchRequest);
        Assert.Equal(HttpStatusCode.Forbidden, searchResponse.StatusCode);
        var wwwAuth = searchResponse.Headers.WwwAuthenticate.ToString();
        Assert.Contains("insufficient_scope", wwwAuth);
        Assert.Contains("workspace.search", wwwAuth);

        string searchBody = await searchResponse.Content.ReadAsStringAsync();
        Assert.Contains("INSUFFICIENT_SCOPE", searchBody);
    }

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
    public async Task AllNineTools_WhenTokenLacksScope_Returns403WithExactScopeRequirement(string toolName, string requiredScope)
    {
        // Grant a different scope than requiredScope
        string grantedScope = requiredScope == AuthorizationScopes.WorkspaceRead
            ? AuthorizationScopes.GitRead
            : AuthorizationScopes.WorkspaceRead;

        var (code, verifier) = await AuthorizeAndGetCodeAsync(grantedScope);

        var client = CreateTestClient();
        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = TestRedirectUri,
            ["client_id"] = TestClientId,
            ["code_verifier"] = verifier
        }));

        string tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokenBody);
        string accessToken = doc.RootElement.GetProperty("access_token").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                $"{{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\",\"params\":{{\"name\":\"{toolName}\",\"arguments\":{{}}}},\"id\":99}}",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var wwwAuth = response.Headers.WwwAuthenticate.ToString();
        Assert.Contains("insufficient_scope", wwwAuth);
        Assert.Contains(requiredScope, wwwAuth);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("INSUFFICIENT_SCOPE", body);
    }
}

