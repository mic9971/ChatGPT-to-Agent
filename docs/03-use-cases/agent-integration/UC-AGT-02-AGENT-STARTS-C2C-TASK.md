# UC-AGT-02 — Agent Starts C2C Task

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Execution Agent |
| Depends on | UC-CLI-04, UC-C2C-01 |

## Goal

Standardize how an execution agent ensures runtime readiness, opens task and sends INIT to planner.

## Trigger

User asks agent to use C2C for a coding task.

## Preconditions

- Integration pack loaded.

## Postconditions

- Runtime READY/NEED_ACTION handled; valid INIT produced for planner.

## Scope

### In scope
- `c2c ensure --json`
- session create
- INIT relay

### Out of scope
- agent bypasses pairing/security
- pastes repo into control message

## Referenced business rules

- `BR-COM-004`
- `BR-C2C-001`
- `BR-C2C-003`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Agent calls ensure.
2. If action required, stop and tell user exact requirement.
3. Create task/session.
4. Relay INIT exactly/bounded to ChatGPT planner via available UI/integration transport.
5. Wait for PLAN.

## Alternate / failure flows

- Planner unavailable -> HANDOFF/manual fallback instructions.

## Detailed design

### Application contracts

- Integration instruction, not Core SDK contract in V1

### Persistence

Session state handled by C2C.NET.

### Security

Agent may not fabricate READY or bypass auth.

### Concurrency / idempotency

Repeated ensure/task creation follows idempotency/request key guidance.

### Limits / pagination / timeouts

Control message cap.

### Observability

Agent reports commands actually run.

### Error codes

- `C2C_NOT_READY`

## Test design

### Unit tests
- instruction conformance review

### Integration tests
- E2E sample agent flow

### Adversarial / security tests
- ensure returns NEED_PAIRING

## Acceptance criteria

- [ ] Agent stops for NEED_PAIRING instead of hacking around it.

## Implementation checklist

- [ ] Write Antigravity runbook
- [ ] E2E checklist

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
