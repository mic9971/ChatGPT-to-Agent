# UC-EXE-03 — Read Normalized Test Status

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.3 |
| Module | Execution Evidence |
| Primary actor | MCP planner |
| Depends on | UC-EXE-01 |

## Goal

Return normalized test status associated with an execution record.

## Trigger

`test_status(executionId)`.

## Preconditions

- Execution record exists.
- Caller has `execution.read`.

## Postconditions

- Normalized test suites/counts/status returned.

## Scope

### In scope
- structured test summary
- unknown/not-run distinction
- bounded failing test names when safe

### Out of scope
- parsing arbitrary console text as source of truth when structured metadata exists

## Referenced business rules

- `BR-EXE-003`
- `BR-EXE-007`
- `BR-COM-003`
- `BR-COM-006`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Load execution record.
2. Read structured test summary.
3. Normalize status enum: NotRun/Passed/Failed/Partial/Unknown.
4. Bound failure details.
5. Return safe DTO.

## Alternate / failure flows

- No test metadata -> NotRun/Unknown per explicit field, not inferred passed.
- Partial suite -> Partial.

## Detailed design

### Application contracts

- `IExecutionQuery.GetTestStatusAsync`

### Persistence

Stored as part of execution record or referenced normalized test record.

### Security

Test names/messages are untrusted and bounded/redacted before exposure.

### Concurrency / idempotency

Immutable read.

### Limits / pagination / timeouts

Failure names/messages count/bytes capped.

### Observability

Test outcome counts as metrics without full names.

### Error codes

- `EXECUTION_NOT_FOUND`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- status normalization
- not-run semantics

### Integration tests
- MCP test status

### Adversarial / security tests
- test name contains control chars/secret-like token

## Acceptance criteria

- [ ] Exit code 0 with no tests does not become Passed automatically.
- [ ] Structured Failed is preserved.

## Implementation checklist

- [ ] Implement normalized model
- [ ] Query mapping
- [ ] Tests

## Architecture references

- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
