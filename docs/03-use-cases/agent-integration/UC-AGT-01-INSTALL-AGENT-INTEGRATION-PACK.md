# UC-AGT-01 — Install Agent Integration Pack

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Local user / Agent bootstrap |
| Depends on | UC-CLI-04 |

## Goal

Install or expose a small integration instruction pack that teaches a coding agent how to operate C2C.NET without coupling Core to that agent.

## Trigger

User chooses Antigravity/Codex/etc integration.

## Preconditions

- C2C CLI available.

## Postconditions

- Agent-specific instructions are available in expected location or explicitly referenced.

## Scope

### In scope
- markdown skill/rules
- version metadata
- read order

### Out of scope
- embedding agent SDK into Core V1
- granting extra capabilities

## Referenced business rules

- `BR-COM-002`
- `BR-COM-010`
- `BR-COM-012`

The referenced BR files under `docs/02-common/` are normative. If this use case conflicts with a BR, the BR wins unless an approved ADR explicitly changes the rule.

## Main flow

1. Select integration pack.
2. Validate target install path is user-approved.
3. Copy/link versioned instruction files.
4. Return installed version and usage trigger.

## Alternate / failure flows

- Target format unsupported -> return manual instructions rather than guessing.

## Detailed design

### Application contracts

- `IAgentIntegrationInstaller` optional; V1 may be documented/manual

### Persistence

Integration pack files only.

### Security

Instructions cannot change bridge permission model.

### Concurrency / idempotency

Install idempotent by version/path.

### Limits / pagination / timeouts

Small files.

### Observability

Install event/path category without personal path if telemetry.

### Error codes

- `C2C_INVALID_ARGUMENT`

## Test design

### Unit tests
- version/idempotency

### Integration tests
- manual/fixture install

### Adversarial / security tests
- malicious existing file -> do not overwrite unless explicit flag/backup policy

## Acceptance criteria

- [ ] Core has no dependency on Antigravity/Codex SDK.

## Implementation checklist

- [ ] Define pack manifest
- [ ] Installer or manual docs
- [ ] Tests if automated

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`

## Agent implementation rule

Implement **only this use case and its explicit dependencies**. Read the referenced BR files first. Do not pre-build later use cases. If implementation evidence requires a design change, stop and update/approve the design before broadening scope.
