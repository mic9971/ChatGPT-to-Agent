# UC-AUTH-01 — Create Pairing Session

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.5 |
| Module | Authorization & Pairing |
| Primary actor | Local user / CLI |
| Depends on | UC-WS-01 |

## Goal

Create a short-lived one-time approval session for pairing a remote MCP client with this workspace bridge.

## Trigger

`c2c pair`.

## Preconditions

- Bridge configured.
- Remote auth profile enabled.

## Postconditions

- Pairing session/code exists with TTL/attempt budget; no access token is disclosed.

## Scope

### In scope
- CSPRNG code
- TTL
- one-time session
- safe pairing URL/instructions

### Out of scope
- long-lived credential in terminal/chat
- reusable static password

## Referenced business rules

- `BR-AUTH-007`
- `BR-AUTH-008`
- `BR-SEC-011`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Invalidate or keep at most configured active pairing sessions per policy.
2. Generate CSPRNG verifier/code and hashed lookup representation.
3. Persist session expiry/attempt count atomically.
4. Return human-safe code/URL.

## Alternate / failure flows

- Existing active session -> rotate or return existing according to explicit policy; default rotate only on user request.
- Clock expired -> session unusable.

## Detailed design

### Application contracts

- `IPairingService.CreateAsync`
- `IPairingStore`

### Persistence

Persist hash/verifier state, not plaintext long-lived token; pairing code may exist only as necessary for display/verification lifecycle.

### Security

Attempt limit, TTL, no code logging.

### Concurrency / idempotency

Creation serialized enough to avoid ambiguous active sessions.

### Limits / pagination / timeouts

Code length/TTL config validated.

### Observability

Create/expire/success/fail counters, never code.

### Error codes

- `AUTH_PAIRING_EXPIRED`
- `C2C_CONFLICT`

## Test design

### Unit tests
- entropy/format
- expiry
- attempt count

### Integration tests
- CLI pair output and store

### Adversarial / security tests
- brute-force attempts
- code log capture test

## Acceptance criteria

- [ ] Code expires and is one-time.
- [ ] No access/refresh token printed.

## Implementation checklist

- [ ] Pairing model/store
- [ ] CSPRNG generator
- [ ] CLI adapter
- [ ] Tests

## Architecture references

- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
