# UC-AGT-02 — Agent Starts C2C Task

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Execution Agent |
| Depends on | UC-CLI-04, UC-C2C-01, UC-CTRL-01, UC-CTRL-02, UC-CTRL-03, UC-CTRL-04 |

## Goal

Standardize how an execution agent ensures runtime readiness, opens/attaches the task-bound planning conversation, sends INIT and accepts a validated PLAN.

## Main flow

1. Agent calls `c2c ensure --json`.
2. If user action is required, stop and surface the exact requirement.
3. Start/load local task session (`UC-CTRL-01`).
4. Open/attach ChatGPT conversation (`UC-CTRL-02`).
5. Relay validated INIT (`UC-CTRL-03`).
6. Receive/validate PLAN (`UC-CTRL-04`).
7. Route accepted PLAN to `UC-AGT-03` for local execution.

## Referenced business rules

- `BR-COM-004`
- `BR-C2C-001`
- `BR-C2C-003`
- `BR-CTRL-003`
- `BR-CTRL-004`
- `BR-CTRL-005`

## Security

The agent may not fabricate READY, bypass MCP pairing/auth, capture ChatGPT account credentials, or paste repository evidence into control messages.

## Failure flows

- NEED_PAIRING/NEED_SETUP -> user action.
- ChatGPT login/MFA/CAPTCHA -> `CTRL_USER_AUTH_REQUIRED`.
- Conversation unavailable -> explicit HANDOFF/recovery path, not silent replacement.

## Tests

- ensure READY -> INIT -> PLAN;
- NEED_PAIRING;
- browser auth required;
- wrong task/iteration planner response rejected.

## Acceptance criteria

- [ ] Agent stops for required setup/auth actions instead of bypassing them.
- [ ] INIT/PLAN use the control-plane driver while workspace evidence remains MCP-only.

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`

## Agent implementation rule

Implement only this UC and its explicit dependencies. Do not pre-build later use cases.
