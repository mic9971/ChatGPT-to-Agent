# V0.5 Definition of Done and V0.6 Handoff

## V0.5 is DONE when

All of the following are true:

```text
[ ] V0.4 predecessor gate was green before auth exposure
[ ] auth substrate decision report accepted
[ ] selected OAuth library/profile pinned and documented
[ ] filesystem-only V1 persistence preserved or explicit ADR approved
[ ] UC-AUTH-01 pairing session implemented
[ ] UC-AUTH-02 authorization + pairing approval implemented
[ ] UC-AUTH-03 token issue/validation implemented
[ ] UC-AUTH-04 refresh rotation implemented when refresh enabled
[ ] UC-AUTH-05 revoke/unpair capability implemented
[ ] UC-MCP-02 protected-call path complete
[ ] Protected Resource Metadata works
[ ] Authorization Server Metadata works
[ ] PKCE S256 enforced
[ ] issuer/resource/workspace/client bindings enforced
[ ] all nine MCP tools use exact read-only scope mapping
[ ] 401/403 challenge behavior is correct
[ ] pairing/auth-code/refresh replay tests pass
[ ] revocation behavior proven
[ ] CIMD security tests pass if CIMD advertised
[ ] DCR absent unless compatibility ADR/profile approved
[ ] secret canary tests pass
[ ] full build green
[ ] full tests green
[ ] no CLI/control-plane runtime work bundled
```

## Expected remote flow after V0.5

```text
ChatGPT / MCP client
  -> GET protected-resource metadata
  -> discover authorization server
  -> resolve supported client registration profile
  -> authorization request + PKCE + resource/scopes
  -> local user pairing approval
  -> authorization code
  -> token exchange
  -> bearer token
  -> /mcp
  -> authentication
  -> workspace/resource binding
  -> tool scope check
  -> existing read-only capability
```

Refresh, when enabled:

```text
expired/renewal-needed access
  -> refresh grant
  -> rotate refresh token
  -> new access + refresh
```

Unpair:

```text
local revoke capability
  -> grant revoked
  -> refresh family revoked
  -> future protected access denied
```

## Handoff to V0.6 CLI

V0.6 should only add command/adapters around already-proven capabilities:

```text
c2c pair
  -> IPairingService

c2c unpair
  -> IAuthorizationRevoker

c2c status/doctor
  -> safe auth/tunnel status only
```

V0.6 must not redesign OAuth, token semantics or pairing persistence unless V0.5 surfaced a documented defect.

## Handoff evidence

Before V0.6 starts, preserve a phase report containing:

```text
AUTH_PROFILE:
CLIENT_REGISTRATION_PROFILE:
MCP_RESOURCE_URI_POLICY:
SCOPES:
ACCESS_TTL:
REFRESH_POLICY:
REVOCATION_POLICY:
PAIRING_POLICY:
PUBLIC_METADATA_ENDPOINTS:
TEST_TOTAL:
BUILD_RESULT:
MANUAL_CHATGPT_SMOKE: PASS | NOT_RUN | BLOCKED
KNOWN_COMPATIBILITY_LIMITATIONS:
```

Do not start V0.6 automatically after the final V0.5 review. The user/reviewer explicitly opens the next phase.
