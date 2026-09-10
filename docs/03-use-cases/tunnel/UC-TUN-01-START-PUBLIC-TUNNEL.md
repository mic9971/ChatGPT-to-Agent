# UC-TUN-01 — Start Public Tunnel

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.4 |
| Module | Tunnel & Runtime Connectivity |
| Primary actor | CLI/runtime |
| Depends on | UC-MCP-01 |

## Goal

Start a configured tunnel provider from public HTTPS endpoint to the loopback bridge and record owned process/session metadata.

## Trigger

`c2c start` or `c2c ensure` when remote connectivity is required.

## Preconditions

- Bridge healthy on loopback.
- Tunnel provider binary/config available.

## Postconditions

- One live tunnel session exists and public endpoint is known safely.

## Scope

### In scope
- ITunnelProvider abstraction
- Cloudflare Quick Tunnel first
- owned process metadata
- readiness detection

### Out of scope
- exposing bridge on 0.0.0.0
- hard-coding Cloudflare throughout Core

## Referenced business rules

- `BR-SEC-005`
- `BR-CON-004`
- `BR-COM-008`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Acquire tunnel lifecycle lock.
2. Check for existing owned healthy tunnel; reuse if compatible.
3. Start provider using typed process arguments.
4. Read provider output through bounded parser to discover endpoint.
5. Validate HTTPS endpoint shape.
6. Persist owned session metadata atomically.
7. Return public endpoint without secrets.

## Alternate / failure flows

- Provider missing -> setup/doctor error.
- Process exits before readiness -> failed start and cleanup.
- Existing foreign process -> never kill; report conflict.

## Detailed design

### Application contracts

- `ITunnelProvider.StartAsync`
- `ITunnelSessionStore`
- `IOwnedProcessRunner`

### Persistence

Owned tunnel session metadata only; no provider secret tokens in logs/config.

### Security

Bridge remains loopback. Provider credentials protected by platform/local config rules.

### Concurrency / idempotency

Serialized start/reuse via BR-CON-004.

### Limits / pagination / timeouts

Startup timeout, output buffer cap.

### Observability

Provider name, transition, duration, result code; redact command secrets.

### Error codes

- `TUNNEL_START_FAILED`
- `C2C_CONFLICT`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- lifecycle lock
- endpoint parser

### Integration tests
- fake provider start/reuse

### Adversarial / security tests
- malformed endpoint output
- secret in provider output
- foreign pid

## Acceptance criteria

- [ ] Concurrent ensure calls produce one owned tunnel.
- [ ] No wildcard bridge bind needed.

## Implementation checklist

- [ ] Implement abstraction/fake first
- [ ] Cloudflare provider
- [ ] Session store
- [ ] Tests

## Architecture references

- `01-architecture/07-TUNNEL-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
