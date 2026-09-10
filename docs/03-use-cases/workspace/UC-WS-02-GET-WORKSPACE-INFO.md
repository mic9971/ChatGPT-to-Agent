# UC-WS-02 — Get Workspace Info

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.2 |
| Module | Workspace & Filesystem |
| Primary actor | MCP planner / local diagnostics |
| Depends on | UC-WS-01 |

## Goal

Return safe metadata describing the bound workspace and supported capabilities without leaking absolute local secrets.

## Trigger

`workspace_info` MCP tool or local diagnostic query.

## Preconditions

- Bridge is bound to a workspace.
- Caller has required workspace read authorization when remote.

## Postconditions

- Safe workspace metadata is returned.
- No workspace content is modified.

## Scope

### In scope
- workspace id
- safe display name
- Git presence/current branch when safe
- capabilities/tool limits
- bridge version

### Out of scope
- absolute home path
- secret config values
- full environment dump

## Referenced business rules

- `BR-COM-002`
- `BR-COM-005`
- `BR-COM-006`
- `BR-SEC-006`
- `BR-SEC-007`
- `BR-OBS-001`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Validate caller/tool scope.
2. Read immutable workspace context.
3. Read bounded Git metadata if available.
4. Map to safe DTO.
5. Return protocol/tool limit metadata.

## Alternate / failure flows

- No Git repository -> return `git.available=false`, not error.
- Git metadata timeout -> return safe degraded metadata with warning code if contract supports it.

## Detailed design

### Application contracts

- `IWorkspaceQuery.GetInfoAsync`
- `WorkspaceInfoDto`

### Persistence

None.

### Security

Never return canonical absolute root to remote MCP caller.

### Concurrency / idempotency

Read-only; context immutable after runtime start.

### Limits / pagination / timeouts

Metadata only; branch/name lengths bounded.

### Observability

MCP operation latency and safe result status.

### Error codes

- `C2C_NOT_READY`
- `AUTH_INSUFFICIENT_SCOPE`
- `GIT_NOT_AVAILABLE`

## Test design

### Unit tests
- safe DTO mapping
- Git absent behavior

### Integration tests
- MCP tool call returns expected schema

### Adversarial / security tests
- workspace directory contains sensitive names; info does not enumerate them

## Acceptance criteria

- [ ] Tool returns stable schema.
- [ ] No absolute home/workspace path leaks remotely.
- [ ] No mutation occurs.

## Implementation checklist

- [ ] Create query contract
- [ ] Implement safe mapper
- [ ] Add MCP adapter
- [ ] Add tests

## Architecture references

- `01-architecture/04-MCP-DESIGN.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
