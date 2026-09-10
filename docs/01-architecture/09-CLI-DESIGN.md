# 09 — CLI Design

## UX principle

Humans get concise progress; agents get stable JSON. Commands should be safe to retry where possible.

## Commands

### `c2c setup [workspace]`
Validate prerequisites, create app-state workspace identity/config, validate security rules, optionally configure tunnel/auth, start bridge, perform MCP file-read self-test.

### `c2c start`
Start bridge for the configured workspace. Idempotent if already running with the same config.

### `c2c stop`
Stop only runtime processes owned by this workspace/runtime instance.

### `c2c status [--json]`
Return bridge/tunnel/auth/workspace health without leaking secrets.

### `c2c doctor [--json]`
Check .NET/runtime packaging, Git, cloudflared/provider, filesystem permissions, app-state corruption, port conflict, auth metadata and workspace policy configuration.

### `c2c ensure [--json]`
Primary agent command. Ensure workspace config, bridge and tunnel are ready. Return a state machine result rather than asking the agent to understand infrastructure.

Example:

```json
{
  "schemaVersion": 1,
  "status": "ready",
  "bridge": "healthy",
  "tunnel": "healthy",
  "pairing": "paired"
}
```

Other statuses: `setup_required`, `pairing_required`, `repair_required`, `workspace_mismatch`, `failed`.

### `c2c pair` / `c2c unpair`
Create approval bootstrap or revoke local/remote authorization state as supported.

### `c2c session`
Read/update bounded local C2C task checkpoint. `--json` is the agent contract.

### `c2c record`
Persist execution evidence. Must be idempotent by caller key or execution id.

### `c2c logs`
Read local runtime logs; default tail is bounded. Agent integration should not paste logs into C2C messages.

## Exit codes

Keep a documented stable set, e.g. 0 success, 2 usage/config, 3 not-ready/pairing, 4 security-policy denial, 5 dependency unavailable, 10 internal error. JSON includes a more specific `errorCode`.

## Config precedence

CLI argument > environment variable > workspace app-state config > global app-state default. Project repository files should not be able to silently override security-sensitive global options.
