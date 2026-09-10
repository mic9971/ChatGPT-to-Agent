# Release Gates

## Gate A — Local data plane
- Workspace boundary adversarial suite green.
- MCP read-only catalog verified.
- No public bind.

## Gate B — Evidence plane
- Git secret-diff tests green.
- Execution artifact sanitizer/restricted tests green.
- Idempotent execution record tests green.

## Gate C — Remote connectivity
- Tunnel ownership/recovery tests green.
- Auth issuer/PKCE/scope/workspace/replay tests green.
- Unpair/revocation scenario green.

## Gate D — Control plane
- C2C state transition and crash/resume fixtures green.
- No raw evidence in C2C message fixtures.

## Gate E — Agent E2E
- Antigravity performs at least one two-iteration PLAN -> EXECUTED -> REPLAN -> EXECUTED -> DONE scenario.
- Planner independently reads diff/test evidence through MCP.

## Gate F — Packaging
- Self-contained artifacts smoke-tested on target platforms.
- No secrets included in archive.
