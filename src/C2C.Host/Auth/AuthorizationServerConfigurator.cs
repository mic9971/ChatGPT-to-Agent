using System;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

using C2C.Core.Authorization;

namespace C2C.Host.Auth;

/// <summary>
/// Configures OpenIddict OAuth 2.1 server and token validation in database-free degraded mode
/// adhering to UC-AUTH-02, BR-AUTH-001, BR-AUTH-002, BR-AUTH-003, and BR-COM-012.
/// </summary>
public static class AuthorizationServerConfigurator
{
    public static IServiceCollection AddC2CAuthorizationServer(
        this IServiceCollection services,
        Action<AuthorizationServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        AuthorizationServerOptions options = new();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddAuthentication(auth =>
        {
            auth.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        services.AddOpenIddict()
            .AddServer(server =>
            {
                server.SetAuthorizationEndpointUris("/connect/authorize")
                      .SetTokenEndpointUris("/connect/token");

                server.AllowAuthorizationCodeFlow()
                      .RequireProofKeyForCodeExchange();

                server.AllowRefreshTokenFlow();

                server.RegisterScopes(
                    AuthorizationScopes.WorkspaceRead,
                    AuthorizationScopes.WorkspaceSearch,
                    AuthorizationScopes.GitRead,
                    AuthorizationScopes.ExecutionRead,
                    AuthorizationScopes.OfflineAccess);

                server.EnableDegradedMode();
                server.UseDataProtection();

                // Ephemeral keys guarantee in-memory signing and encryption fallback
                server.AddEphemeralEncryptionKey()
                      .AddEphemeralSigningKey();

                server.SetIssuer(new Uri(options.Issuer));

                var aspNetCore = server.UseAspNetCore()
                                       .EnableAuthorizationEndpointPassthrough()
                                       .EnableTokenEndpointPassthrough();

                if (options.DisableTransportSecurityRequirement)
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }

                // Degraded mode client & redirect validation event handlers
                server.AddEventHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>(builder =>
                    builder.UseInlineHandler(async context =>
                    {
                        var httpContext = OpenIddictServerAspNetCoreHelpers.GetHttpRequest(context.Transaction)?.HttpContext;
                        if (httpContext == null) return;

                        if (string.IsNullOrEmpty(context.ClientId))
                        {
                            context.Reject(
                                error: OpenIddictConstants.Errors.InvalidClient,
                                description: "The client identifier is missing.");
                            return;
                        }

                        var resolver = httpContext.RequestServices.GetRequiredService<IClientMetadataResolver>();
                        var result = await resolver.ValidateClientAsync(context.ClientId, context.RedirectUri);

                        if (!result.IsValid)
                        {
                            context.Reject(
                                error: OpenIddictConstants.Errors.InvalidClient,
                                description: result.ErrorMessage ?? "The client identifier or redirect URI is invalid.");
                        }
                    }));

                server.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
                    builder.UseInlineHandler(async context =>
                    {
                        if (context.Request.IsAuthorizationCodeGrantType())
                        {
                            var httpContext = OpenIddictServerAspNetCoreHelpers.GetHttpRequest(context.Transaction)?.HttpContext;
                            if (httpContext == null) return;

                            if (string.IsNullOrEmpty(context.ClientId))
                            {
                                context.Reject(
                                    error: OpenIddictConstants.Errors.InvalidClient,
                                    description: "The client identifier is missing.");
                                return;
                            }

                            var resolver = httpContext.RequestServices.GetRequiredService<IClientMetadataResolver>();
                            var result = await resolver.ValidateClientAsync(context.ClientId, context.Request.RedirectUri);

                            if (!result.IsValid)
                            {
                                context.Reject(
                                    error: OpenIddictConstants.Errors.InvalidClient,
                                    description: result.ErrorMessage ?? "The client identifier is invalid.");
                            }
                        }
                    }));

                server.AddEventHandler<OpenIddictServerEvents.ValidateRevocationRequestContext>(builder =>
                    builder.UseInlineHandler(context => default));
            })
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.UseAspNetCore();
                validation.UseDataProtection();
            });

        return services;
    }
}
