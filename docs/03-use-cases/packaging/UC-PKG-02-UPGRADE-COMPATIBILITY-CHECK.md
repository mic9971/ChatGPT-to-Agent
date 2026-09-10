# UC-PKG-02 — Upgrade / Compatibility Check

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P2 |
| Phase | V1.x |
| Module | Packaging & Upgrade |
| Primary actor | Local user / Release runtime |
| Depends on | UC-PKG-01 |

## Goal

Upgrade C2C.NET while preserving/migrating supported local config/session/execution schemas safely.

## Trigger

User replaces binary with newer version and starts/status/ensure.

## Preconditions

- Existing supported-version local state.

## Postconditions

- State is read or explicitly migrated; unsupported versions fail safely without destructive rewrite.

## Scope

### In scope
- schema compatibility
- backup/migration
- version report

### Out of scope
- silent destructive migration
- downgrade corruption

## Referenced business rules

- `BR-COM-010`
- `BR-CON-002`
- `BR-COM-011`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Read state envelope versions.
2. If current supported -> use directly.
3. If migratable -> create backup/temporary migrated representation, validate, atomically replace.
4. If unsupported -> stop with remediation guidance.

## Alternate / failure flows

- Downgrade sees newer unsupported version -> fail without rewrite.
- Migration interrupted -> old valid state remains.

## Detailed design

### Application contracts

- `IStateMigration` chain if/when needed

### Persistence

Versioned local state with backups during migration.

### Security

Migration never prints secrets/raw token material.

### Concurrency / idempotency

Migration serialized with runtime stopped/owner lock.

### Limits / pagination / timeouts

Bounded file sizes before migration.

### Observability

Migration version/result.

### Error codes

- `PROTOCOL_VERSION_UNSUPPORTED`
- `C2C_CONFLICT`

## Test design

### Unit tests
- migration idempotency
- unsupported version

### Integration tests
- upgrade fixture

### Adversarial / security tests
- crash during migration

## Acceptance criteria

- [ ] Interrupted migration does not destroy last valid state.

## Implementation checklist

- [ ] Implement only when first schema migration exists
- [ ] Tests

## Architecture references

- `01-architecture/11-DATA-MODEL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
