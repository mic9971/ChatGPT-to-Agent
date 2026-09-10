# UC-MCP-01 — Start Stateless MCP Endpoint

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.1 |
| Module | MCP Data Plane |
| Primary actor | Runtime / MCP client |
| Depends on | UC-WS-01 |

## Goal

Expose the approved read-only tool catalog over the official ASP.NET Core MCP transport on a loopback host.

## Trigger

`c2c start/ensure` starts bridge; MCP client connects to `/mcp`.

## Preconditions

- Workspace configured.
- Host options valid.

## Postconditions

- MCP endpoint is reachable locally and advertises only approved tools.

## Scope

### In scope
- official C# SDK HTTP transport
- stateless mode
- tool discovery
- protocol version validation

### Out of scope
- legacy SSE-first design
- MCP transport session as C2C task state
- write/exec tools

## Referenced business rules

- `BR-COM-002`
- `BR-COM-010`
- `BR-SEC-005`
- `BR-SEC-010`
- `BR-COM-011`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate host bind options.
2. Register approved MCP server/tool adapters through DI.
3. Configure stateless HTTP transport.
4. Map `/mcp` with host/origin/auth policies.
5. Start on loopback and report readiness to local lifecycle API.

## Alternate / failure flows

- Unsupported protocol -> typed protocol error.
- Attempted wildcard bind -> startup failure.
- Auth disabled local development profile -> only local access allowed.

## Detailed design

### Application contracts

- ASP.NET Core composition root
- MCP tool registration module
- `IMcpCapabilityCatalog` optional internal abstraction

### Persistence

No MCP session persistence.

### Security

Loopback, AllowedHosts/origin policy, remote auth when tunnel profile enabled.

### Concurrency / idempotency

Host startup single-owner per workspace; request handlers concurrent and stateless.

### Limits / pagination / timeouts

Request body/time limits set at HTTP transport level.

### Observability

OpenTelemetry/ASP.NET request metrics; do not capture request/response bodies.

### Error codes

- `MCP_PROTOCOL_UNSUPPORTED`
- `C2C_NOT_READY`

## Test design

### Unit tests
- tool catalog contains no forbidden tools

### Integration tests
- TestServer/real HTTP lists/calls tools

### Adversarial / security tests
- host header/origin invalid
- forbidden tool absent

## Acceptance criteria

- [ ] Only approved read-only tools discoverable.
- [ ] No MCP session store introduced.
- [ ] Host cannot bind public wildcard in V1.

## Implementation checklist

- [ ] Add `ModelContextProtocol.AspNetCore`
- [ ] Configure stateless transport
- [ ] Add tool registration tests
- [ ] Add HTTP security tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/02-HIGH-LEVEL-ARCHITECTURE.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
