# V0.6 CLI Contract and Exit Codes

## Principle

Human output is presentation. `--json` is an agent API and must be versioned, bounded and deterministic.

## Common JSON envelope

Every agent-facing command should return one JSON object to stdout:

```json
{
  "schemaVersion": 1,
  "command": "ensure",
  "status": "ready",
  "errorCode": null,
  "actionRequired": null,
  "data": {},
  "warnings": []
}
```

Rules:

- no banners/progress text may be mixed into stdout in `--json` mode;
- diagnostics/progress go to stderr only when explicitly allowed and must never contain secrets;
- scripts depend on `schemaVersion`, `command`, `status`, `errorCode` and documented `data` fields, not `message` wording;
- optional fields must have stable omission/null semantics;
- no absolute local path appears in machine output unless the specific command contract marks it local-safe and necessary;
- tokens, pairing verifier/hash, auth code, refresh token, private keys and raw provider credentials are forbidden.

## Status vocabulary

Use command-specific status values under a bounded common taxonomy:

```text
success
ready
not_configured
not_ready
pairing_required
repair_required
degraded
conflict
denied
failed
cancelled
```

Do not invent a new spelling in individual command handlers without updating this contract.

## Action-required vocabulary

For agent automation, prefer a stable action code rather than natural-language remediation:

```text
run_setup
run_pair
retry
run_doctor
resolve_port_conflict
install_dependency
manual_intervention
none
```

`actionRequired` is advisory; it never grants permission for an unsafe action.

## Exit codes

V0.6 defines:

| Exit | Meaning |
|---:|---|
| 0 | command completed successfully / requested state reached |
| 2 | invalid usage or invalid configuration |
| 3 | valid command but action is required before readiness, e.g. setup/pairing |
| 4 | security/authentication/authorization denial |
| 5 | required external dependency unavailable |
| 6 | ownership/port/runtime conflict |
| 7 | timeout or cancellation |
| 10 | unexpected internal failure |

A command-specific `errorCode` carries the precise failure reason. Exit codes remain coarse and stable.

## `setup --json`

Minimum data:

```json
{
  "workspaceId": "...",
  "configured": true,
  "capabilities": {
    "git": "available",
    "tunnel": "available"
  },
  "nextAction": "run_start"
}
```

Never expose canonical absolute path in agent JSON by default.

## `status --json`

Minimum data:

```json
{
  "workspace": "configured",
  "bridge": "healthy",
  "tunnel": "healthy",
  "authorization": "paired",
  "publicEndpoint": "https://..."
}
```

`publicEndpoint` is not treated as a secret but must not imply authorization is bypassed.

## `doctor --json`

Stable check ids:

```json
{
  "checks": [
    {
      "id": "workspace.config",
      "status": "pass",
      "errorCode": null,
      "actionRequired": null
    }
  ]
}
```

Check ids are API. Natural-language descriptions are not.

Suggested initial ids:

```text
runtime.dotnet
workspace.config
workspace.permissions
workspace.policy
git.binary
host.binary
bridge.port
bridge.health
tunnel.provider
tunnel.state
auth.metadata
auth.pairing
appstate.integrity
logs.writable
```

## `ensure --json`

Map the existing runtime state machine without inventing parallel policy:

```text
Runtime Ready       -> status=ready, exit=0
Need Setup          -> status=not_configured, action=run_setup, exit=3
Need Pairing        -> status=pairing_required, action=run_pair, exit=3
Dependency Missing  -> status=repair_required, action=install_dependency, exit=5
Foreign Port        -> status=conflict, action=resolve_port_conflict, exit=6
Timeout             -> status=failed/cancelled as applicable, exit=7
```

## `pair --json`

Pairing code is intentionally human-sensitive and short-lived. Default machine mode should return pairing metadata/URL/instructions without a bearer or refresh token. If automation requires displaying the short-lived pairing code, the field must be explicitly documented as sensitive, never logged, and only emitted by the `pair` command itself.

## `record --json`

Return at minimum:

```json
{
  "executionId": "...",
  "recorded": true,
  "idempotentReplay": false
}
```

No raw artifact body is echoed back.

## `logs --json`

Returns bounded structured entries or a bounded tail object. It never accepts arbitrary path and never returns unredacted log storage bytes.

## Compatibility rule

Adding optional fields is allowed within schema version 1 when old consumers remain correct. Renaming/removing fields, changing status meaning or exit mapping requires a schema-version decision and compatibility review.
