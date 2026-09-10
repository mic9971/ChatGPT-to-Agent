# UC-WS-01 — Configure and Bind Workspace

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.1 |
| Module | Workspace & Filesystem |
| Primary actor | Local user / CLI |
| Depends on | None |

## Goal

Create validated local configuration for exactly one workspace root and derive a stable workspace identity.

## Trigger

`c2c setup --workspace <path>` or first-run setup.

## Preconditions

- Requested directory exists or setup can report not found.
- No active runtime currently owns the workspace.

## Postconditions

- Canonical workspace root is persisted atomically.
- A stable workspace id is available to subsequent operations.

## Scope

### In scope
- Canonicalize root
- Validate readable directory
- Detect Git repository as metadata
- Persist workspace config
- Acquire/validate ownership lock metadata

### Out of scope
- Remote MCP access
- Tunnel startup
- OAuth pairing

## Referenced business rules

- `BR-COM-001`
- `BR-COM-007`
- `BR-COM-009`
- `BR-COM-011`
- `BR-SEC-001`
- `BR-SEC-005`
- `BR-CON-002`
- `BR-CON-003`
- `BR-COM-010`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. CLI validates argument and expands only local user input.
2. Resolve the root to canonical absolute path.
3. Reject file/non-directory roots and unsupported filesystem errors with stable code.
4. Create workspace id from a stable salted strategy that does not expose the raw absolute path.
5. Persist config by atomic replace.
6. Return safe summary and next step.

## Alternate / failure flows

- Already configured with same canonical root -> idempotent success.
- Configured for a different root while runtime active -> conflict.
- Permission denied/canonicalization unresolved -> fail closed.

## Detailed design

### Application contracts

- `IWorkspaceConfigurator.ConfigureAsync(WorkspaceConfigureRequest, CancellationToken)`
- `IWorkspaceIdentityFactory.Create(canonicalRoot)`
- `IWorkspaceConfigStore.SaveAsync(...)`

### Persistence

User-scoped config JSON; atomic write; do not persist secrets in workspace config.

### Security

Raw canonical path is local-only; remote responses use workspace id/safe label. Canonicalization errors fail closed.

### Concurrency / idempotency

Serialize configuration per config store; ownership metadata must not be overwritten by concurrent setup.

### Limits / pagination / timeouts

No unbounded enumeration required; Git detection uses bounded process timeout.

### Observability

Log config operation/result code and workspace id; never log token/secret.

### Error codes

- `WORKSPACE_ITEM_NOT_FOUND`
- `WORKSPACE_PATH_DENIED`
- `C2C_CONFLICT`

## Test design

### Unit tests
- canonical root normalization
- idempotent same-root configure
- different-root conflict
- atomic write recovery

### Integration tests
- CLI JSON/text contract
- config reload after process restart

### Adversarial / security tests
- permission denied root
- symlinked root resolution
- malformed config recovery

## Acceptance criteria

- [ ] Same workspace setup can be repeated safely.
- [ ] Different workspace cannot silently replace active binding.
- [ ] Persisted config survives restart and contains no secret token.

## Implementation checklist

- [ ] Implement contracts in Core
- [ ] Implement canonical root resolver
- [ ] Implement config store
- [ ] Add CLI adapter later or test service directly
- [ ] Add unit/integration tests

## Architecture references

- `01-architecture/02-HIGH-LEVEL-ARCHITECTURE.md`
- `01-architecture/05-WORKSPACE-SECURITY.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
