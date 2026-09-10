# UC-EXE-04 — Read Sanitized Execution Output

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

List and read bounded execution artifact content only after sanitizer/classification.

## Trigger

`execution_output(executionId,artifactId,cursor)`.

## Preconditions

- Execution record and artifact metadata exist.
- Caller has `execution.read`.

## Postconditions

- Safe/truncated body returned or restricted status returned without body.

## Scope

### In scope
- artifact list/status
- safe text chunks
- redaction
- restricted metadata

### Out of scope
- raw file path read
- private-key body
- unbounded logs

## Referenced business rules

- `BR-EXE-004`
- `BR-EXE-005`
- `BR-SEC-009`
- `BR-COM-006`
- `BR-COM-009`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Resolve artifact by execution-scoped id, never caller filesystem path.
2. Load sanitizer result/classification.
3. If restricted: return metadata only.
4. If safe/truncated: read sanitized representation, paginate.
5. Return integrity/version metadata if available.

## Alternate / failure flows

- Artifact missing -> typed not found.
- Sanitization pending/corrupt -> restricted/fail closed.

## Detailed design

### Application contracts

- `IExecutionArtifactReader.ListAsync/GetChunkAsync`
- `IArtifactSanitizer`

### Persistence

Store raw local artifact only if design permits and access is local-only; remote-readable sanitized representation/classification stored separately.

### Security

Remote caller cannot supply arbitrary local path. Private key blocks/secrets cause restriction/redaction per policy.

### Concurrency / idempotency

Sanitization finalized before remote availability; immutable classification per record version.

### Limits / pagination / timeouts

Hard artifact bytes/lines; chunk pagination.

### Observability

Artifact status/read count only; no content logging.

### Error codes

- `EXECUTION_NOT_FOUND`
- `SENSITIVE_CONTENT_DENIED`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- sanitizer classification
- chunking

### Integration tests
- MCP artifact read

### Adversarial / security tests
- private key block
- bearer token pattern
- home path redaction
- huge log

## Acceptance criteria

- [ ] Restricted artifact body never returned.
- [ ] Safe output is bounded and sanitized.

## Implementation checklist

- [ ] Define artifact model
- [ ] Sanitizer pipeline
- [ ] Store sanitized output
- [ ] MCP adapter/tests

## Architecture references

- `01-architecture/08-EXECUTION-REVIEW.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
