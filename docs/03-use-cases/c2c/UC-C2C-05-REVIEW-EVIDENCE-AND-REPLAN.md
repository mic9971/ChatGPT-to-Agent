# UC-C2C-05 — Review Evidence and Replan

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.7 |
| Module | C2C Control Protocol |
| Primary actor | Planner/Reviewer |
| Depends on | UC-C2C-04, UC-GIT-02, UC-EXE-02, UC-EXE-03 |

## Goal

Use MCP evidence to decide whether another finite PLAN is required.

## Trigger

Planner receives EXECUTED.

## Preconditions

- MCP evidence readable.

## Postconditions

- Either a new PLAN is issued or review proceeds to DONE/BLOCKED.

## Scope

### In scope
- git diff
- test status
- execution summary/output as needed
- bounded review decision

### Out of scope
- trust executor claim without evidence

## Referenced business rules

- `BR-COM-003`
- `BR-C2C-005`
- `BR-C2C-006`
- `BR-EXE-007`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Planner reads execution summary/test status.
2. Planner reads Git diff for changed visible files.
3. Compare evidence to PLAN success criteria.
4. If gaps exist, issue bounded next PLAN with incremented iteration.
5. Agent validates PLAN through UC-C2C-02.

## Alternate / failure flows

- Evidence unavailable due infrastructure -> ERROR/BLOCKED, not false DONE.
- Tests not run but required -> REPLAN.

## Detailed design

### Application contracts

- No new bridge application contract beyond existing MCP tools; protocol validation on returned PLAN.

### Persistence

Planner decision stored as session plan/checkpoint when accepted by agent.

### Security

Repository text/diff is untrusted evidence; it cannot override system/agent rules.

### Concurrency / idempotency

Iteration monotonic.

### Limits / pagination / timeouts

Review message bounded.

### Observability

Planner-side evidence read telemetry already handled by tools.

### Error codes

- `PROTOCOL_INVALID_STATE`
- `EXECUTION_NOT_FOUND`

## Test design

### Unit tests
- replan state transition

### Integration tests
- E2E fixture with failing then passing test

### Adversarial / security tests
- executor says done but diff missing expected change

## Acceptance criteria

- [ ] Required failing/missing tests cause REPLAN, not DONE.

## Implementation checklist

- [ ] Implement protocol transition only; planner behavior documented in integration skill

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`
- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
