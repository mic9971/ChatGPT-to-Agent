# UC-CLI-05 — Pair and Unpair CLI

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Local user |
| Depends on | UC-AUTH-01, UC-AUTH-05 |

## Goal

Expose local human commands for creating pairing session and revoking client access.

## Trigger

`c2c pair` / `c2c unpair`.

## Preconditions

- Auth slice available.

## Postconditions

- Pairing instructions or revocation result returned safely.

## Scope

### In scope
- pairing code display once
- safe paired client selector/id

### Out of scope
- access token display
- secret listing

## Referenced business rules

- `BR-AUTH-007`
- `BR-AUTH-006`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Pair delegates to pairing service and displays code/url safely.
2. Unpair resolves safe client id and delegates to revoker.
3. JSON mode never includes bearer/refresh tokens.

## Alternate / failure flows

- No paired clients -> safe empty result.

## Detailed design

### Application contracts

- CLI adapters

### Persistence

Auth stores handled by services.

### Security

Terminal pairing code is sensitive short-lived display; mark warning and never logs.

### Concurrency / idempotency

Pair/unpair service semantics.

### Limits / pagination / timeouts

Small output.

### Observability

Command event without code.

### Error codes

- `AUTH_PAIRING_EXPIRED`

## Test design

### Unit tests
- output redaction

### Integration tests
- CLI flow

### Adversarial / security tests
- captured logger excludes code/token

## Acceptance criteria

- [ ] No long-lived credential in output.

## Implementation checklist

- [ ] Adapters/tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`
- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
