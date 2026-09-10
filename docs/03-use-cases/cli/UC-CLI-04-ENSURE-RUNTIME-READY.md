# UC-CLI-04 — Ensure Runtime Ready

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Execution Agent |
| Depends on | UC-TUN-03, UC-CLI-02 |

## Goal

Provide one idempotent agent-facing command that converges local runtime to ready state or returns precise action required.

## Trigger

`c2c ensure --json`.

## Preconditions

- Workspace configured or command can return NEED_SETUP.

## Postconditions

- READY / NEED_SETUP / NEED_PAIRING / DEGRADED / ERROR result.

## Scope

### In scope
- orchestration of bridge/tunnel/health
- machine-readable status

### Out of scope
- silent credential creation
- infinite retries

## Referenced business rules

- `BR-CON-003`
- `BR-CON-004`
- `BR-COM-007`
- `BR-COM-011`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Call runtime ensure state machine.
2. Check auth/pairing readiness.
3. Map to stable CLI JSON status/action.
4. Exit code follows documented mapping.

## Alternate / failure flows

- Need pairing -> no failure spam; return action required.
- Partial degraded local-only -> explicit status.

## Detailed design

### Application contracts

- CLI adapter over `IRuntimeEnsurer`

### Persistence

No direct persistence beyond services called.

### Security

Never bypass auth to force READY.

### Concurrency / idempotency

Designed for concurrent/repeated agent calls.

### Limits / pagination / timeouts

Overall command timeout.

### Observability

Ensure stage metrics.

### Error codes

- `C2C_NOT_READY`
- `TUNNEL_START_FAILED`

## Test design

### Unit tests
- status mapping

### Integration tests
- concurrent ensure fake runtime

### Adversarial / security tests
- pairing absent
- foreign port

## Acceptance criteria

- [ ] Agent can safely call before every C2C task.

## Implementation checklist

- [ ] CLI adapter/json tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
