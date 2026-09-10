# Current MCP/Auth Spec and Interoperability Notes

**Research checked:** 2026-09-10.

This file records external facts that can change independently of the repository. Re-check them before implementation and before release.

## MCP 2026-07-28 direction

The 2026-07-28 MCP authorization revision hardens OAuth integration:

- authorization servers should return `iss` in authorization responses and clients validate it before code redemption;
- credentials/registration state are issuer-bound and must not be reused across authorization servers;
- Client ID Metadata Documents (CIMD) are the standard direction;
- Dynamic Client Registration (DCR) is formally deprecated and retained only for backward compatibility;
- Protected Resource Metadata remains fundamental for protected-resource discovery.

Official release notes:
`https://blog.modelcontextprotocol.io/posts/2026-07-28/`

Protected Resource Metadata is standardized by RFC 9728:
`https://datatracker.ietf.org/doc/html/rfc9728/`

Authorization Server Metadata is RFC 8414:
`https://datatracker.ietf.org/doc/html/rfc8414/`

## Resource binding

The MCP authorization profile requires clients to target the intended protected resource. V0.5 must treat the canonical MCP resource URI as an authorization boundary and validate that tokens are intended for this resource/workspace deployment.

Do not accept an otherwise valid token issued for another MCP resource.

## ChatGPT interoperability evidence

Current OpenAI guidance for custom MCP apps confirms:

- ChatGPT can configure a remote MCP server with OAuth;
- OAuth setup follows the provider's discovery metadata;
- for long-lived connectivity, refresh-token issuance matters;
- for OIDC-style providers, `offline_access` should be advertised in discovery and requested to obtain refresh access where supported.

Reference:
`https://help.openai.com/en/articles/12584461-developer-mode-apps-and-full-mcp-connectors-in-chatgpt-beta`

The public OpenAI guidance does not, by itself, freeze every lower-level registration detail for our target deployment. Therefore V0.5 starts with an interoperability spike instead of assuming a specific ChatGPT DCR/CIMD behavior.

## OpenIddict baseline

OpenIddict remains the preferred .NET candidate for protocol/token primitives. Current stable documentation shows 7.7.x supports ASP.NET Core 8/9/10 and server + validation stacks.

References:

- `https://documentation.openiddict.com/integrations/aspnet-core`
- `https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance`
- `https://documentation.openiddict.com/configuration/token-storage`

Important constraint: OpenIddict's normal revocation/token-storage model expects backing storage. Its degraded mode removes many automatic client/redirect/revocation validations and requires custom handlers. Since C2C.NET V1 explicitly uses filesystem app-state, the agent must prove a safe integration approach before adding production auth endpoints.

Do not introduce SQLite/EF merely to make the library easy to configure unless an ADR explicitly changes `11-DATA-MODEL.md`.

## Mandatory V0.5A spike questions

Before production auth code, answer with tests/prototype evidence:

1. Can the selected OpenIddict stable version handle our authorization-code + PKCE + refresh flow while keeping repository-approved filesystem persistence?
2. Which responsibilities remain custom in degraded/custom-store mode?
3. How is `iss` added/validated?
4. How are the protected-resource and authorization-server metadata documents served?
5. How is the MCP `resource` audience enforced?
6. Can CIMD be supported without unsafe SSRF behavior?
7. Does the actual target ChatGPT client use CIMD, DCR, or another configured registration path?
8. How is `offline_access` advertised/requested for refresh interoperability?
9. Can revoke/unpair invalidate future remote access without a database?

If any security-critical answer is unclear, stop and create an ADR rather than hand-roll around the uncertainty.

## CIMD fetch security

If C2C.NET accepts URL-formatted CIMD client IDs, treat metadata retrieval as SSRF-sensitive network input:

- HTTPS only;
- non-root path required;
- reject userinfo/fragments;
- bounded DNS/connect/read timeout;
- bounded response bytes;
- JSON content only;
- `client_id` in document must exactly equal requested metadata URL;
- redirect URIs must match metadata;
- redirects disabled or each redirect fully revalidated;
- private/link-local/loopback targets denied unless an explicitly approved compatibility profile requires otherwise;
- DNS rebinding/resolution changes fail closed;
- cache only bounded validated metadata.

Do not advertise CIMD support until this resolver passes adversarial tests.

## DCR policy

DCR is OFF by default. If the interoperability spike proves a target client requires it:

1. document the evidence;
2. create a compatibility ADR/profile;
3. bind registration state to issuer;
4. rate-limit registration;
5. validate redirect URIs/application type;
6. never allow DCR support to weaken the CIMD/pre-registration path.
