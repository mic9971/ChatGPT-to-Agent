using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace C2C.IntegrationTests.Authorization;

/// <summary>
/// WebApplicationFactory fixture configuring OpenIddict Server + Validation in degraded/stateless mode
/// proving database-free OAuth 2.1 authorization server operations adhering to 03-AUTH-SUBSTRATE-SPIKE.md.
/// </summary>
public sealed class OpenIddictSpikeFixture : WebApplicationFactory<C2C.Host.Program>
{
    public const string ExpectedResource = "http://127.0.0.1:5000/mcp";
    public const string ExpectedIssuer = "https://test.c2c.local/";
    public const string TestClientId = "c2c-test-client";
    public const string TestRedirectUri = "https://client.test.local/callback";

    // Track redeemed authorization code IDs to test single-use enforcement in degraded mode
    private readonly HashSet<string> _redeemedCodeIds = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    protected override IHostBuilder CreateHostBuilder()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webHostBuilder =>
            {
                webHostBuilder.ConfigureServices(services =>
                {
                services.AddRouting();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                });
                services.AddAuthorization();

                services.AddOpenIddict()
                    .AddServer(options =>
                    {
                        options.SetAuthorizationEndpointUris("/connect/authorize")
                               .SetTokenEndpointUris("/connect/token");

                        options.AllowAuthorizationCodeFlow()
                               .RequireProofKeyForCodeExchange();

                        options.AllowRefreshTokenFlow();

                        options.RegisterScopes("mcp:read", "offline_access");

                        // Use ephemeral keys for the spike test environment
                        options.AddEphemeralEncryptionKey()
                               .AddEphemeralSigningKey();

                        options.SetIssuer(new Uri(ExpectedIssuer));

                        options.UseAspNetCore()
                               .DisableTransportSecurityRequirement()
                               .EnableAuthorizationEndpointPassthrough()
                               .EnableTokenEndpointPassthrough();

                        // Database-free degraded mode preserving BR-COM-012 (no EF Core / SQLite)
                        options.EnableDegradedMode();

                        // In degraded mode, OpenIddict requires explicit handlers for request validation:
                        options.AddEventHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>(builder =>
                            builder.UseInlineHandler(context =>
                            {
                                if (!string.Equals(context.ClientId, TestClientId, StringComparison.Ordinal))
                                {
                                    context.Reject(
                                        error: OpenIddictConstants.Errors.InvalidClient,
                                        description: "The client identifier is invalid.");
                                    return default;
                                }

                                if (!string.Equals(context.RedirectUri, TestRedirectUri, StringComparison.Ordinal))
                                {
                                    context.Reject(
                                        error: OpenIddictConstants.Errors.InvalidRequest,
                                        description: "The redirect URI is invalid.");
                                    return default;
                                }

                                return default;
                            }));

                        options.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
                            builder.UseInlineHandler(context =>
                            {
                                if (context.Request.IsAuthorizationCodeGrantType())
                                {
                                    if (!string.Equals(context.ClientId, TestClientId, StringComparison.Ordinal))
                                    {
                                        context.Reject(
                                            error: OpenIddictConstants.Errors.InvalidClient,
                                            description: "The client identifier is invalid.");
                                        return default;
                                    }
                                }

                                return default;
                            }));
                    })
                    .AddValidation(options =>
                    {
                        options.UseLocalServer();
                        options.UseAspNetCore();
                    });
            });

            webHostBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    // 1. Authorize endpoint
                    endpoints.MapGet("/connect/authorize", async context =>
                    {
                        var request = context.GetOpenIddictServerRequest() ??
                            throw new InvalidOperationException("Missing OpenIddict request.");

                        // In degraded mode: validate client_id and redirect_uri in custom handler
                        if (!string.Equals(request.ClientId, TestClientId, StringComparison.Ordinal))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid client_id");
                            return;
                        }

                        if (!string.Equals(request.RedirectUri, TestRedirectUri, StringComparison.Ordinal))
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Invalid redirect_uri");
                            return;
                        }

                        // Build principal representing approved user / workspace session
                        var identity = new ClaimsIdentity(
                            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                            nameType: OpenIddictConstants.Claims.Name,
                            roleType: OpenIddictConstants.Claims.Role);

                        identity.AddClaim(OpenIddictConstants.Claims.Subject, "test-user-ws1");
                        identity.AddClaim("workspace_id", "ws_test_123");

                        // Add unique code ID claim to allow tracking single-use redemption
                        string codeId = Guid.NewGuid().ToString("N");
                        identity.AddClaim("auth_code_id", codeId);

                        var principal = new ClaimsPrincipal(identity);
                        principal.SetScopes(request.GetScopes());

                        // Canonical MCP resource binding
                        principal.SetResources(ExpectedResource);

                        // Include RFC 9207 iss claim
                        identity.AddClaim(OpenIddictConstants.Claims.Issuer, ExpectedIssuer);

                        // Sign in via OpenIddict to issue the authorization code
                        await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
                    });

                    // 2. Token endpoint
                    endpoints.MapPost("/connect/token", async context =>
                    {
                        var request = context.GetOpenIddictServerRequest() ??
                            throw new InvalidOperationException("Missing OpenIddict request.");

                        if (request.IsAuthorizationCodeGrantType())
                        {
                            // Retrieve principal extracted from decrypted authorization code
                            var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                            if (!result.Succeeded || result.Principal == null)
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync("Invalid authorization code");
                                return;
                            }

                            // Verify and enforce single-use authorization code
                            string? codeId = result.Principal.FindFirst("auth_code_id")?.Value;
                            if (!string.IsNullOrEmpty(codeId))
                            {
                                lock (_lock)
                                {
                                    if (!_redeemedCodeIds.Add(codeId))
                                    {
                                        // Code was already redeemed! Replay attack detected
                                        context.Response.StatusCode = 400;
                                        context.Response.ContentType = "application/json";
                                        context.Response.WriteAsync("{\"error\":\"invalid_grant\",\"error_description\":\"Authorization code already redeemed.\"}").Wait();
                                        return;
                                    }
                                }
                            }

                            // Issue access token (and refresh token if offline_access scope was requested)
                            var identity = new ClaimsIdentity(result.Principal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                            var principal = new ClaimsPrincipal(identity);
                            principal.SetScopes(result.Principal.GetScopes());
                            principal.SetResources(ExpectedResource);

                            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
                            return;
                        }

                        if (request.IsRefreshTokenGrantType())
                        {
                            var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                            if (!result.Succeeded || result.Principal == null)
                            {
                                context.Response.StatusCode = 400;
                                await context.Response.WriteAsync("Invalid refresh token");
                                return;
                            }

                            var identity = new ClaimsIdentity(result.Principal.Claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                            var principal = new ClaimsPrincipal(identity);
                            principal.SetScopes(result.Principal.GetScopes());
                            principal.SetResources(ExpectedResource);

                            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
                            return;
                        }

                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Unsupported grant_type");
                    });

                    // 3. Protected MCP endpoint validating the access token
                    endpoints.MapGet("/mcp-protected", async context =>
                    {
                        var user = context.User;
                        if (user.Identity?.IsAuthenticated != true)
                        {
                            context.Response.StatusCode = 401;
                            return;
                        }

                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"status\":\"authorized\",\"subject\":\"" + user.FindFirst(OpenIddictConstants.Claims.Subject)?.Value + "\"}");
                    }).RequireAuthorization();
                });
            });
        });
    }
}
