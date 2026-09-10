# UC-CLI-07 — Record Evidence and Read Local Logs

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Execution Agent / Local user |
| Depends on | UC-EXE-01 |

## Goal

Record execution evidence and view C2C.NET diagnostic logs locally without exposing sensitive raw content.

## Trigger

`c2c record ...` / `c2c logs`.

## Preconditions

- Workspace configured.

## Postconditions

- Evidence persisted or bounded redacted diagnostic logs displayed.

## Scope

### In scope
- record adapter
- local structured log viewer/filter/tail bounded

### Out of scope
- remote raw logs
- source/artifact body logging

## Referenced business rules

- `BR-EXE-001`
- `BR-COM-009`
- `BR-OBS-001`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Record delegates to execution recorder.
2. Logs reads only C2C.NET structured diagnostic sink, not arbitrary filesystem path.
3. Apply line/time/count bounds and redaction.

## Alternate / failure flows

- Log file missing -> empty/safe message.
- Invalid record -> stable error.

## Detailed design

### Application contracts

- CLI adapters
- `IDiagnosticLogReader` local-only

### Persistence

Execution store + local diagnostic logs.

### Security

Logs already redacted at write; reader does not accept arbitrary path.

### Concurrency / idempotency

Concurrent read/tail bounded.

### Limits / pagination / timeouts

Max lines/bytes/time window.

### Observability

N/A for log viewer; no self-recursive logging body.

### Error codes

- `C2C_INVALID_ARGUMENT`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- record validation
- log bounds

### Integration tests
- CLI record/logs

### Adversarial / security tests
- log contains token-like input -> redacted before persisted

## Acceptance criteria

- [ ] `c2c logs` cannot read arbitrary file.
- [ ] Record returns execution id.

## Implementation checklist

- [ ] Adapters/tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`
- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
