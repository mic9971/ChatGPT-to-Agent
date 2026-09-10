# UC-EXE-02 — Read Execution Summary

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

Return bounded safe metadata for a referenced execution id.

## Trigger

`execution_summary(executionId)`.

## Preconditions

- Execution record exists.
- Caller has `execution.read`.

## Postconditions

- Safe execution summary returned.

## Scope

### In scope
- task/iteration
- timestamps
- exit classification
- visible changed files
- test counts
- artifact metadata status

### Out of scope
- raw logs
- secret paths
- unfiltered command environment

## Referenced business rules

- `BR-COM-003`
- `BR-EXE-002`
- `BR-EXE-007`
- `BR-SEC-007`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Authorize scope.
2. Load versioned record.
3. Validate workspace binding.
4. Apply changed-path visibility filter.
5. Map safe artifact metadata.
6. Return bounded summary.

## Alternate / failure flows

- Record version unsupported -> typed error.
- Artifact missing -> summary can mark missing/incomplete without leaking path.

## Detailed design

### Application contracts

- `IExecutionQuery.GetSummaryAsync`

### Persistence

Read-only execution store.

### Security

Workspace binding and visibility enforced.

### Concurrency / idempotency

Immutable record reads concurrent.

### Limits / pagination / timeouts

Changed-file/artifact lists capped.

### Observability

Read count/latency/result.

### Error codes

- `EXECUTION_NOT_FOUND`
- `AUTH_INSUFFICIENT_SCOPE`
- `PROTOCOL_VERSION_UNSUPPORTED`

## Test design

### Unit tests
- safe mapping
- visibility filter

### Integration tests
- MCP summary call

### Adversarial / security tests
- execution from another workspace
- restricted artifact metadata

## Acceptance criteria

- [ ] No raw artifact body returned.
- [ ] Cross-workspace execution id denied/not found safely.

## Implementation checklist

- [ ] Implement query
- [ ] MCP adapter
- [ ] Tests

## Architecture references

- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
