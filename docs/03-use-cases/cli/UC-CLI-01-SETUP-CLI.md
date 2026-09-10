# UC-CLI-01 — Setup CLI

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P0 |
| Phase | V0.6 |
| Module | CLI & Local Lifecycle |
| Primary actor | Local user / Execution Agent |
| Depends on | UC-WS-01 |

## Goal

Perform first-time validation/configuration of workspace, runtime paths and required local dependencies.

## Trigger

`c2c setup`.

## Preconditions

- Binary runs on supported platform.

## Postconditions

- Workspace config exists and prerequisite diagnostics are reported.

## Scope

### In scope
- workspace selection
- config path
- dependency detection
- safe defaults

### Out of scope
- automatic remote pairing unless requested
- installing arbitrary packages silently

## Referenced business rules

- `BR-COM-007`
- `BR-COM-009`
- `BR-COM-010`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Parse options.
2. Call workspace configure.
3. Validate runtime directory permissions.
4. Detect optional Git/search/cloudflared dependencies.
5. Return next steps and JSON envelope when requested.

## Alternate / failure flows

- Dependency missing -> setup can succeed with degraded capability if not required for local slice; doctor reports.

## Detailed design

### Application contracts

- CLI command adapter over application services

### Persistence

Config created by UC-WS-01.

### Security

Never echo secret environment values.

### Concurrency / idempotency

Single setup coordination.

### Limits / pagination / timeouts

Bounded diagnostics.

### Observability

Command result code/duration.

### Error codes

- `C2C_INVALID_ARGUMENT`
- `WORKSPACE_ITEM_NOT_FOUND`

## Test design

### Unit tests
- option validation

### Integration tests
- CLI process exit code/JSON schema

### Adversarial / security tests
- workspace path with spaces/metacharacters

## Acceptance criteria

- [ ] `--json` is stable.
- [ ] Setup does not start remote exposure silently.

## Implementation checklist

- [ ] System.CommandLine or chosen CLI framework
- [ ] Adapters/tests

## Architecture references

- `01-architecture/09-CLI-DESIGN.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
