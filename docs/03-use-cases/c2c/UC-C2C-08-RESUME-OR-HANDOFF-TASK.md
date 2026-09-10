# UC-C2C-08 — Resume or HANDOFF Task

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Execution Agent / User |
| Depends on | UC-C2C-01 |

## Goal

Continue a task after local process restart, or create a bounded HANDOFF when the planner conversation cannot continue.

## Trigger

Agent restarts, chat unavailable, or explicit handoff request.

## Preconditions

- Persisted session checkpoint exists.

## Postconditions

- Agent either resumes from exact local checkpoint or emits HANDOFF brief without inventing RESUME protocol state.

## Scope

### In scope
- checkpoint load
- next expected action
- handoff summary

### Out of scope
- replaying command blindly
- full transcript/source dump

## Referenced business rules

- `BR-C2C-008`
- `BR-C2C-009`
- `BR-CON-006`
- `BR-COM-004`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Load latest valid checkpoint.
2. Validate workspace binding/schema.
3. Determine next expected action from state.
4. If planner conversation available: continue existing flow.
5. If unavailable: render HANDOFF with goal,constraints,last evidence,open issues,next action.

## Alternate / failure flows

- Corrupt checkpoint -> recovery error and preserve file for diagnostics; do not guess state.
- EXECUTING at crash -> mark uncertain and require evidence/worktree inspection before re-execution.

## Detailed design

### Application contracts

- `IC2CSessionRecovery.LoadAsync`
- `IC2CHandoffBuilder`

### Persistence

Versioned checkpoint with atomic writes/backups as designed.

### Security

Handoff excludes raw source/diff/log/token.

### Concurrency / idempotency

Newest monotonic checkpoint wins; older cannot overwrite.

### Limits / pagination / timeouts

Handoff hard cap.

### Observability

Resume/handoff event and state.

### Error codes

- `PROTOCOL_VERSION_UNSUPPORTED`
- `C2C_NOT_READY`

## Test design

### Unit tests
- next-action derivation
- handoff bounds

### Integration tests
- restart after PLAN/EXECUTING/EXECUTED

### Adversarial / security tests
- corrupt checkpoint
- stale older checkpoint

## Acceptance criteria

- [ ] No RESUME state added.
- [ ] Crash in EXECUTING does not auto-run command again.

## Implementation checklist

- [ ] Recovery service
- [ ] Handoff renderer
- [ ] Tests

## Architecture references

- `01-architecture/12-ERROR-RECOVERY.md`
- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
