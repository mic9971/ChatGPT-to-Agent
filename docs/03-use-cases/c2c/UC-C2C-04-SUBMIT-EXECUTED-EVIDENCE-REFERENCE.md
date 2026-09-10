# UC-C2C-04 — Submit EXECUTED Evidence Reference

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Execution Agent |
| Depends on | UC-EXE-01, UC-C2C-03 |

## Goal

Link a finalized execution record to the active task iteration and produce bounded EXECUTED control message.

## Trigger

Agent finishes local implementation/tests and runs `c2c record`.

## Preconditions

- Execution record finalized.
- Session iteration matches record.

## Postconditions

- Session checkpoint becomes EXECUTED_SENT and message references execution id.

## Scope

### In scope
- execution id reference
- result summary sentence
- state transition

### Out of scope
- paste diff/log/source into message

## Referenced business rules

- `BR-C2C-004`
- `BR-COM-003`
- `BR-COM-004`
- `BR-CON-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate execution record belongs to workspace/task/iteration.
2. Update checkpoint EXECUTED_LOCAL then EXECUTED_SENT as transport step is acknowledged/recorded.
3. Render bounded EXECUTED envelope with evidence id.

## Alternate / failure flows

- Record missing/mismatched -> reject.
- Duplicate same execution -> idempotent message rendering.

## Detailed design

### Application contracts

- `IC2CSessionService.AttachExecutionAsync`
- message renderer

### Persistence

Session references immutable execution id.

### Security

No artifact bodies in control plane.

### Concurrency / idempotency

Checkpoint monotonic; same evidence can be re-rendered after crash without creating duplicate execution.

### Limits / pagination / timeouts

Control cap.

### Observability

State/execution id only.

### Error codes

- `EXECUTION_NOT_FOUND`
- `PROTOCOL_INVALID_STATE`

## Test design

### Unit tests
- execution binding

### Integration tests
- session+execution store integration

### Adversarial / security tests
- cross-workspace execution id

## Acceptance criteria

- [ ] EXECUTED contains evidence reference only.
- [ ] Mismatched evidence rejected.

## Implementation checklist

- [ ] Bind services
- [ ] Renderer
- [ ] Tests

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`
- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
