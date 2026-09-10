# UC-AUTH-04 — Rotate Refresh Token

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.5 |
| Module | Authorization & Pairing |
| Primary actor | Remote MCP client |
| Depends on | UC-AUTH-03 |

## Goal

Refresh remote access using a valid refresh token while rotating single-use token state.

## Trigger

Token refresh grant.

## Preconditions

- Refresh token unexpired, unrevoked and bound to client/workspace/issuer.

## Postconditions

- New access token and new refresh token issued; old refresh token cannot be reused.

## Scope

### In scope
- refresh grant
- rotation
- replay detection

### Out of scope
- non-rotating perpetual refresh token

## Referenced business rules

- `BR-AUTH-005`
- `BR-SEC-006`
- `BR-SEC-008`
- `BR-CON-005`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate token hash/binding/expiry.
2. Atomically mark consumed and create successor.
3. Issue new access/refresh tokens.
4. Audit replay attempts without token value.

## Alternate / failure flows

- Concurrent same refresh token -> one succeeds, others replay-denied.
- Revoked/unpaired -> deny.

## Detailed design

### Application contracts

- OAuth refresh token store/service

### Persistence

Hashed/protected token state with predecessor/successor/revoked metadata as needed.

### Security

Constant-time/secure lookup behavior provided by implementation/library; never log token.

### Concurrency / idempotency

Atomic single-use rotation.

### Limits / pagination / timeouts

Bounded token chain/cleanup policy.

### Observability

Refresh success/replay/revoked counters.

### Error codes

- `AUTH_INVALID_TOKEN`

## Test design

### Unit tests
- single-use rotation
- concurrent replay

### Integration tests
- refresh endpoint

### Adversarial / security tests
- stolen old refresh replay
- wrong client/workspace

## Acceptance criteria

- [ ] Exactly one concurrent refresh succeeds.
- [ ] Old token unusable after rotation.

## Implementation checklist

- [ ] Implement rotation store semantics
- [ ] Tests

## Architecture references

- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
