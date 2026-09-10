using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

using C2C.Core.Authorization;
using C2C.Core.Workspace;

namespace C2C.Host.Auth;

/// <summary>
/// HTTP middleware protecting the MCP endpoint adhering to UC-AUTH-03, UC-MCP-02, and 10-MCP-AUTHORIZATION-INTEGRATION.md.
/// Enforces Bearer token authentication, workspace binding, active grant verification, and tool-scope authorization.
/// </summary>
public sealed class McpAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public McpAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IWorkspaceContext workspaceContext,
        IAuthorizationStateStore grantStore,
        AuthorizationServerOptions authOptions)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(workspaceContext);
        ArgumentNullException.ThrowIfNull(grantStore);
        ArgumentNullException.ThrowIfNull(authOptions);

        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await _next(context);
            return;
        }

        if (!authOptions.EnableRemoteAuthorization)
        {
            // Explicit local-only development mode (BR-SEC-005, 10-MCP-AUTHORIZATION-INTEGRATION.md)
            await _next(context);
            return;
        }

        var authResult = await context.AuthenticateAsync(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        string metadataUrl = $"{authOptions.Issuer.TrimEnd('/')}/.well-known/oauth-protected-resource";

        if (!authResult.Succeeded || authResult.Principal == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"invalid_token\", resource_metadata=\"{metadataUrl}\"";
            return;
        }

        var principal = authResult.Principal;

        // 1. Verify workspace binding (BR-SEC-006)
        string? tokenWorkspaceId = principal.FindFirst("workspace_id")?.Value;
        if (!string.Equals(tokenWorkspaceId, workspaceContext.Id.Value, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"invalid_token\", error_description=\"Token workspace mismatch\", resource_metadata=\"{metadataUrl}\"";
            return;
        }

        // 2. Verify active grant in storage
        string? grantId = principal.FindFirst("grant_id")?.Value;
        if (!string.IsNullOrEmpty(grantId))
        {
            var grant = await grantStore.GetGrantAsync(workspaceContext.Id.Value, grantId, context.RequestAborted);
            if (grant == null || grant.Status != AuthorizationGrantStatus.Active)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers["WWW-Authenticate"] = $"Bearer error=\"invalid_token\", error_description=\"Authorization grant revoked or expired\", resource_metadata=\"{metadataUrl}\"";
                return;
            }
        }

        // 3. Tool-scope requirement resolver before invoking MCP handler (10-MCP-AUTHORIZATION-INTEGRATION.md)
        if (HttpMethods.IsPost(context.Request.Method))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(body);
                    if (jsonDoc.RootElement.TryGetProperty("method", out var methodProp) &&
                        methodProp.GetString() == "tools/call" &&
                        jsonDoc.RootElement.TryGetProperty("params", out var paramsProp) &&
                        paramsProp.TryGetProperty("name", out var nameProp))
                    {
                        string? toolName = nameProp.GetString();
                        if (!string.IsNullOrEmpty(toolName))
                        {
                            string? requiredScope = McpToolScopes.GetRequiredScope(toolName);
                            if (requiredScope != null && !principal.HasScope(requiredScope))
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";
                                context.Response.Headers["WWW-Authenticate"] =
                                    $"Bearer error=\"insufficient_scope\", scope=\"{requiredScope}\", resource_metadata=\"{metadataUrl}\"";

                                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                                {
                                    error = "insufficient_scope",
                                    error_code = "INSUFFICIENT_SCOPE",
                                    error_description = $"The caller does not have the required scope '{requiredScope}' for tool '{toolName}'.",
                                    scope = requiredScope
                                }), context.RequestAborted);
                                return;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Let downstream MCP handler handle malformed JSON
                }
            }
        }

        context.User = principal;
        await _next(context);
    }
}
