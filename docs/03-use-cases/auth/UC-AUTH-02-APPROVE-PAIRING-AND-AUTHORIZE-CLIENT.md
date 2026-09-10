# UC-AUTH-02 — Approve Pairing and Authorize Client

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.5 |
| Module | Authorization & Pairing |
| Primary actor | Remote MCP client + local approval |
| Depends on | UC-AUTH-01 |

## Goal

Validate pairing approval and complete an OAuth-compatible authorization-code flow bound to this workspace/client.

## Trigger

Client initiates authorization and user supplies/approves valid pairing session.

## Preconditions

- Unexpired pairing session.
- Client metadata/registration accepted by configured profile.

## Postconditions

- Authorization code issued with workspace/client/scopes binding; pairing session consumed.

## Scope

### In scope
- authorization request validation
- PKCE challenge
- issuer/client validation
- scope consent bounded to V1 scopes
- consume pairing

### Out of scope
- password grant
- implicit flow
- granting undeclared write/shell scope

## Referenced business rules

- `BR-AUTH-001`
- `BR-AUTH-002`
- `BR-AUTH-003`
- `BR-AUTH-007`
- `BR-SEC-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate client identity/redirect/issuer rules.
2. Validate requested scopes against allowlist.
3. Require valid pairing approval.
4. Bind authorization grant to workspace/client/issuer/PKCE challenge.
5. Consume pairing session atomically.
6. Issue short-lived authorization code.

## Alternate / failure flows

- Invalid/expired pairing -> deny and count attempt.
- Unsupported scope -> deny.
- Pairing consumed concurrently -> only one success.

## Detailed design

### Application contracts

- authorization endpoint/service using vetted OAuth library primitives
- pairing approval adapter

### Persistence

Authorization grants/codes stored/protected according to OAuth library design.

### Security

PKCE S256 required for public/browser client; one-time pairing consumption.

### Concurrency / idempotency

Atomic pairing consumption prevents double authorization.

### Limits / pagination / timeouts

Request/body limits and authorization-code TTL.

### Observability

Auth failure reason category, client/workspace ids, no secret/code.

### Error codes

- `AUTH_PAIRING_EXPIRED`
- `AUTH_PAIRING_REJECTED`
- `C2C_INVALID_ARGUMENT`

## Test design

### Unit tests
- scope allowlist
- pairing consume race

### Integration tests
- authorization endpoint flow

### Adversarial / security tests
- redirect mismatch
- issuer mismatch
- PKCE downgrade
- double-use pairing

## Acceptance criteria

- [ ] Only allowed read scopes granted.
- [ ] Pairing session cannot authorize twice.

## Implementation checklist

- [ ] Integrate OAuth server primitives
- [ ] Bind pairing approval
- [ ] Tests

## Architecture references

- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
