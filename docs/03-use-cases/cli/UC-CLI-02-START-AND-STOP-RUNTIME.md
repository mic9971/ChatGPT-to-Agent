# UC-CLI-02 — Start and Stop Runtime

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Local user / Agent |
| Depends on | UC-MCP-01, UC-TUN-01, UC-TUN-02 |

## Goal

Start/stop owned bridge runtime and optional tunnel according to profile.

## Trigger

`c2c start` / `c2c stop`.

## Preconditions

- Workspace configured.

## Postconditions

- Start creates one healthy owned runtime; stop removes owned runtime without touching foreign processes.

## Scope

### In scope
- bridge process lifecycle
- optional tunnel lifecycle
- ownership metadata

### Out of scope
- kill by name
- public wildcard bind

## Referenced business rules

- `BR-CON-003`
- `BR-CON-008`
- `BR-SEC-005`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Start: validate ownership, launch bridge, health probe, optionally tunnel.
2. Stop: stop tunnel then bridge using ownership identity and bounded graceful shutdown.

## Alternate / failure flows

- Already started/stopped -> idempotent.
- Port foreign-owned -> conflict.

## Detailed design

### Application contracts

- `IRuntimeController.StartAsync/StopAsync`

### Persistence

Runtime ownership state.

### Security

Only owned processes terminated.

### Concurrency / idempotency

Owner lock and idempotent lifecycle.

### Limits / pagination / timeouts

Startup/shutdown timeouts.

### Observability

Transition logs/metrics.

### Error codes

- `C2C_CONFLICT`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- lifecycle transitions

### Integration tests
- fake/child process integration

### Adversarial / security tests
- PID reuse/foreign port

## Acceptance criteria

- [ ] Repeated start/stop safe.
- [ ] Foreign process untouched.

## Implementation checklist

- [ ] Runtime controller
- [ ] CLI adapters/tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
