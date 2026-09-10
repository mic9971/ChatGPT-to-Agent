# UC-C2C-07 — Handle BLOCKED or ERROR

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Planner / Runtime / Execution Agent |
| Depends on | UC-C2C-01 |

## Goal

Represent a task that cannot progress due to missing input/capability (BLOCKED) or protocol/infrastructure fault (ERROR).

## Trigger

Planner/agent/runtime encounters a stop condition.

## Preconditions

- Active task.

## Postconditions

- Session records bounded reason and next recoverable action when known.

## Scope

### In scope
- reason code
- safe message
- recoverability
- next action

### Out of scope
- stack trace/raw log in control message
- mislabeling ordinary defect as infrastructure ERROR

## Referenced business rules

- `BR-C2C-007`
- `BR-COM-007`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Classify condition.
2. BLOCKED if external input/capability required; ERROR if protocol/runtime fault.
3. Persist safe checkpoint and reason code.
4. Render bounded control message.

## Alternate / failure flows

- Recoverable BLOCKED can later accept PLAN after required input.
- ERROR may require ensure/repair then resume from checkpoint.

## Detailed design

### Application contracts

- `IC2CSessionService.BlockAsync/FailAsync`

### Persistence

Checkpoint with safe reason code/message.

### Security

No raw exceptions/logs.

### Concurrency / idempotency

Atomic checkpoint.

### Limits / pagination / timeouts

Bounded reason fields.

### Observability

Error category/correlation id.

### Error codes

- `PROTOCOL_INVALID_STATE`
- `C2C_NOT_READY`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- classification/transition

### Integration tests
- runtime failure fixture

### Adversarial / security tests
- exception contains secret -> message/log redacted

## Acceptance criteria

- [ ] BLOCKED and ERROR remain distinguishable.
- [ ] No sensitive diagnostics in control message.

## Implementation checklist

- [ ] State transitions/error mapper/tests

## Architecture references

- `01-architecture/12-ERROR-RECOVERY.md`
- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
