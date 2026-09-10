# UC-CTRL-07 — Handoff to Replacement Conversation

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-08, UC-CTRL-02, UC-CTRL-03 |

## Goal

Create a replacement planning conversation when the previous one cannot be used, preserving bounded task continuity without copying repository evidence into the control message.

## Main flow

1. Confirm the old conversation is unavailable or explicitly replaced.
2. Generate the protocol-defined bounded HANDOFF brief from persisted session state.
3. Open a new conversation.
4. Bind the new conversation reference to the same workspace/task.
5. Send HANDOFF.
6. Planner re-reads workspace/evidence through MCP and returns the next valid state.

## Referenced business rules

`BR-CTRL-001`, `BR-CTRL-002`, `BR-CTRL-004`, `BR-CTRL-005`, `BR-C2C-009`.

## Security

HANDOFF never contains raw source, diff, log, OAuth credentials, browser credentials or sensitive absolute paths.

## Tests

- successful handoff;
- oversized handoff rejected;
- cross-task conversation binding rejected;
- evidence remains MCP-only.

## Acceptance criteria

- [ ] Replacement conversation can reconstruct task intent from bounded state and MCP evidence.
- [ ] Old/new conversation references cannot be attached to a different workspace/task silently.

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
