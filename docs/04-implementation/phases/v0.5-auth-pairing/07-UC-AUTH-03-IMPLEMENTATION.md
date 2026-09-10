# UC-AUTH-03 Implementation — Issue and Validate Access Token

## Goal

Exchange a valid authorization code for short-lived access and optional refresh credentials, then validate authorization before every protected MCP tool invocation.

## Token requirements

Access authorization must be bound to:

```text
workspaceId
clientId
issuer
resource
scopes
grantId
expiry
```

The exact token wire format is owned by the selected vetted OAuth library profile. Do not invent a custom bearer-token format merely to simplify persistence.

## Flow — token exchange

1. Validate authorization code through library/protocol handler.
2. Validate client/redirect and PKCE verifier.
3. Enforce one-time code redemption.
4. Load active authorization grant.
5. Validate workspace/client/issuer/resource binding.
6. Issue short-lived access token.
7. Issue refresh token only if refresh policy is enabled and requested/allowed.
8. Persist only required protected/hash-indexed state.

## Flow — MCP validation

Before application dispatch:

1. authenticate bearer token;
2. validate expiry and issuer;
3. validate canonical MCP resource/audience;
4. validate workspace binding;
5. validate active grant/revocation state according to selected model;
6. determine granted scopes;
7. check tool-specific scope;
8. only then invoke the existing MCP application service.

## 401 vs 403

- absent/invalid/expired/wrong-resource token -> `401` authorization challenge;
- authenticated token missing required scope -> `403` with `insufficient_scope` challenge metadata;
- application service must not run on either failure.

## Refresh interoperability

If refresh is enabled, authorization-server discovery must accurately advertise support. `offline_access` is requested/advertised according to current target-client and OAuth/OIDC interoperability evidence; it is not a tool scope.

## Tool scope mapping

```text
workspace_info      workspace.read
read_file           workspace.read
list_directory      workspace.read
search_workspace    workspace.search
git_status          git.read
git_diff            git.read
execution_summary   execution.read
test_status          execution.read
execution_output    execution.read
```

## Tests

- valid code + PKCE exchange;
- code replay rejected;
- expired access token -> 401;
- wrong issuer -> 401;
- wrong resource -> 401;
- wrong workspace -> 401/deny according to safe profile;
- missing scope -> 403 before service invocation;
- each of the nine tools has exact scope mapping test;
- access token/Authorization header absent from logs;
- local-only profile remains explicit and cannot be activated by public/tunnel path accidentally.

## Completion report

```text
STATE: EXECUTED
TARGET_UC: UC-AUTH-03
TOKEN_PROFILE: ...
ACCESS_TTL: ...
REFRESH_ENABLED: yes|no
RESOURCE_BINDING: ...
WORKSPACE_BINDING: ...
TOOL_SCOPE_TESTS: ...
TESTS: ...
DEVIATIONS: ...
```
