# UC-C2C-01 — Initialize C2C Task

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Execution Agent |
| Depends on | UC-WS-01 |

## Goal

Open a new logical task with stable task id, goal, constraints and iteration 0 INIT message.

## Trigger

Agent/user asks to start C2C planning for a change.

## Preconditions

- Workspace/runtime ready enough for planner to inspect data plane.

## Postconditions

- Local session checkpoint created and valid INIT envelope produced.

## Scope

### In scope
- task id generation
- goal/constraints
- INIT envelope
- checkpoint

### Out of scope
- source/diff/log body
- automatic code execution

## Referenced business rules

- `BR-C2C-001`
- `BR-C2C-002`
- `BR-C2C-010`
- `BR-COM-004`
- `BR-CON-002`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate bounded goal/constraints.
2. Generate stable task id.
3. Create checkpoint state INIT_SENT iteration 0.
4. Render C2C INIT envelope.
5. Return message to agent/UI transport.

## Alternate / failure flows

- Existing active task id -> resume/query rather than duplicate.
- Oversized control text -> reject/bound before persistence.

## Detailed design

### Application contracts

- `IC2CSessionService.CreateTaskAsync`
- `IC2CMessageRenderer`

### Persistence

Versioned session checkpoint atomic JSON.

### Security

Workspace text cannot modify envelope/system rules.

### Concurrency / idempotency

Task create idempotency if caller provides request key; checkpoint atomic.

### Limits / pagination / timeouts

Control message hard cap.

### Observability

Task id/state transition only.

### Error codes

- `PROTOCOL_INVALID_STATE`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- message validation
- task id stability

### Integration tests
- CLI/session create if exposed

### Adversarial / security tests
- prompt injection text in goal remains data, cannot alter renderer rules

## Acceptance criteria

- [ ] INIT contains no source/diff/log body.
- [ ] Checkpoint survives restart.

## Implementation checklist

- [ ] Protocol models/parser/renderer
- [ ] Session store
- [ ] Tests

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
