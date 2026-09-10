# UC-PKG-01 — Publish Self-Contained CLI

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.9 |
| Module | Packaging & Upgrade |
| Primary actor | Release engineer / CI |
| Depends on | None |

## Goal

Produce platform-specific self-contained C2C.NET executable artifacts that do not require preinstalled .NET runtime.

## Trigger

Release build/tag pipeline.

## Preconditions

- All release-gate tests pass.

## Postconditions

- Versioned artifacts and checksums produced for supported RIDs.

## Scope

### In scope
- Release configuration
- single-file/self-contained as validated
- checksums
- version info

### Out of scope
- bundling secrets/config
- unsigned claims not actually produced

## Referenced business rules

- `BR-COM-010`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Restore locked dependencies.
2. Build/test release configuration.
3. Publish supported RIDs.
4. Run smoke `c2c --version` and local setup/status on artifact.
5. Generate checksums/SBOM if adopted.
6. Archive with versioned names.

## Alternate / failure flows

- Single-file incompatibility for dependency -> document exception rather than unsafe hack.

## Detailed design

### Application contracts

- CI/release scripts

### Persistence

Build artifacts only.

### Security

No secrets in package; dependency provenance/checksums.

### Concurrency / idempotency

CI concurrency/version tag guarantees one artifact set.

### Limits / pagination / timeouts

Artifact size budget tracked, not hard security boundary.

### Observability

Build duration/artifact size.

### Error codes

- `C2C_NOT_READY`

## Test design

### Unit tests
- version stamping helpers

### Integration tests
- artifact smoke tests

### Adversarial / security tests
- scan package for secret fixtures

## Acceptance criteria

- [ ] Runs without installed .NET runtime on target smoke environment.

## Implementation checklist

- [ ] Add publish profiles/scripts
- [ ] CI matrix
- [ ] Smoke tests

## Architecture references

- `01-architecture/15-NON-FUNCTIONAL-REQUIREMENTS.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
