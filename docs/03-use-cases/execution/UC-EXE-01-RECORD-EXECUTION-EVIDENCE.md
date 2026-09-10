# UC-EXE-01 — Record Execution Evidence

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.3 |
| Module | Execution Evidence |
| Primary actor | Execution Agent / CLI |
| Depends on | UC-WS-01 |

## Goal

Persist immutable, versioned evidence metadata for one task iteration without re-executing the command.

## Trigger

`c2c record --task ... --iteration ... --exit-code ...`.

## Preconditions

- Workspace configured.
- Task/iteration identifiers valid.

## Postconditions

- Finalized execution record is atomically persisted and has an execution id.

## Scope

### In scope
- metadata
- changed-file references
- test summary
- artifact references
- idempotency key

### Out of scope
- executing command
- trusting arbitrary path outside runtime state
- remote write

## Referenced business rules

- `BR-EXE-001`
- `BR-EXE-006`
- `BR-CON-001`
- `BR-CON-002`
- `BR-COM-010`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate identifiers/iteration against session if one exists.
2. Validate record size/counts.
3. Normalize changed-file relative paths and filter unsafe disclosure metadata.
4. Create execution id/idempotency mapping.
5. Persist artifact metadata references then record atomically.
6. Return execution id and summary.

## Alternate / failure flows

- Same finalized idempotency key+same payload -> same execution id.
- Same key+different payload -> conflict.
- Older iteration tries to overwrite newer finalized session -> reject.

## Detailed design

### Application contracts

- `IExecutionRecorder.RecordAsync(ExecutionRecordRequest, CancellationToken)`
- `IExecutionStore`

### Persistence

User-scoped execution JSON, atomic writes; artifact bodies stored separately after sanitizer classification.

### Security

Input is local executor data but still untrusted; path/artifact metadata validated and bounded.

### Concurrency / idempotency

Idempotency and atomic persistence per BR-CON-001/002.

### Limits / pagination / timeouts

Hard counts for changed files/tests/artifacts; command text optional and bounded.

### Observability

Record operation id/task id/execution id/result, no raw artifact body.

### Error codes

- `C2C_CONFLICT`
- `C2C_INVALID_ARGUMENT`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- idempotent same payload
- conflicting key
- schema version

### Integration tests
- CLI record persists/reloads

### Adversarial / security tests
- malicious artifact filename/path
- huge metadata
- older iteration overwrite

## Acceptance criteria

- [ ] Repeated identical record is safe.
- [ ] Bridge never runs the command.
- [ ] Record survives restart.

## Implementation checklist

- [ ] Implement contracts/store
- [ ] Atomic persistence
- [ ] CLI adapter
- [ ] Tests

## Architecture references

- `01-architecture/08-EXECUTION-REVIEW.md`
- `01-architecture/11-DATA-MODEL.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
