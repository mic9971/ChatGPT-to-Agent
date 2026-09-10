using System;

namespace C2C.Host.Auth;

/// <summary>
/// Configuration options for the C2C OAuth 2.1 authorization server adhering to 04-DETAILED-AUTH-DESIGN.md.
/// </summary>
public sealed class AuthorizationServerOptions
{
    public string Issuer { get; set; } = "https://127.0.0.1:5000/";

    public string Resource { get; set; } = "http://127.0.0.1:5000/mcp";

    /// <summary>
    /// When true, disables HTTPS requirement in OpenIddict for local test fixtures (BR-SEC-005 loopback testing).
    /// Defaults to false for secure production operation.
    /// </summary>
    public bool DisableTransportSecurityRequirement { get; set; }

    /// <summary>
    /// When true, enforces OAuth 2.1 authentication and tool scope validation on MCP endpoints.
    /// Defaults to true; can only be disabled for explicit local-only development mode (10-MCP-AUTHORIZATION-INTEGRATION.md).
    /// </summary>
    public bool EnableRemoteAuthorization { get; set; } = true;
}
