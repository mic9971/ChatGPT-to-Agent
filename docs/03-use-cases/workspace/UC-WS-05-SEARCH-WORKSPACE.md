# UC-WS-05 — Search Workspace

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.2 |
| Module | Workspace & Filesystem |
| Primary actor | MCP planner |
| Depends on | UC-WS-03, UC-WS-04 |

## Goal

Search visible workspace text with bounded matches/snippets while never exposing denied paths/content.

## Trigger

`search_workspace(query,pathScope,cursor,limit)`.

## Preconditions

- Workspace bound.
- Caller has `workspace.search`.

## Postconditions

- Visible bounded matches returned in stable order.

## Scope

### In scope
- literal/default text search
- optional safe relative scope
- path+line+bounded snippet
- pagination

### Out of scope
- arbitrary shell regex injection
- search of sensitive/ignored content
- unbounded recursive output

## Referenced business rules

- `BR-COM-006`
- `BR-APP-003`
- `BR-SEC-003`
- `BR-SEC-004`
- `BR-SEC-007`
- `BR-APP-005`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate query length/options.
2. Resolve optional scope.
3. Build search command through typed arguments or managed implementation; never shell-concatenate user input.
4. Provide ignore/sensitive exclusions before content output.
5. Normalize candidate paths and re-check shared policy.
6. Bound snippet/total matches and paginate.

## Alternate / failure flows

- Search executable unavailable -> typed capability error or managed fallback.
- Timeout -> partial results only if contract explicitly marks incomplete; otherwise timeout error.

## Detailed design

### Application contracts

- `IWorkspaceSearch.SearchAsync(SearchRequest, CancellationToken)`
- `ISearchBackend`

### Persistence

None.

### Security

Never invoke `/bin/sh -c` or equivalent with query. Denied files excluded before snippets are returned.

### Concurrency / idempotency

Search process is cancellable and killed only if owned. Multiple searches may run concurrently under concurrency limits.

### Limits / pagination / timeouts

Query length, max matches, max snippet bytes, process timeout all server bounded.

### Observability

Search duration/result counts; no raw query if configured sensitive logging policy says hash only.

### Error codes

- `WORKSPACE_PATH_DENIED`
- `OUTPUT_LIMIT_EXCEEDED`
- `C2C_TIMEOUT`
- `C2C_INVALID_ARGUMENT`

## Test design

### Unit tests
- argument escaping
- filtering
- pagination
- timeout mapping

### Integration tests
- real/fake search backend fixture

### Adversarial / security tests
- query containing shell metacharacters
- ignored/sensitive matches
- symlink tree

## Acceptance criteria

- [ ] No command injection path exists.
- [ ] Denied files never appear.
- [ ] Timeout/cancellation bounded.

## Implementation checklist

- [ ] Define search backend abstraction
- [ ] Implement rg/managed backend
- [ ] Apply shared policy
- [ ] Add MCP adapter/tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
