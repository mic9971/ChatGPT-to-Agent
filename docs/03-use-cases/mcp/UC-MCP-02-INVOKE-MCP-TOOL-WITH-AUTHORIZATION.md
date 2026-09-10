# UC-MCP-02 — Invoke MCP Tool with Authorization

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.5 |
| Module | MCP Data Plane |
| Primary actor | MCP planner |
| Depends on | UC-MCP-01 |

## Goal

Route an MCP tool call through authentication/scope validation, application service and safe error mapping.

## Trigger

Client invokes one registered MCP tool.

## Preconditions

- MCP endpoint running.
- Tool exists.

## Postconditions

- Authorized call returns safe bounded result or stable error.

## Scope

### In scope
- scope validation
- request validation
- application dispatch
- safe error mapping

### Out of scope
- policy logic duplicated inside tool class
- stack trace exposure

## Referenced business rules

- `BR-COM-007`
- `BR-SEC-006`
- `BR-SEC-007`
- `BR-COM-008`
- `BR-OBS-001`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate protocol/request schema.
2. Authenticate caller if remote profile enabled.
3. Check required scope for tool.
4. Dispatch to application service with cancellation token.
5. Map expected failures to stable MCP error data.
6. Record safe metrics/correlation.

## Alternate / failure flows

- Unauthenticated -> authorization challenge/401 per selected MCP profile.
- Insufficient scope -> deny without invoking service.
- Cancellation -> safe cancelled result; no swallow.

## Detailed design

### Application contracts

- Thin MCP adapter per tool
- shared authorization requirement metadata/policy

### Persistence

None.

### Security

Tool-specific scope enforcement before data access.

### Concurrency / idempotency

Stateless request handling; application services thread-safe as needed.

### Limits / pagination / timeouts

Input/output limits validated both at transport and service level.

### Observability

Correlation id, tool name, duration, result code only.

### Error codes

- `AUTH_INVALID_TOKEN`
- `AUTH_INSUFFICIENT_SCOPE`
- `C2C_INVALID_ARGUMENT`
- `C2C_CANCELLED`

## Test design

### Unit tests
- adapter maps errors
- scope requirement mapping

### Integration tests
- authorized/unauthorized MCP call

### Adversarial / security tests
- expired token
- wrong workspace token
- malformed request

## Acceptance criteria

- [ ] Service not invoked on failed authorization.
- [ ] Errors leak no local details.

## Implementation checklist

- [ ] Create common adapter policy
- [ ] Add auth integration once auth slice exists
- [ ] Test every tool scope

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/06-OAUTH-PAIRING.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
