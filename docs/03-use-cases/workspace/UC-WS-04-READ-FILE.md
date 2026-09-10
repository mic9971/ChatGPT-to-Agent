# UC-WS-04 — Read File

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.1 |
| Module | Workspace & Filesystem |
| Primary actor | MCP planner |
| Depends on | UC-WS-01 |

## Goal

Read a bounded text chunk from an allowed workspace-relative file.

## Trigger

`read_file(path,start,maxLines/maxBytes)`.

## Preconditions

- Workspace bound.
- Caller has `workspace.read`.

## Postconditions

- Bounded text chunk and continuation metadata returned.
- Denied/binary/unsafe content is not returned.

## Scope

### In scope
- text files
- bounded line/byte chunk
- encoding metadata
- continuation cursor

### Out of scope
- binary streaming
- whole huge file
- sensitive/ignored path

## Referenced business rules

- `BR-COM-002`
- `BR-COM-006`
- `BR-APP-003`
- `BR-SEC-001`
- `BR-SEC-002`
- `BR-SEC-003`
- `BR-SEC-004`
- `BR-COM-011`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate request limits.
2. Resolve canonical file via shared policy.
3. Reject directory/binary/oversized unsupported mode.
4. Open file with safe sharing semantics.
5. Read only requested bounded segment.
6. Return normalized line/cursor metadata.

## Alternate / failure flows

- File changes during read -> return chunk tied to observed fingerprint; continuation may require restart if fingerprint changed.
- Encoding unsupported -> typed error.
- File disappeared -> not found.

## Detailed design

### Application contracts

- `IWorkspaceFileReader.ReadTextAsync(FileReadRequest, CancellationToken)`
- `FileChunk`

### Persistence

None.

### Security

Authorization occurs after canonical resolution and before body open. Sensitive and ignore policy checked on canonical target.

### Concurrency / idempotency

Concurrent writes by external tools allowed; reader does not lock workspace file. Continuation detects incompatible fingerprint changes.

### Limits / pagination / timeouts

Conservative default bytes/lines; absolute hard cap from validated options.

### Observability

Log path hash/relative safe path only if policy permits; never file body.

### Error codes

- `WORKSPACE_PATH_DENIED`
- `WORKSPACE_PATH_OUTSIDE_ROOT`
- `WORKSPACE_ITEM_NOT_FOUND`
- `SENSITIVE_CONTENT_DENIED`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- path policy
- chunk boundary
- cursor/fingerprint

### Integration tests
- MCP HTTP tool integration

### Adversarial / security tests
- `../`
- absolute escape
- symlink escape
- .env
- private key
- large file
- binary file

## Acceptance criteria

- [ ] Allowed file readable.
- [ ] Every escape/sensitive fixture denied.
- [ ] Returned bytes never exceed hard cap.

## Implementation checklist

- [ ] Implement reader after path policy tests are green
- [ ] Add MCP adapter
- [ ] Add adversarial integration tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
