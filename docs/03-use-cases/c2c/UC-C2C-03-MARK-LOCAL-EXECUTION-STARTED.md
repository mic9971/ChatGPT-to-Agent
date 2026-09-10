# UC-C2C-03 — Mark Local Execution Started

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-02 |

## Goal

Persist optional local EXECUTING checkpoint before modifying/building/testing.

## Trigger

Agent begins an accepted PLAN.

## Preconditions

- PLAN_RECEIVED checkpoint.

## Postconditions

- Checkpoint records EXECUTING and plan iteration.

## Scope

### In scope
- local state only
- start timestamp

### Out of scope
- remote MCP mutation
- new protocol requirement

## Referenced business rules

- `BR-C2C-002`
- `BR-C2C-008`
- `BR-CON-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate session is executable.
2. Persist EXECUTING local checkpoint/time.
3. Agent proceeds outside bridge to edit/build/test.

## Alternate / failure flows

- Process crashes after checkpoint -> resume knows execution may be incomplete and requires inspection before re-run.

## Detailed design

### Application contracts

- `IC2CSessionService.MarkExecutingAsync`

### Persistence

Session checkpoint.

### Security

No command/content captured automatically.

### Concurrency / idempotency

Older iteration cannot mark newer plan executing.

### Limits / pagination / timeouts

Small local record.

### Observability

State transition.

### Error codes

- `PROTOCOL_INVALID_STATE`

## Test design

### Unit tests
- transition validation

### Integration tests
- crash/reload fixture

### Adversarial / security tests
- double-start same iteration

## Acceptance criteria

- [ ] Checkpoint is optional protocol-wise but deterministic locally.

## Implementation checklist

- [ ] Session transition/tests

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
