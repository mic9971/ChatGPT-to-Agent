# UC-C2C-06 — Complete Task with DONE

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Planner/Reviewer + Execution Agent |
| Depends on | UC-C2C-05 |

## Goal

Close a task only when success criteria and review evidence are satisfied.

## Trigger

Planner returns `STATE: DONE`.

## Preconditions

- Task has executed/reviewed evidence for current iteration.

## Postconditions

- Session checkpoint is terminal DONE with bounded final summary.

## Scope

### In scope
- DONE validation
- final summary
- terminal checkpoint

### Out of scope
- automatic git commit/push
- DONE without evidence

## Referenced business rules

- `BR-C2C-005`
- `BR-COM-003`
- `BR-CON-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Parse DONE envelope.
2. Validate task/iteration and required reviewed evidence reference exists locally.
3. Persist terminal DONE checkpoint.
4. Return final status to agent/user.

## Alternate / failure flows

- DONE for stale iteration -> reject.
- Evidence absent -> protocol conflict requiring review.

## Detailed design

### Application contracts

- `IC2CSessionService.CompleteAsync`

### Persistence

Terminal session checkpoint retained per cleanup policy.

### Security

Final summary must not include secret/raw bodies.

### Concurrency / idempotency

Terminal state immutable except archival metadata.

### Limits / pagination / timeouts

Bounded summary.

### Observability

Completion event/task id/result.

### Error codes

- `PROTOCOL_INVALID_STATE`

## Test design

### Unit tests
- done validation

### Integration tests
- complete/reload session

### Adversarial / security tests
- DONE after missing tests

## Acceptance criteria

- [ ] DONE cannot be accepted for stale/missing evidence.

## Implementation checklist

- [ ] Terminal transition/tests

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
