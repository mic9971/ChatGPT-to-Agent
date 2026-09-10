# UC-AGT-04 — Agent Submit / Resume / Handoff

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-04, UC-C2C-06, UC-C2C-08 |

## Goal

After execution, record evidence, submit EXECUTED, then follow planner DONE/PLAN/BLOCKED; recover safely after interruptions.

## Trigger

Local execution iteration ends or agent restarts.

## Preconditions

- Task/session exists.

## Postconditions

- Evidence linked and next protocol state followed without losing task identity.

## Scope

### In scope
- record
- EXECUTED relay
- accept PLAN/DONE
- resume/handoff

### Out of scope
- paste raw logs/diff
- claim DONE without planner review

## Referenced business rules

- `BR-C2C-004`
- `BR-C2C-005`
- `BR-C2C-008`
- `BR-C2C-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Run `c2c record`.
2. Attach execution to session and relay EXECUTED.
3. Wait for planner response.
4. If PLAN, execute next iteration.
5. If DONE, close task.
6. If interruption, inspect session next action; use HANDOFF only if conversation unavailable.

## Alternate / failure flows

- Planner evidence read fails -> do not mark done.
- Agent restart at EXECUTING -> inspect worktree/evidence before repeating commands.

## Detailed design

### Application contracts

- Integration instructions

### Persistence

Session/execution stores.

### Security

No raw artifacts in control messages.

### Concurrency / idempotency

Idempotent record/relay on restart.

### Limits / pagination / timeouts

Bounded messages.

### Observability

Report state transitions.

### Error codes

- `EXECUTION_NOT_FOUND`
- `PROTOCOL_INVALID_STATE`

## Test design

### Unit tests
- integration behavior checklist

### Integration tests
- E2E two-iteration scenario

### Adversarial / security tests
- restart between record and relay

## Acceptance criteria

- [ ] Task identity/iteration preserved across restart.

## Implementation checklist

- [ ] Antigravity/Codex integration tests/manual scenario

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`
- `01-architecture/12-ERROR-RECOVERY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
