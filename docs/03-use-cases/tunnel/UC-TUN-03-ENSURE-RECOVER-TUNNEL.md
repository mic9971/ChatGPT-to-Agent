# UC-TUN-03 — Ensure/Recover Tunnel

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.4 |
| Module | Tunnel & Runtime Connectivity |
| Primary actor | Execution Agent / CLI |
| Depends on | UC-TUN-01, UC-TUN-02, UC-MCP-01 |

## Goal

Converge bridge+tunnel runtime to READY from stopped, stale or partially failed local state.

## Trigger

`c2c ensure --json`.

## Preconditions

- Workspace configured.

## Postconditions

- Returns READY with one healthy bridge/tunnel or a precise action-required error.

## Scope

### In scope
- health checks
- stale state repair
- start/reuse
- machine-readable status

### Out of scope
- infinite restart loop
- silent re-pair/credential bypass

## Referenced business rules

- `BR-CON-003`
- `BR-CON-004`
- `BR-COM-007`
- `BR-COM-008`
- `BR-COM-011`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Acquire runtime owner/lifecycle coordination.
2. Check bridge health; start owned bridge if absent.
3. Validate persisted tunnel session; reuse if healthy.
4. Repair stale owned state then start provider if needed.
5. Check pairing/auth readiness separately.
6. Return status enum READY/NEED_PAIRING/DEGRADED/ERROR.

## Alternate / failure flows

- Bridge port occupied by foreign process -> conflict.
- Tunnel cannot start -> return deterministic failure; no endless retries.
- Pairing missing -> NEED_PAIRING, do not fake success.

## Detailed design

### Application contracts

- `IRuntimeEnsurer.EnsureAsync`
- `RuntimeEnsureResult`

### Persistence

Runtime/tunnel ownership state.

### Security

No authorization bypass when connectivity is restored.

### Concurrency / idempotency

Concurrent ensure converges via locks and idempotent checks.

### Limits / pagination / timeouts

Each stage has timeout; bounded retry policy only for transient provider readiness.

### Observability

Stage timings/status transitions.

### Error codes

- `C2C_NOT_READY`
- `C2C_CONFLICT`
- `TUNNEL_START_FAILED`

## Test design

### Unit tests
- ensure state machine

### Integration tests
- fake bridge+tunnel matrix

### Adversarial / security tests
- foreign port owner
- stale session
- pairing absent

## Acceptance criteria

- [ ] `ensure` is safe to call repeatedly by agents.
- [ ] READY means health checks actually pass.

## Implementation checklist

- [ ] Implement state machine
- [ ] JSON contract
- [ ] Tests

## Architecture references

- `01-architecture/07-TUNNEL-DESIGN.md`
- `01-architecture/09-CLI-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
