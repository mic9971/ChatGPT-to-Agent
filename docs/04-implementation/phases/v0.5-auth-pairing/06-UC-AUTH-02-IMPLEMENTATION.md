# UC-AUTH-02 Implementation — Approve Pairing and Authorize Client

## Goal

Bind one valid pairing approval to one validated OAuth authorization request and issue a single-use authorization grant/code through vetted OAuth server primitives.

## Mandatory prerequisite

V0.5A auth-substrate spike must be accepted before this UC. Do not improvise protocol behavior around library gaps.

## Expected placement

```text
C2C.Core/Authorization/
  AuthorizationScope.cs / AuthorizationScopes.cs
  AuthorizationGrant.cs
  IAuthorizationStateStore.cs
  IClientMetadataResolver.cs

C2C.Infrastructure/Authorization/
  JsonAuthorizationStateStore.cs
  CimdClientMetadataResolver.cs      # only if profile enabled

C2C.Host/Auth/
  AuthorizationServerConfigurator.cs
  AuthorizationEndpoint.cs|handler
  metadata endpoint/configuration
```

## Flow

1. Receive authorization request through selected OAuth server integration.
2. Validate issuer/resource/client registration profile.
3. Validate exact redirect URI.
4. Require authorization-code flow and PKCE `S256` for public/browser clients.
5. Validate requested scopes against V1 allowlist.
6. Validate pairing code/session for the same workspace.
7. Atomically consume pairing session.
8. Create authorization grant bound to:
   - workspace id
   - client id
   - issuer
   - canonical MCP resource URI
   - granted scopes
9. Issue protected, short-lived authorization code using library primitives.
10. Return OAuth-compatible response including issuer behavior required by current MCP profile.

## CIMD profile

If CIMD is selected:

- resolve client metadata through the hardened resolver;
- require metadata `client_id` exact match;
- validate redirect URI against metadata;
- cache only validated bounded metadata;
- never allow metadata fetch failure to fall back to an unvalidated client.

## DCR profile

Do not implement DCR here by default. If the interoperability spike proves DCR is required, stop and create/approve a compatibility ADR before adding `/register`.

## Pairing consumption race

Two concurrent authorization requests using the same pairing session must produce exactly one successful consume. The losing request receives a safe OAuth/auth error and cannot reuse the session.

## Tests

- valid authorization request + valid pairing;
- PKCE S256 required;
- `plain`/missing PKCE rejected where public-client policy applies;
- redirect mismatch rejected;
- unsupported scope rejected;
- wrong workspace pairing rejected;
- expired/locked/consumed pairing rejected;
- concurrent consume: one success;
- issuer/resource mismatch rejected;
- CIMD malformed/oversized/SSRF cases when CIMD enabled;
- no authorization code/pairing code in logs.

## Completion report

```text
STATE: EXECUTED
TARGET_UC: UC-AUTH-02
AUTH_LIBRARY_PROFILE: ...
CLIENT_REGISTRATION_PROFILE: CIMD | preregistered | compatibility-DCR
PKCE_POLICY: ...
PAIRING_CONSUME_ATOMICITY: ...
SCOPES: ...
TESTS: ...
DEVIATIONS: ...
```

Stop after authorization-code issuance is proven. Do not expose protected MCP yet.
