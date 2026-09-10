# V0.6C — UC-CLI-05 Pair and Unpair Commands

## Dependency gate

This slice starts only after V0.5 Authorization/Pairing is implemented and accepted. The CLI is only an adapter over those services.

Required capabilities:

```text
IPairingService or accepted equivalent
IAuthorizationRevoker or accepted equivalent
safe paired-client query/selector
real pairing/readiness state
```

Do not invent a second auth store or token format in the CLI.

## `c2c pair`

### Goal

Create one short-lived local approval bootstrap for pairing a remote MCP client.

### Flow

```text
parse safe options
 -> validate workspace/auth profile
 -> call pairing service
 -> receive short-lived display result
 -> render human instructions or bounded JSON
```

### Security

The pairing code is sensitive even though short-lived.

Rules:

- display only as part of the explicit pair command result;
- never write it to normal diagnostic logs;
- never persist plaintext merely for CLI convenience;
- never print access token, refresh token, authorization code, PKCE verifier or client secret;
- no automatic browser credential handling;
- repeated pair behavior follows V0.5 pairing policy rather than CLI-specific rotation logic.

### Suggested JSON

```json
{
  "schemaVersion": 1,
  "command": "pair",
  "status": "success",
  "errorCode": null,
  "actionRequired": "manual_intervention",
  "data": {
    "pairingUrl": "https://...",
    "expiresInSeconds": 300,
    "pairingCode": "ABCD-EFGH"
  },
  "warnings": ["pairingCode is short-lived sensitive output; do not log it"]
}
```

If the implementation decides not to emit the code in JSON mode, document that choice and provide a deterministic human-only workflow. Do not silently change it later.

## `c2c unpair`

### Goal

Revoke a selected client's authorization for the current workspace.

### Selection

Use safe client metadata/id only. Never list token values.

Non-interactive/agent mode requires an explicit stable client id when more than one candidate exists. Do not guess.

### Flow

```text
resolve current workspace
 -> resolve safe client id
 -> call V0.5 revoker
 -> revoke grant/refresh family/access semantics according to accepted auth model
 -> clear local pairing association
 -> return idempotent result
```

Already-unpaired should remain idempotent where V0.5 defines it that way.

## Command/output boundary

CLI does not:

- validate OAuth signatures itself;
- rotate refresh tokens;
- fetch CIMD metadata;
- inspect bearer token contents for business decisions;
- modify another workspace's auth state.

## Tests

- pair delegates once to pairing service;
- pairing code absent from captured logger;
- no access/refresh token appears in stdout/stderr;
- pair expiry/attempt errors map to stable exit/error codes;
- unpair unknown/already-unpaired behavior is deterministic;
- unpair wrong-workspace client denied;
- after unpair, V0.5 integration test proves protected MCP access/refresh is denied according to policy;
- concurrent unpair is safe/idempotent;
- JSON snapshot contains only approved fields.

## Completion gate

```text
[ ] CLI contains no auth/token policy
[ ] no long-lived credential output
[ ] pairing code never enters normal logs
[ ] unpair is workspace-bound
[ ] actual V0.5 revoke semantics are preserved
[ ] auth security suite remains green
```
