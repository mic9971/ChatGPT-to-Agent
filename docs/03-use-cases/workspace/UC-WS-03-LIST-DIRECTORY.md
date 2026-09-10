# UC-WS-03 — List Directory

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.2 |
| Module | Workspace & Filesystem |
| Primary actor | MCP planner |
| Depends on | UC-WS-01 |

## Goal

List visible child entries under a workspace-relative directory with filtering, stable order and pagination.

## Trigger

`list_directory(path,cursor,limit)`.

## Preconditions

- Workspace bound.
- Caller has `workspace.read`.

## Postconditions

- Only allowed entries are returned.
- No denied entry name is leaked.

## Scope

### In scope
- relative path
- file/directory metadata safe subset
- stable sorting
- cursor pagination

### Out of scope
- file body
- hidden denied names
- recursive unlimited listing

## Referenced business rules

- `BR-COM-006`
- `BR-APP-003`
- `BR-SEC-001`
- `BR-SEC-002`
- `BR-SEC-003`
- `BR-SEC-004`
- `BR-APP-005`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Resolve requested directory through shared access policy.
2. Enumerate direct children only.
3. For each child, apply canonical/sensitive/ignore visibility without opening body.
4. Sort by stable normalized relative path/name.
5. Apply server maximum and cursor.
6. Return next cursor when more visible items remain.

## Alternate / failure flows

- Path denied/outside -> deny.
- Directory missing -> not found.
- Entry disappears during enumeration -> skip safely or return deterministic partial policy; never follow outside root.

## Detailed design

### Application contracts

- `IWorkspaceDirectoryReader.ListAsync(RelativePath, PageRequest, CancellationToken)`

### Persistence

None.

### Security

Filtering occurs before name disclosure. Symlink target containment is checked before an entry is marked visible/readable.

### Concurrency / idempotency

Concurrent file changes are tolerated; one page is not a transactional snapshot. Cursor includes request fingerprint/version strategy.

### Limits / pagination / timeouts

Default 100 items; hard max defined in options. No recursive traversal in V1.

### Observability

Count visible/denied entries as metrics without names.

### Error codes

- `WORKSPACE_PATH_DENIED`
- `WORKSPACE_PATH_OUTSIDE_ROOT`
- `WORKSPACE_ITEM_NOT_FOUND`
- `OUTPUT_LIMIT_EXCEEDED`

## Test design

### Unit tests
- visibility filtering
- stable sort
- cursor continuation

### Integration tests
- MCP list against temp workspace

### Adversarial / security tests
- symlink escape child
- sensitive child
- .c2cignore child
- TOCTOU delete during list

## Acceptance criteria

- [ ] Denied names never appear.
- [ ] Pagination returns each visible item at most once for a stable fixture.
- [ ] Root escape attempts fail.

## Implementation checklist

- [ ] Implement directory reader
- [ ] Reuse access policy
- [ ] Implement cursor encoder
- [ ] Add MCP adapter/tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
