# Common Observability and Error Rules

## Observability

| ID | Rule |
|---|---|
| BR-OBS-001 | Logs are structured and contain operation/correlation ids, not source/log bodies. |
| BR-OBS-002 | Metrics cover request counts, latency, denied-access counts, tunnel state transitions and auth failures without secret dimensions. |
| BR-OBS-003 | C2C task id / execution id may be used as correlation identifiers after validation and bounding. |
| BR-OBS-004 | Health endpoints reveal only safe process readiness information unless authenticated locally. |

## Common error codes

- `C2C_INVALID_ARGUMENT`
- `C2C_NOT_READY`
- `C2C_CONFLICT`
- `C2C_TIMEOUT`
- `C2C_CANCELLED`
- `WORKSPACE_PATH_DENIED`
- `WORKSPACE_PATH_OUTSIDE_ROOT`
- `WORKSPACE_ITEM_NOT_FOUND`
- `SENSITIVE_CONTENT_DENIED`
- `OUTPUT_LIMIT_EXCEEDED`
- `GIT_NOT_AVAILABLE`
- `EXECUTION_NOT_FOUND`
- `AUTH_INVALID_TOKEN`
- `AUTH_INSUFFICIENT_SCOPE`
- `AUTH_PAIRING_EXPIRED`
- `AUTH_PAIRING_REJECTED`
- `TUNNEL_START_FAILED`
- `PROTOCOL_INVALID_STATE`
- `PROTOCOL_VERSION_UNSUPPORTED`

Error responses SHALL include a stable code, safe human-readable message and optional correlation id. They SHALL NOT include stack trace, token, source body, command output or raw absolute home path.
