# UC-AGT-03 — Agent Executes Accepted PLAN

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-02 |

## Goal

Execute only the accepted bounded PLAN, edit/build/test locally, and preserve project coding/security rules.

## Trigger

Valid PLAN received.

## Preconditions

- Agent has workspace edit/terminal permissions from its own environment, independent of MCP.

## Postconditions

- Scoped implementation and tests completed or blocked; no C2C security boundary weakened.

## Scope

### In scope
- inspect minimal source
- edit
- build/test
- diff review

### Out of scope
- unrelated refactor
- disable tests/security
- MCP write tool addition

## Referenced business rules

- `BR-C2C-003`
- `BR-COM-002`
- `BR-COM-005`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Read target UC/BR/rules.
2. Inspect minimal source/tests.
3. Implement smallest coherent change.
4. Run focused tests then broader applicable suite.
5. Review local diff for scope/security.
6. Record evidence.

## Alternate / failure flows

- Plan conflicts with approved rules -> stop BLOCKED and report conflict.
- Tests fail unrelated -> document exact evidence, do not hide.

## Detailed design

### Application contracts

- Agent runtime behavior governed by `.agent/rules` and integration pack

### Persistence

Source changes managed by agent/Git, not bridge.

### Security

Never weaken C2C workspace/auth rules for convenience.

### Concurrency / idempotency

Agent serializes its own conflicting edits; C2C only tracks iterations.

### Limits / pagination / timeouts

Keep work inside PLAN scope.

### Observability

Report exact commands/test results to execution record, not fabricated claims.

### Error codes

- `PROTOCOL_INVALID_STATE`

## Test design

### Unit tests
- N/A code tests depend on target UC

### Integration tests
- E2E agent plan execution

### Adversarial / security tests
- PLAN asks forbidden behavior

## Acceptance criteria

- [ ] Implementation matches target UC acceptance criteria.
- [ ] No unrelated code change.

## Implementation checklist

- [ ] Use target UC checklist
- [ ] Run tests
- [ ] Record evidence

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
