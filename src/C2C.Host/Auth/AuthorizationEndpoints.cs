using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation;
using OpenIddict.Validation.AspNetCore;

using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Host.Auth;

/// <summary>
/// OAuth 2.1 authorization, token, and revocation endpoint handlers adhering to UC-AUTH-02, UC-AUTH-03,
/// UC-AUTH-04, UC-AUTH-05, BR-AUTH-001, BR-AUTH-002, BR-AUTH-005, BR-AUTH-006, and BR-SEC-006.
/// Validates PKCE, scopes, single-use codes, refresh token rotation with replay detection, and unpairing.
/// </summary>
public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapMethods("/connect/authorize", ["GET", "POST"], HandleAuthorizeAsync);
        endpoints.MapPost("/connect/token", HandleTokenAsync);
        endpoints.MapPost("/connect/revoke", HandleRevokeAsync);
        endpoints.MapPost("/connect/unpair", HandleUnpairAsync);
        endpoints.MapGet("/.well-known/oauth-protected-resource", HandleProtectedResourceMetadataAsync);

        return endpoints;
    }

    private static async Task HandleAuthorizeAsync(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest();
        if (request == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_request", "Missing or invalid OAuth authorization request.");
            return;
        }

        // 1. Enforce PKCE S256 (BR-AUTH-001)
        if (string.IsNullOrEmpty(request.CodeChallenge) ||
            !string.Equals(request.CodeChallengeMethod, OpenIddictConstants.CodeChallengeMethods.Sha256, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_request", "PKCE with code_challenge_method=S256 is required.");
            return;
        }

        // 2. Validate requested scopes against V1 allowlist (04-DETAILED-AUTH-DESIGN.md)
        var requestedScopes = request.GetScopes();
        if (!AuthorizationScopes.ValidateAll(requestedScopes, out string? invalidScope))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_scope", $"The requested scope '{invalidScope}' is unsupported.");
            return;
        }

        // 3. Extract pairing code
        string? pairingCode = context.Request.Query["pairing_code"].FirstOrDefault()
            ?? context.Request.Headers["X-Pairing-Code"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(pairingCode) && context.Request.HasFormContentType)
        {
            pairingCode = context.Request.Form["pairing_code"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_request", "A valid pairing_code is required for authorization approval.");
            return;
        }

        var workspaceContext = context.RequestServices.GetRequiredService<IWorkspaceContext>();
        var pairingService = context.RequestServices.GetRequiredService<IPairingService>();
        var grantStore = context.RequestServices.GetRequiredService<IAuthorizationStateStore>();
        var authOptions = context.RequestServices.GetRequiredService<AuthorizationServerOptions>();
        var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();

        // 4. Atomically validate and consume pairing session (BR-AUTH-007, BR-SEC-011)
        var validation = await pairingService.ValidateAndConsumeCodeAsync(
            workspaceContext.Id.Value,
            pairingCode,
            request.ClientId,
            context.RequestAborted);

        if (!validation.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            string errorType = validation.ErrorCode switch
            {
                CommonErrorCodes.AuthPairingExpired => "invalid_grant",
                CommonErrorCodes.AuthPairingLocked => "access_denied",
                _ => "access_denied"
            };

            await WriteJsonErrorAsync(context, errorType, validation.ErrorMessage ?? "Pairing validation failed.", validation.ErrorCode);
            return;
        }

        // 5. Create and persist authorization grant
        string grantId = Guid.NewGuid().ToString("N");
        var grant = new AuthorizationGrant
        {
            GrantId = grantId,
            WorkspaceId = workspaceContext.Id.Value,
            ClientId = request.ClientId!,
            Issuer = authOptions.Issuer,
            Resource = authOptions.Resource,
            Scopes = requestedScopes.ToArray(),
            Status = AuthorizationGrantStatus.Active,
            CreatedAt = timeProvider.GetUtcNow(),
            SchemaVersion = 1
        };

        await grantStore.SaveGrantAsync(workspaceContext.Id.Value, grant, context.RequestAborted);

        // 6. Build principal and issue authorization code via OpenIddict
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, workspaceContext.Id.Value).SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim("workspace_id", workspaceContext.Id.Value).SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim("grant_id", grantId).SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.ClientId, request.ClientId!).SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Issuer, authOptions.Issuer).SetDestinations(OpenIddictConstants.Destinations.AccessToken));
        identity.AddClaim(new Claim("auth_code_id", Guid.NewGuid().ToString("N")).SetDestinations(OpenIddictConstants.Destinations.AccessToken));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(requestedScopes);
        principal.SetResources(authOptions.Resource);

        await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    private static async Task HandleTokenAsync(HttpContext context)
    {
        var request = context.GetOpenIddictServerRequest();
        if (request == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_request", "Missing or invalid OAuth token request.");
            return;
        }

        if (request.IsAuthorizationCodeGrantType())
        {
            var authenticateResult = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Invalid or expired authorization code.");
                return;
            }

            var principal = authenticateResult.Principal;

            // 1. Enforce single-use authorization code (BR-AUTH-005, BR-CON-005)
            string? codeId = principal.FindFirst("auth_code_id")?.Value;
            if (!string.IsNullOrEmpty(codeId))
            {
                var tracker = context.RequestServices.GetRequiredService<ICodeRedemptionTracker>();
                if (!tracker.TryRedeemCode(codeId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await WriteJsonErrorAsync(context, "invalid_grant", "Authorization code has already been redeemed.");
                    return;
                }
            }

            // 2. Validate active grant
            var grantStore = context.RequestServices.GetRequiredService<IAuthorizationStateStore>();
            string? workspaceId = principal.FindFirst("workspace_id")?.Value;
            string? grantId = principal.FindFirst("grant_id")?.Value;

            if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(grantId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Missing grant or workspace binding.");
                return;
            }

            var grant = await grantStore.GetGrantAsync(workspaceId, grantId, context.RequestAborted);
            if (grant == null || grant.Status != AuthorizationGrantStatus.Active)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Authorization grant is revoked or does not exist.");
                return;
            }

            var authOptions = context.RequestServices.GetRequiredService<AuthorizationServerOptions>();

            // 3. Issue access token (and optional refresh token family)
            var identity = new ClaimsIdentity(
                claims: principal.Claims,
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            if (principal.HasScope(AuthorizationScopes.OfflineAccess))
            {
                var refreshStore = context.RequestServices.GetRequiredService<IRefreshFamilyStore>();
                var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();

                string familyId = Guid.NewGuid().ToString("N");
                string tokenId = Guid.NewGuid().ToString("N");
                string tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenId)));

                var family = new RefreshFamily
                {
                    FamilyId = familyId,
                    GrantId = grantId,
                    WorkspaceId = workspaceId,
                    ClientId = principal.FindFirst(OpenIddictConstants.Claims.ClientId)?.Value ?? request.ClientId!,
                    Issuer = authOptions.Issuer,
                    Resource = authOptions.Resource,
                    CurrentGeneration = 1,
                    CurrentTokenHash = tokenHash,
                    ExpiresAt = timeProvider.GetUtcNow().AddDays(14),
                    Status = RefreshFamilyStatus.Active,
                    UpdatedAt = timeProvider.GetUtcNow(),
                    SchemaVersion = 1
                };

                await refreshStore.SaveFamilyAsync(workspaceId, family, context.RequestAborted);

                identity.AddClaim(new Claim("refresh_family_id", familyId));
                identity.AddClaim(new Claim("refresh_token_id", tokenId));
                identity.AddClaim(new Claim("refresh_token_gen", "1"));
            }

            foreach (var claim in identity.Claims)
            {
                if (claim.Type is "refresh_family_id" or "refresh_token_id" or "refresh_token_gen")
                {
                    claim.SetDestinations(Array.Empty<string>());
                }
                else
                {
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                }
            }

            var tokenPrincipal = new ClaimsPrincipal(identity);
            tokenPrincipal.SetScopes(principal.GetScopes());
            tokenPrincipal.SetResources(authOptions.Resource);
            tokenPrincipal.SetAccessTokenLifetime(TimeSpan.FromHours(1));

            if (principal.HasScope(AuthorizationScopes.OfflineAccess))
            {
                tokenPrincipal.SetRefreshTokenLifetime(TimeSpan.FromDays(14));
            }

            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, tokenPrincipal);
            return;
        }

        if (request.IsRefreshTokenGrantType())
        {
            var authenticateResult = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Invalid or expired refresh token.");
                return;
            }

            var principal = authenticateResult.Principal;
            var grantStore = context.RequestServices.GetRequiredService<IAuthorizationStateStore>();
            var refreshStore = context.RequestServices.GetRequiredService<IRefreshFamilyStore>();
            var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();
            var authOptions = context.RequestServices.GetRequiredService<AuthorizationServerOptions>();

            string? workspaceId = principal.FindFirst("workspace_id")?.Value;
            string? grantId = principal.FindFirst("grant_id")?.Value;

            if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(grantId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Missing grant or workspace binding.");
                return;
            }

            var grant = await grantStore.GetGrantAsync(workspaceId, grantId, context.RequestAborted);
            if (grant == null || grant.Status != AuthorizationGrantStatus.Active)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Authorization grant is revoked or does not exist.");
                return;
            }

            string? familyId = principal.FindFirst("refresh_family_id")?.Value;
            string? tokenId = principal.FindFirst("refresh_token_id")?.Value;
            string? genStr = principal.FindFirst("refresh_token_gen")?.Value;

            if (string.IsNullOrEmpty(familyId) || string.IsNullOrEmpty(tokenId) || string.IsNullOrEmpty(genStr) || !int.TryParse(genStr, out int presentedGen))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Missing refresh token binding metadata.");
                return;
            }

            var family = await refreshStore.GetFamilyAsync(workspaceId, familyId, context.RequestAborted);
            if (family == null || family.Status == RefreshFamilyStatus.Revoked)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Refresh family is revoked or does not exist.");
                return;
            }

            if (family.Status == RefreshFamilyStatus.ReplayDetected)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Refresh token replay detected. Family revoked.");
                return;
            }

            string tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenId)));
            bool isMatch = family.CurrentGeneration == presentedGen &&
                           CryptographicOperations.FixedTimeEquals(
                               Encoding.UTF8.GetBytes(family.CurrentTokenHash),
                               Encoding.UTF8.GetBytes(tokenHash));

            if (!isMatch)
            {
                // Replay detected per BR-CON-005: revoke family immediately
                family.Status = RefreshFamilyStatus.ReplayDetected;
                family.UpdatedAt = timeProvider.GetUtcNow();
                await refreshStore.SaveFamilyAsync(workspaceId, family, context.RequestAborted);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Refresh token replay detected. Family revoked.");
                return;
            }

            if (family.ExpiresAt <= timeProvider.GetUtcNow())
            {
                family.Status = RefreshFamilyStatus.Revoked;
                family.UpdatedAt = timeProvider.GetUtcNow();
                await refreshStore.SaveFamilyAsync(workspaceId, family, context.RequestAborted);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "invalid_grant", "Refresh token is expired.");
                return;
            }

            // Rotate single-use refresh token
            int nextGen = family.CurrentGeneration + 1;
            string nextTokenId = Guid.NewGuid().ToString("N");
            string nextTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nextTokenId)));

            family.CurrentGeneration = nextGen;
            family.CurrentTokenHash = nextTokenHash;
            family.UpdatedAt = timeProvider.GetUtcNow();
            await refreshStore.SaveFamilyAsync(workspaceId, family, context.RequestAborted);

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: OpenIddictConstants.Claims.Name,
                roleType: OpenIddictConstants.Claims.Role);

            foreach (var claim in principal.Claims)
            {
                if (claim.Type is OpenIddictConstants.Claims.Subject or "workspace_id" or "grant_id" or OpenIddictConstants.Claims.ClientId or OpenIddictConstants.Claims.Issuer)
                {
                    identity.AddClaim(new Claim(claim.Type, claim.Value));
                }
            }

            identity.AddClaim(new Claim("refresh_family_id", family.FamilyId));
            identity.AddClaim(new Claim("refresh_token_id", nextTokenId));
            identity.AddClaim(new Claim("refresh_token_gen", nextGen.ToString()));

            foreach (var claim in identity.Claims)
            {
                if (claim.Type is "refresh_family_id" or "refresh_token_id" or "refresh_token_gen")
                {
                    claim.SetDestinations(Array.Empty<string>());
                }
                else
                {
                    claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                }
            }

            var tokenPrincipal = new ClaimsPrincipal(identity);
            tokenPrincipal.SetScopes(principal.GetScopes());
            tokenPrincipal.SetResources(authOptions.Resource);
            tokenPrincipal.SetAccessTokenLifetime(TimeSpan.FromHours(1));
            tokenPrincipal.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, tokenPrincipal);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await WriteJsonErrorAsync(context, "unsupported_grant_type", "The requested grant_type is not supported.");
    }

    private static async Task HandleProtectedResourceMetadataAsync(HttpContext context)
    {
        var authOptions = context.RequestServices.GetRequiredService<AuthorizationServerOptions>();

        var metadata = new
        {
            resource = authOptions.Resource,
            authorization_servers = new[] { authOptions.Issuer.TrimEnd('/') }
        };

        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, metadata);
    }

    private static async Task HandleRevokeAsync(HttpContext context)
    {
        var workspaceContext = context.RequestServices.GetRequiredService<IWorkspaceContext>();
        var revoker = context.RequestServices.GetRequiredService<IAuthorizationRevoker>();
        var validationService = context.RequestServices.GetService<OpenIddictValidationService>();

        string? token = null;
        string? clientId = null;
        string? grantId = null;

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            token = form["token"].FirstOrDefault();
            clientId = form["client_id"].FirstOrDefault();
            grantId = form["grant_id"].FirstOrDefault();
        }
        else if (context.Request.HasJsonContentType())
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
            {
                token = tokenProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("client_id", out var clientProp))
            {
                clientId = clientProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("grant_id", out var grantProp))
            {
                grantId = grantProp.GetString();
            }
        }

        if (!string.IsNullOrWhiteSpace(grantId))
        {
            await revoker.RevokeGrantAsync(workspaceContext.Id.Value, grantId, context.RequestAborted);
        }
        else if (!string.IsNullOrWhiteSpace(clientId))
        {
            await revoker.RevokeClientAsync(workspaceContext.Id.Value, clientId, context.RequestAborted);
        }
        else if (!string.IsNullOrWhiteSpace(token) && validationService != null)
        {
            try
            {
                var principal = await validationService.ValidateAccessTokenAsync(token, context.RequestAborted);
                string? tokenGrantId = principal?.FindFirst("grant_id")?.Value;
                string? tokenWorkspaceId = principal?.FindFirst("workspace_id")?.Value;

                if (!string.IsNullOrEmpty(tokenGrantId) &&
                    string.Equals(tokenWorkspaceId, workspaceContext.Id.Value, StringComparison.Ordinal))
                {
                    await revoker.RevokeGrantAsync(workspaceContext.Id.Value, tokenGrantId, context.RequestAborted);
                }
            }
            catch
            {
                // RFC 7009: Invalidation of unknown or expired tokens must succeed silently
            }
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    private static async Task HandleUnpairAsync(HttpContext context)
    {
        var workspaceContext = context.RequestServices.GetRequiredService<IWorkspaceContext>();
        var revoker = context.RequestServices.GetRequiredService<IAuthorizationRevoker>();

        string? clientId = null;

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(context.RequestAborted);
            clientId = form["client_id"].FirstOrDefault();
        }
        else if (context.Request.HasJsonContentType())
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            if (doc.RootElement.TryGetProperty("client_id", out var clientProp))
            {
                clientId = clientProp.GetString();
            }
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            var authResult = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            if (authResult.Succeeded && authResult.Principal != null)
            {
                clientId = authResult.Principal.FindFirst(OpenIddictConstants.Claims.ClientId)?.Value;
            }
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "invalid_request", "client_id is required for unpair.", CommonErrorCodes.InvalidArgument);
            return;
        }

        var result = await revoker.RevokeClientAsync(workspaceContext.Id.Value, clientId, context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static async Task WriteJsonErrorAsync(
        HttpContext context,
        string error,
        string description,
        string? errorCode = null)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            error,
            error_description = description,
            error_code = errorCode
        };
        await JsonSerializer.SerializeAsync(context.Response.Body, payload);
    }
}
