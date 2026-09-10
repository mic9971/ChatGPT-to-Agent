# Detailed V0.5 Authorization Design

## Responsibility split

```text
C2C.Core/Authorization
  - pairing/auth domain contracts
  - scope names and authorization decisions
  - workspace/client/grant identity models
  - no ASP.NET/OpenIddict types

C2C.Infrastructure/Authorization
  - filesystem auth/pairing state
  - CSPRNG/hash helpers behind owned abstractions
  - CIMD HTTP metadata resolver if enabled
  - retention/cleanup

C2C.Host/Auth
  - OAuth/OpenIddict integration
  - discovery/metadata endpoints
  - authorize/token/revoke HTTP adapters
  - authentication/authorization policies
  - MCP challenge/error integration
```

Do not introduce global `Models`, `Services`, `Interfaces`, `Helpers` folders.

## Core contracts

Exact names may be adjusted to nearby conventions, but the capabilities should remain small and owned:

```text
IPairingService
IPairingStore
IAuthorizationStateStore
IAuthorizationRevoker
IClientMetadataResolver
IAuthorizationClock (prefer TimeProvider adapter if needed)
```

Do not create interfaces merely for mocking. Each interface above must represent persistence, network, nondeterministic time/randomness, or a security boundary.

## Pairing state

Conceptual record:

```text
PairingSession
  pairingSessionId
  workspaceId
  codeHash
  createdAt
  expiresAt
  attemptsRemaining
  status = Active | Consumed | Expired | Locked
  approvedClientId?
  schemaVersion
```

Plain pairing code exists only long enough to return to the local caller. Persist a cryptographic lookup/verifier representation, not the display value where avoidable.

## Authorization grant state

Conceptual record:

```text
AuthorizationGrant
  grantId
  workspaceId
  clientId
  issuer
  resource
  scopes[]
  status = Active | Revoked
  createdAt
  revokedAt?
  refreshFamilyId?
  schemaVersion
```

The grant is the revocation anchor. Access-token validation must be able to determine whether the grant/workspace/client/resource is still authorized according to the selected token model.

## Refresh family state

Conceptual record:

```text
RefreshFamily
  familyId
  grantId
  currentTokenHash
  generation
  expiresAt
  status = Active | Revoked | ReplayDetected
  updatedAt
```

Rotation is serialized per family. A predecessor cannot be redeemed twice.

## Filesystem persistence

Follow `11-DATA-MODEL.md`:

```text
workspaces/<workspace-id>/auth/
  pairing.json
  grants.json
  refresh-families.json
  client-metadata-cache.json?   # optional, bounded, non-secret
```

Security state writes:

1. acquire capability lock;
2. reload state inside lock;
3. validate schema/invariants;
4. mutate in memory;
5. write temp file with user-only permissions;
6. flush;
7. atomic replace/rename;
8. release lock.

Corrupt auth state fails closed. Never silently reset authorization state to "empty/allow".

## Token/profile strategy

The selected OAuth library handles standard protocol parsing and cryptographic token/code protection wherever possible. C2C.NET owns workspace/client/resource/scopes and revocation semantics.

The production implementation must not hand-roll PKCE, signature algorithms, authorization-code cryptography or token serialization when the vetted library provides them.

If filesystem-only persistence requires OpenIddict degraded/custom-store integration, V0.5A must prove which validations are still provided by the library and which are explicitly implemented by C2C.NET.

## Scope model

V1 operational scopes:

```text
workspace.read
workspace.search
git.read
execution.read
```

`offline_access` is authorization-server/client refresh semantics, not an MCP tool permission.

Tool mapping:

```text
workspace_info      -> workspace.read
read_file           -> workspace.read
list_directory      -> workspace.read
search_workspace    -> workspace.search
git_status          -> git.read
git_diff            -> git.read
execution_summary   -> execution.read
test_status         -> execution.read
execution_output    -> execution.read
```

No wildcard/all-powerful scope in V1.

## Metadata endpoints

Protected resource metadata must identify the canonical MCP resource and corresponding authorization server. Authorization-server metadata must advertise only capabilities actually implemented and tested.

Do not advertise:

- CIMD support until resolver validation is proven;
- DCR unless compatibility profile enabled;
- refresh capability unless refresh flow is enabled and tested;
- scopes not actually enforceable.

## 401/403 behavior

Unauthenticated/invalid token:

```text
HTTP 401
WWW-Authenticate: Bearer ... resource_metadata="..."
```

Authenticated but insufficient scope:

```text
HTTP 403
WWW-Authenticate: Bearer error="insufficient_scope" scope="..." resource_metadata="..."
```

The application service is not invoked when authorization fails.

## Local-only mode

Remote auth can be disabled for local development, but this must be an explicit runtime profile. A public/tunneled endpoint must never silently fall back to no-auth mode.

## Logging

Allowed:

- workspace id (safe internal id)
- client safe id/hash prefix
- grant safe id
- scope name
- result category
- duration/correlation id

Forbidden:

- pairing code
- authorization code
- access token
- refresh token
- Authorization header
- PKCE verifier
- raw CIMD response on failure
