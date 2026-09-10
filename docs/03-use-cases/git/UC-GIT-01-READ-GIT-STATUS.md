# UC-GIT-01 — Read Git Status

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.2 |
| Module | Git Evidence |
| Primary actor | MCP planner |
| Depends on | UC-WS-01 |

## Goal

Return bounded machine-readable working-tree status for visible paths only.

## Trigger

`git_status`.

## Preconditions

- Workspace bound.
- Git repository available.
- Caller has `git.read`.

## Postconditions

- Allowed changed paths/statuses returned; denied paths omitted.

## Scope

### In scope
- porcelain machine-readable status
- renames status
- bounded entries

### Out of scope
- Git mutation
- commit history exploration V1
- denied path disclosure

## Referenced business rules

- `BR-COM-002`
- `BR-COM-006`
- `BR-SEC-003`
- `BR-SEC-004`
- `BR-SEC-007`
- `BR-EXE-002`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Run Git using typed argument list in workspace root with timeout.
2. Parse porcelain output.
3. Canonicalize/visibility-check each candidate path.
4. Filter denied entries before DTO creation.
5. Sort and bound result.

## Alternate / failure flows

- Not a Git repo -> `GIT_NOT_AVAILABLE`.
- Malformed/unsupported status line -> safe parser error, no raw line leak.

## Detailed design

### Application contracts

- `IGitReader.GetStatusAsync`
- `IGitProcess`

### Persistence

None.

### Security

No shell command concatenation. Denied path names removed before response/logging.

### Concurrency / idempotency

Concurrent read-only Git operations allowed under process concurrency limit.

### Limits / pagination / timeouts

Max changed entries and process timeout.

### Observability

Git operation duration/result count/exit classification.

### Error codes

- `GIT_NOT_AVAILABLE`
- `C2C_TIMEOUT`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- porcelain parser
- rename paths
- filtering

### Integration tests
- temp Git repo status integration

### Adversarial / security tests
- sensitive changed file
- ignored file
- malformed filenames/newlines as supported by porcelain -z

## Acceptance criteria

- [ ] Sensitive changed path does not appear.
- [ ] No Git state mutation.
- [ ] Stable schema returned.

## Implementation checklist

- [ ] Use porcelain `-z` style parser
- [ ] Reuse workspace policy
- [ ] Add tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
