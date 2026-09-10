# UC-CLI-03 — Status and Doctor

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Local user / Agent |
| Depends on | None |

## Goal

Report current runtime state and diagnose configuration/dependency/connectivity problems without mutation.

## Trigger

`c2c status` / `c2c doctor`.

## Preconditions

- Binary runs.

## Postconditions

- Safe machine-readable diagnostics returned.

## Scope

### In scope
- workspace config presence
- bridge health
- tunnel state
- pairing readiness
- dependency versions/presence

### Out of scope
- secret dump
- repair mutation by doctor

## Referenced business rules

- `BR-COM-007`
- `BR-COM-009`
- `BR-OBS-004`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Status performs lightweight state/health reads.
2. Doctor performs bounded deeper checks with per-check result code and suggested remediation.
3. Return aggregate status and non-zero exit only according to documented severity policy.

## Alternate / failure flows

- Missing config -> NOT_CONFIGURED diagnostic.
- Optional dependency absent -> warning/degraded, not crash.

## Detailed design

### Application contracts

- `IRuntimeDiagnostics.GetStatusAsync/RunDoctorAsync`

### Persistence

Read-only.

### Security

No secret/token values displayed; absolute paths local output only when explicitly safe and useful, never remote.

### Concurrency / idempotency

Parallel independent checks allowed with bounded timeout.

### Limits / pagination / timeouts

Each doctor check timeout.

### Observability

Doctor check durations.

### Error codes

- `C2C_NOT_READY`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- severity aggregation

### Integration tests
- CLI output snapshot/schema

### Adversarial / security tests
- malformed config
- unreachable tunnel
- expired pairing

## Acceptance criteria

- [ ] Doctor never mutates state.
- [ ] JSON result has stable check ids.

## Implementation checklist

- [ ] Diagnostics services
- [ ] CLI formatting/tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
