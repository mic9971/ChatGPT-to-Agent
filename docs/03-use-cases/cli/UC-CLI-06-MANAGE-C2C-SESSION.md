# UC-CLI-06 — Manage C2C Session

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | CLI & Local Lifecycle |
| Primary actor | Execution Agent / User |
| Depends on | UC-C2C-01, UC-C2C-08 |

## Goal

Create, inspect and recover local C2C task checkpoints through stable CLI JSON.

## Trigger

`c2c session new/status/handoff` as final command shape is approved.

## Preconditions

- Protocol slice available.

## Postconditions

- Session operation returns current state/next expected action.

## Scope

### In scope
- task create
- session status
- handoff rendering

### Out of scope
- editing source
- automatic planner message sending unless integration layer does it

## Referenced business rules

- `BR-C2C-001`
- `BR-C2C-008`
- `BR-COM-010`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Parse subcommand.
2. Delegate to session service.
3. Return bounded JSON/text with task/state/iteration/next action.

## Alternate / failure flows

- Unknown task -> not found.
- Corrupt state -> safe recovery error.

## Detailed design

### Application contracts

- CLI adapters

### Persistence

Session store.

### Security

No raw plan/evidence bodies in default status output beyond bounded safe summary.

### Concurrency / idempotency

Read/update semantics delegated.

### Limits / pagination / timeouts

Bounded summaries.

### Observability

State query counts.

### Error codes

- `C2C_NOT_READY`
- `PROTOCOL_VERSION_UNSUPPORTED`

## Test design

### Unit tests
- next-action mapping

### Integration tests
- session CLI restart

### Adversarial / security tests
- corrupt session file

## Acceptance criteria

- [ ] Agent can determine next action after restart.

## Implementation checklist

- [ ] Command design tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`
- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
