# 01 — Software Requirements Specification (SRS)

## Functional requirements

### FR-01 Workspace binding
The bridge SHALL serve exactly one configured workspace root per running instance. Every remote access token SHALL be bound to that workspace identity.

### FR-02 Loopback host
The local HTTP host SHALL bind only to loopback by default. Configuration that attempts wildcard/public binding SHALL fail closed in V1.

### FR-03 MCP read-only tools
The bridge SHALL expose read-only workspace/evidence tools. V1 target set:

- `workspace_info`
- `list_directory`
- `read_file`
- `search_workspace`
- `git_status`
- `git_diff`
- `test_status`
- `execution_summary`
- `execution_output`

No write/delete/shell/commit/package-install tool SHALL exist.

### FR-04 Path containment
Every tool that references a path SHALL pass through one canonical workspace policy. Path traversal, absolute escapes, symlink escapes, null-byte tricks and denied sensitive paths SHALL be rejected.

### FR-05 Sensitive content
Sensitive files SHALL be denied by default. A user-maintained `.c2cignore` SHALL add exclusions. Example/sample environment files MAY be explicitly allowed.

### FR-06 Bounded outputs
File reads, search matches, directory listings, diffs and execution outputs SHALL have hard byte/item/line caps and deterministic pagination/cursors where needed.

### FR-07 Git evidence
Git tools SHALL be read-only and SHALL filter denied paths before any diff body is returned.

### FR-08 Execution record
The execution agent SHALL be able to persist a bounded execution record containing task id, iteration, command classification/metadata, exit status, changed files, test summary and references to optional log artifacts.

### FR-09 Execution log sanitizer
Execution-output bodies SHALL be sanitized before exposure. Private-key blocks and explicitly restricted content SHALL not be returned.

### FR-10 C2C task protocol
The system SHALL define transport-independent states: `INIT`, `PLAN`, optional `EXECUTING`, `EXECUTED`, `REVIEW`, `DONE`, `BLOCKED`, `ERROR`, `HANDOFF`.

### FR-11 Checkpoint/resume
Local task checkpoints SHALL support process restart without inventing an MCP session or a `RESUME` protocol state. If the old planning conversation is unavailable, HANDOFF SHALL carry only a bounded task brief.

### FR-12 Auth discovery
For remote HTTP MCP, the server SHALL implement current MCP authorization discovery requirements including Protected Resource Metadata and authorization-server discovery when authorization is enabled.

### FR-13 Client registration strategy
CIMD SHALL be the preferred modern registration path when interoperable. DCR SHALL be optional compatibility support only. Pre-registration MAY be supported for controlled clients.

### FR-14 Pairing
Interactive setup SHALL use a short-lived one-time pairing/approval mechanism. Long-lived access/refresh credentials SHALL not be exposed in human-visible control messages.

### FR-15 Tunnel provider
The runtime SHALL abstract public connectivity behind `ITunnelProvider`. Initial provider: Cloudflare Quick Tunnel; stable/named tunnel can follow.

### FR-16 CLI
The CLI SHALL support at minimum: `setup`, `start`, `stop`, `status`, `doctor`, `ensure`, `pair`, `unpair`, `session`, `record`, `logs`.

### FR-17 Machine-readable mode
Agent-facing CLI operations SHALL provide stable `--json` output with schema/version fields and stable error codes.

### FR-18 Automated control transport
The system SHALL support a replaceable control-plane transport that can relay validated bounded C2C messages between an execution agent and ChatGPT Web. The first automated profile targets Antigravity browser/computer-use capability; a manual operator-assisted transport SHALL remain available.

### FR-19 Conversation binding and recovery
An automated planning conversation SHALL be bound to one workspace/task identity. Non-secret conversation references and last accepted C2C checkpoint MAY be persisted to support restart. Missing conversations SHALL use explicit HANDOFF rather than silent context replacement.

### FR-20 Browser authentication boundary
ChatGPT account authentication remains user-owned. The execution agent SHALL NOT capture or persist passwords, passkeys, cookies, CAPTCHA responses, MFA/2FA secrets or equivalent browser credential material. Authentication challenges SHALL return a user-action-required state.

## Non-functional summary

- Security-first and fail-closed.
- Fast startup and low idle memory.
- Cross-platform.
- Deterministic behavior for protocol, path gates and pagination.
- No database/Redis/RabbitMQ in V1.
- Structured logs with redaction.
- High unit-test coverage on security and protocol state transitions.
- Integration tests for MCP/auth/CLI lifecycle.
- Control-plane browser automation is replaceable and recoverable; CI uses deterministic fake drivers rather than live ChatGPT credentials.

## Out of scope for V1

- Remote arbitrary command execution.
- MCP write APIs.
- Multi-workspace bridge.
- Web management dashboard.
- Distributed execution queue.
- Automatic Git commit/push.
- Full proprietary coding-agent SDK integration inside C2C.Core.
- Capturing or bypassing ChatGPT login credentials, CAPTCHA, passkeys or MFA/2FA.
