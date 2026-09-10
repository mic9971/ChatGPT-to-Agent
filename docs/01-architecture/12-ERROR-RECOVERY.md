# 12 — Error, Recovery, Idempotency and Concurrency

## Error taxonomy

- `CONFIG_*` configuration/user setup.
- `WORKSPACE_*` path/security/workspace mismatch.
- `MCP_*` protocol/transport/tool errors.
- `AUTH_*` authorization/pairing/token errors.
- `TUNNEL_*` public connectivity errors.
- `EXECUTION_*` record/artifact errors.
- `C2C_*` protocol/checkpoint conflicts.
- `RUNTIME_*` process/port/app-state failures.

Every error has a stable code, safe user message and optional local diagnostic id. Remote errors do not include stack trace/local full path.

## Process restart

Bridge restart does not invalidate a C2C task checkpoint. MCP itself is stateless at the 2026-07-28 protocol baseline. On startup, runtime validates app-state, workspace identity and auth material before accepting requests.

## Tunnel restart

Quick Tunnel URL may change. `ensure` detects this and returns `repair_required` when remote connector metadata must be updated. Named tunnel avoids this class of repair.

## Task resume

- If prior planning conversation is available: continue it; use local checkpoint to know expected next state.
- If unavailable: create HANDOFF from bounded checkpoint fields, then planner re-reads workspace through MCP.
- Never recreate auth/pairing merely because a task resumed.

## Executor lease

One active executor lease per task in V1. Lease has owner id + expiry. `record` and state-advance operations validate lease or explicit takeover token. This avoids two agents implementing the same plan simultaneously.

## Idempotent operations

- `start`, `stop`, `ensure`: idempotent relative to same workspace/runtime config.
- `record`: idempotent by idempotency key; repeat returns same execution id.
- pairing code consumption: intentionally non-idempotent, one-time.
- refresh token: rotated; replay of old token fails.

## Crash-safe state

Use atomic replace for JSON state and include a checksum/version where useful. If latest file is corrupt, do not silently load an older security state unless recovery is explicit and audited.
