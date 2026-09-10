# UC-AUTH-03 — Issue and Validate Access Token

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.5 |
| Module | Authorization & Pairing |
| Primary actor | Remote MCP client |
| Depends on | UC-AUTH-02 |

## Goal

Exchange a valid authorization grant for a short-lived token and validate it for every remote tool request.

## Trigger

Token endpoint exchange then MCP call.

## Preconditions

- Valid auth code/PKCE verifier.

## Postconditions

- Access token represents workspace/client/issuer/scopes and can authorize only permitted MCP tools.

## Scope

### In scope
- authorization code exchange
- PKCE verification
- access token
- optional refresh token
- token validation middleware

### Out of scope
- bearer token in logs
- workspace-agnostic token

## Referenced business rules

- `BR-AUTH-001`
- `BR-AUTH-002`
- `BR-AUTH-004`
- `BR-SEC-006`
- `BR-SEC-007`
- `BR-SEC-008`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate code/client/redirect/PKCE.
2. Issue bounded-lifetime access token with claims/binding.
3. Issue refresh token only when policy/scope permits.
4. Persist only secure/hash representation when state lookup required.
5. For MCP request validate signature/opaque lookup, expiry, issuer, workspace/client/scopes.

## Alternate / failure flows

- Expired/invalid token -> 401/challenge.
- Valid token wrong workspace -> deny.
- Insufficient scope -> 403/tool denial.

## Detailed design

### Application contracts

- OAuth token service/validation handler
- tool scope policies

### Persistence

Secure token/grant store as required; raw tokens not persisted in plaintext.

### Security

No token in logs or human-visible protocol.

### Concurrency / idempotency

Authorization code single-use; token validation thread-safe.

### Limits / pagination / timeouts

Token lifetimes validated at startup.

### Observability

Success/failure counters by safe category.

### Error codes

- `AUTH_INVALID_TOKEN`
- `AUTH_INSUFFICIENT_SCOPE`

## Test design

### Unit tests
- claim/binding validation
- expiry

### Integration tests
- token exchange + MCP protected call

### Adversarial / security tests
- wrong workspace
- wrong issuer
- expired token
- scope escalation

## Acceptance criteria

- [ ] Token from workspace A cannot read workspace B.
- [ ] Tool scope enforced independently.

## Implementation checklist

- [ ] Implement token profile
- [ ] Auth middleware/policies
- [ ] Tests

## Architecture references

- `01-architecture/06-OAUTH-PAIRING.md`
- `01-architecture/04-MCP-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
