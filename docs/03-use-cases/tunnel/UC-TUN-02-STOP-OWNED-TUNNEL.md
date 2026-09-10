# UC-TUN-02 — Stop Owned Tunnel

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.4 |
| Module | Tunnel & Runtime Connectivity |
| Primary actor | CLI/runtime |
| Depends on | UC-TUN-01 |

## Goal

Stop only the tunnel process/session owned by the current workspace runtime.

## Trigger

`c2c stop` or shutdown.

## Preconditions

- Session metadata may or may not exist.

## Postconditions

- Owned tunnel stopped and state removed; foreign processes untouched.

## Scope

### In scope
- graceful stop
- bounded force-kill owned process
- state cleanup

### Out of scope
- kill by process name
- kill foreign/stale reused PID without ownership validation

## Referenced business rules

- `BR-CON-004`
- `BR-CON-008`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Acquire lifecycle lock.
2. Load session metadata.
3. Validate process ownership marker/start-time identity.
4. Request graceful termination.
5. After timeout, force terminate only if still same owned process.
6. Remove session metadata atomically.

## Alternate / failure flows

- No session -> idempotent success.
- PID reused/ownership mismatch -> do not kill; clear stale metadata/report warning.

## Detailed design

### Application contracts

- `ITunnelProvider.StopAsync`
- owned process identity validator

### Persistence

Remove session metadata after verified stop/stale resolution.

### Security

Never terminate unowned process.

### Concurrency / idempotency

Serialized stop/start; idempotent stop.

### Limits / pagination / timeouts

Grace/kill timeout.

### Observability

Transition and safe ownership mismatch reason.

### Error codes

- `C2C_CONFLICT`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- idempotent stop
- pid reuse check

### Integration tests
- fake process lifecycle

### Adversarial / security tests
- stale PID points to unrelated process

## Acceptance criteria

- [ ] Foreign process never killed.
- [ ] Repeated stop succeeds.

## Implementation checklist

- [ ] Implement ownership identity
- [ ] Stop lifecycle
- [ ] Tests

## Architecture references

- `01-architecture/07-TUNNEL-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
