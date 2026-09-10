# 06 — OAuth, MCP Authorization and Pairing

## Modern MCP direction

The current MCP 2026-07-28 authorization direction differs from older implementations:

- Protected Resource Metadata remains fundamental.
- Authorization-server discovery remains required when auth is enabled.
- OAuth authorization responses harden issuer validation (`iss`).
- Client ID Metadata Documents (CIMD) are the preferred modern registration direction.
- Dynamic Client Registration (DCR) is deprecated for new MCP designs and retained only for compatibility.

Therefore C2C.NET should **not** copy an older DCR-first design blindly.

## Proposed strategy

### Primary path
Use CIMD when the target ChatGPT/MCP client supports it reliably.

### Compatibility path
Support DCR only behind a compatibility feature flag/profile if required by an actual client. DCR credentials must be bound to the issuing authorization-server issuer and never reused across issuer changes.

### Controlled path
Pre-registration may be supported for a managed client/environment.

## Authorization endpoints/capabilities

Exact endpoint shape must match the selected OAuth server library and MCP spec. At minimum the deployment must provide:

- Protected Resource Metadata;
- Authorization Server Metadata or supported OIDC discovery;
- authorization-code flow with PKCE S256 for public/browser clients;
- token endpoint;
- revocation support;
- refresh-token rotation when refresh tokens are issued;
- issuer validation consistent with current MCP/OAuth guidance.

## OpenIddict evaluation

OpenIddict is a strong .NET building block for authorization code, refresh tokens, PKCE, validation and ASP.NET Core hosting. However, as of the research baseline, first-class DCR support is still tracked as an enhancement. That is acceptable because DCR is no longer the preferred new MCP registration mechanism. If a legacy compatibility profile needs DCR, implement a narrowly-scoped compatibility endpoint or adapter rather than distorting the entire auth architecture.

Do not hand-roll cryptographic primitives. Use framework/security-library implementations for PKCE verification, random generation, token protection/signing where possible.

## Token design

Recommended default:

- short-lived access token (about 1 hour unless interoperability dictates otherwise);
- refresh token only when `offline_access` is granted;
- rotate refresh token on use;
- bind authorization to `workspace_id`, `client_id` and issuer;
- store opaque token material only in secure form; if a lookup token must be persisted, persist a cryptographic hash, not raw token;
- use constant-time comparison for secret/token verifier checks.

## Scopes

Initial scopes:

```text
workspace.read
workspace.search
git.read
execution.read
offline_access
```

Each tool checks its required scope independently. Do not treat “authenticated” as authorization for every tool.

## Pairing UX

Pairing is a local approval bootstrap, not a replacement for OAuth security.

Suggested properties:

- CSPRNG-generated code with sufficient entropy;
- 5-minute TTL;
- one-time use;
- attempt cap;
- per-source rate limiting where source information is trustworthy;
- destroy session after success or attempt exhaustion;
- never emit access/refresh token into the human-visible C2C message.

## Secret storage

V1 can use user-only application state permissions plus Data Protection where appropriate. V1.1 should prefer platform secret stores for long-lived sensitive local state:

- macOS Keychain;
- Windows DPAPI/Credential Manager;
- Linux Secret Service/libsecret where available.

The bridge should remain usable in a degraded local-only mode when remote auth is not configured.
