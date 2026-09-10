# UC-GIT-02 — Read Git Diff

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.2 |
| Module | Git Evidence |
| Primary actor | MCP planner |
| Depends on | UC-GIT-01 |

## Goal

Return bounded diff bodies only for changed paths that pass visibility policy.

## Trigger

`git_diff(base?,cursor,limit)`.

## Preconditions

- Workspace bound.
- Git available.
- Caller has `git.read`.

## Postconditions

- Only allowed diff bodies returned.
- Denied file content/name is not exposed.

## Scope

### In scope
- working tree/index diff as approved
- allowed path list
- bounded diff records

### Out of scope
- Git write
- broad unfiltered diff streaming
- secret path patch

## Referenced business rules

- `BR-COM-003`
- `BR-COM-006`
- `BR-SEC-003`
- `BR-SEC-004`
- `BR-EXE-002`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Determine changed path candidates first using machine-readable Git metadata.
2. Apply workspace visibility policy to each candidate.
3. Construct Git diff command with explicit allowed pathspecs only.
4. Parse/bound output by record/bytes.
5. Return continuation metadata.

## Alternate / failure flows

- No allowed changed files -> empty success.
- Allowed file becomes denied/removed between candidate and diff -> skip/fail safe.
- Git timeout -> typed timeout.

## Detailed design

### Application contracts

- `IGitReader.GetDiffAsync(GitDiffRequest, CancellationToken)`

### Persistence

None.

### Security

Critical invariant: never capture a broad diff containing denied bodies and “filter later”. Allowed pathspec is decided before body retrieval.

### Concurrency / idempotency

Read-only Git operations concurrent with editor changes; response represents observed point in time, not transaction.

### Limits / pagination / timeouts

Hard total diff bytes/records; server page limit.

### Observability

Duration, allowed/denied path counts; never log diff body.

### Error codes

- `GIT_NOT_AVAILABLE`
- `OUTPUT_LIMIT_EXCEEDED`
- `C2C_TIMEOUT`

## Test design

### Unit tests
- path candidate filtering
- diff pagination

### Integration tests
- temp repo with allowed changes

### Adversarial / security tests
- changed `.env` plus normal file; response contains only normal patch
- rename into sensitive path

## Acceptance criteria

- [ ] Denied patch body is never read/returned.
- [ ] Allowed patch is bounded and reviewable.

## Implementation checklist

- [ ] Implement two-stage Git read
- [ ] Add adversarial secret-diff test
- [ ] Add MCP adapter

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
