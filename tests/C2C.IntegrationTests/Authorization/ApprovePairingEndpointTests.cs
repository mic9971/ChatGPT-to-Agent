using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
public sealed class ApprovePairingEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string TestClientId = "c2c-integration-client";
    private const string TestRedirectUri = "https://app.example.com/callback";

    private readonly string _storageDir;
    private readonly WebApplicationFactory<Program> _factory;

    public ApprovePairingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _storageDir = Path.Combine(Path.GetTempPath(), "c2c-test-approve-" + Guid.NewGuid().ToString("N"));
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:DisableTransportSecurityRequirement", "true");
            builder.UseSetting("Auth:StorageDirectory", _storageDir);
            builder.ConfigureServices(services =>
            {
                // Pre-register test client
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

    private HttpClient CreateTestClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://127.0.0.1:5000")
        });

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

    [Fact]
    public async Task Authorize_WithValidPairingCodeAndPkce_IssuesAuthorizationCode()
    {
        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var pairingService = scope.ServiceProvider.GetRequiredService<IPairingService>();
        var grantStore = scope.ServiceProvider.GetRequiredService<IAuthorizationStateStore>();

        var pairing = await pairingService.CreateSessionAsync(workspaceContext.Id.Value);

        var (_, challenge) = GeneratePkce();
        string state = Guid.NewGuid().ToString("N");

        string query = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"state={state}&" +
            $"code_challenge={challenge}&" +
            $"code_challenge_method=S256&" +
            $"pairing_code={pairing.DisplayCode}";

        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync(query);

        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Expected redirect but got {response.StatusCode}: {body}");
        Uri? location = response.Headers.Location;
        Assert.NotNull(location);

        var parsedQuery = HttpUtility.ParseQueryString(location.Query);
        string? code = parsedQuery["code"];
        string? returnedState = parsedQuery["state"];
        string? returnedIss = parsedQuery["iss"];

        Assert.False(string.IsNullOrEmpty(code), "Authorization code should be present in redirect URI.");
        Assert.Equal(state, returnedState);
        Assert.Equal("https://127.0.0.1:5000/", returnedIss);

        // Verify grant is stored
        var activeGrants = await grantStore.GetActiveGrantsAsync(workspaceContext.Id.Value);
        Assert.Contains(activeGrants, g => g.ClientId == TestClientId);

        // Verify pairing session is consumed
        var currentSession = await pairingService.GetCurrentSessionAsync(workspaceContext.Id.Value);
        Assert.NotNull(currentSession);
        Assert.Equal(PairingSessionStatus.Consumed, currentSession.Status);
    }

    [Fact]
    public async Task Authorize_WhenPairingCodeMissing_ReturnsBadRequest()
    {
        var (_, challenge) = GeneratePkce();
        string query = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"code_challenge={challenge}&" +
            $"code_challenge_method=S256";

        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_request", body);
    }

    [Fact]
    public async Task Authorize_WhenPairingCodeInvalid_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var pairingService = scope.ServiceProvider.GetRequiredService<IPairingService>();
        await pairingService.CreateSessionAsync(workspaceContext.Id.Value);

        var (_, challenge) = GeneratePkce();
        string query = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"code_challenge={challenge}&" +
            $"code_challenge_method=S256&" +
            $"pairing_code=WRONGCOD";

        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("access_denied", body);
        Assert.Contains("AUTH_PAIRING_INVALID", body);
    }

    [Fact]
    public async Task Authorize_WhenScopeUnsupported_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var pairingService = scope.ServiceProvider.GetRequiredService<IPairingService>();
        var pairing = await pairingService.CreateSessionAsync(workspaceContext.Id.Value);

        var (_, challenge) = GeneratePkce();
        string query = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope=workspace.write&" +
            $"code_challenge={challenge}&" +
            $"code_challenge_method=S256&" +
            $"pairing_code={pairing.DisplayCode}";

        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_scope", body);
    }

    [Fact]
    public async Task Authorize_WhenPkceMissingOrPlain_ReturnsBadRequest()
    {
        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var pairingService = scope.ServiceProvider.GetRequiredService<IPairingService>();
        var pairing = await pairingService.CreateSessionAsync(workspaceContext.Id.Value);

        // PKCE with plain method
        string query = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"code_challenge=testplain&" +
            $"code_challenge_method=plain&" +
            $"pairing_code={pairing.DisplayCode}";

        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync(query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_request", body);
    }

    [Fact]
    public async Task Authorize_ConcurrentRequestsWithSamePairing_OnlyOneSucceeds()
    {
        using var scope = _factory.Services.CreateScope();
        var workspaceContext = scope.ServiceProvider.GetRequiredService<IWorkspaceContext>();
        var pairingService = scope.ServiceProvider.GetRequiredService<IPairingService>();
        var pairing = await pairingService.CreateSessionAsync(workspaceContext.Id.Value);

        var (verifier1, challenge1) = GeneratePkce();
        var (verifier2, challenge2) = GeneratePkce();

        string query1 = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"state=req1&" +
            $"code_challenge={challenge1}&" +
            $"code_challenge_method=S256&" +
            $"pairing_code={pairing.DisplayCode}";

        string query2 = $"/connect/authorize?" +
            $"response_type=code&" +
            $"client_id={TestClientId}&" +
            $"redirect_uri={Uri.EscapeDataString(TestRedirectUri)}&" +
            $"scope={Uri.EscapeDataString(AuthorizationScopes.WorkspaceRead)}&" +
            $"state=req2&" +
            $"code_challenge={challenge2}&" +
            $"code_challenge_method=S256&" +
            $"pairing_code={pairing.DisplayCode}";

        var client1 = CreateTestClient();
        var client2 = CreateTestClient();

        var task1 = client1.GetAsync(query1);
        var task2 = client2.GetAsync(query2);

        var responses = await Task.WhenAll(task1, task2);

        int redirects = responses.Count(r => r.StatusCode == HttpStatusCode.Redirect);
        int badRequests = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        Assert.Equal(1, redirects);
        Assert.Equal(1, badRequests);
    }

    [Fact]
    public async Task ProtectedResourceMetadata_ReturnsCanonicalResourceAndIssuer()
    {
        var client = CreateTestClient();
        HttpResponseMessage response = await client.GetAsync("/.well-known/oauth-protected-resource");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal("http://127.0.0.1:5000/mcp", doc.RootElement.GetProperty("resource").GetString());
        var authServers = doc.RootElement.GetProperty("authorization_servers");
        Assert.Equal("https://127.0.0.1:5000", authServers[0].GetString());
    }
}
