# UC-AUTH-05 — Revoke / Unpair Client

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.5 |
| Module | Authorization & Pairing |
| Primary actor | Local user / CLI |
| Depends on | UC-AUTH-03 |

## Goal

Remove a client's remote authorization for the current workspace and invalidate future token use.

## Trigger

`c2c unpair [client]` or explicit revocation.

## Preconditions

- Workspace configured.

## Postconditions

- Pairing/client grants and refresh tokens revoked; future access denied.

## Scope

### In scope
- list/select safe paired client metadata
- revoke grants/tokens
- clear local client pairing association

### Out of scope
- show raw tokens
- revoke unrelated workspace client

## Referenced business rules

- `BR-AUTH-006`
- `BR-SEC-006`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Resolve target client by safe id.
2. Mark authorization grants/refresh family revoked.
3. Invalidate active access according to selected token model/cache policy.
4. Clear pairing association.
5. Return idempotent result.

## Alternate / failure flows

- Already unpaired -> idempotent success.
- Unknown client -> safe not-found/empty semantics.

## Detailed design

### Application contracts

- `IAuthorizationRevoker.RevokeAsync`

### Persistence

Revocation metadata/audit; raw token absent.

### Security

Workspace binding checked before revocation.

### Concurrency / idempotency

Serialized per client token family.

### Limits / pagination / timeouts

No large output.

### Observability

Revocation event safe ids only.

### Error codes

- `AUTH_INVALID_TOKEN`

## Test design

### Unit tests
- idempotent revoke

### Integration tests
- unpair then protected MCP call fails

### Adversarial / security tests
- attempt to revoke client from other workspace

## Acceptance criteria

- [ ] Revoked client cannot refresh/use future protected requests per policy.

## Implementation checklist

- [ ] CLI adapter
- [ ] Token-store revoke
- [ ] Tests

## Architecture references

- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
