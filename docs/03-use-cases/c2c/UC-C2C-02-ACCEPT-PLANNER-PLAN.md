# UC-C2C-02 — Accept Planner PLAN

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-01 |

## Goal

Validate and persist a bounded planner PLAN for the next execution iteration.

## Trigger

Planner returns `STATE: PLAN` for active task.

## Preconditions

- Task exists in a state expecting plan/replan.
- Task id matches.

## Postconditions

- Plan accepted, iteration advanced as defined, checkpoint becomes PLAN_RECEIVED.

## Scope

### In scope
- plan rationale/actions/files/tests/success criteria
- state transition validation

### Out of scope
- accept plan for wrong task
- unbounded epic plan

## Referenced business rules

- `BR-C2C-001`
- `BR-C2C-002`
- `BR-C2C-003`
- `BR-C2C-010`
- `BR-CON-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Parse bounded envelope.
2. Validate version/task/state/iteration relation.
3. Validate PLAN includes actions/tests/success criteria.
4. Persist plan summary/checkpoint atomically.
5. Return execution-ready plan to agent.

## Alternate / failure flows

- Duplicate identical PLAN -> idempotent accept.
- Older/wrong iteration -> reject.
- Plan too broad/oversized -> protocol validation failure requiring planner to reissue bounded plan.

## Detailed design

### Application contracts

- `IC2CProtocolValidator.ValidatePlan`
- `IC2CSessionService.AcceptPlanAsync`

### Persistence

Plan/checkpoint versioned local state; bounded text.

### Security

Treat plan text as instructions only within agent policy; it cannot grant forbidden MCP/write capability.

### Concurrency / idempotency

Monotonic checkpoint version/iteration.

### Limits / pagination / timeouts

Envelope/section caps.

### Observability

Transition accepted/rejected reason code.

### Error codes

- `PROTOCOL_INVALID_STATE`
- `PROTOCOL_VERSION_UNSUPPORTED`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- state transition
- duplicate plan
- iteration mismatch

### Integration tests
- session persistence

### Adversarial / security tests
- malicious plan asks to bypass security -> agent rules still forbid

## Acceptance criteria

- [ ] Wrong task/iteration cannot overwrite session.
- [ ] Accepted plan is finite and testable.

## Implementation checklist

- [ ] Parser/validator
- [ ] Session transition
- [ ] Tests

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
