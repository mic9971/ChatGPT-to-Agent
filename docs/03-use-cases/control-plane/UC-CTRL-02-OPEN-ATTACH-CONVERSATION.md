# UC-CTRL-02 — Open or Attach Planner Conversation

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent / Control Driver |
| Depends on | UC-CTRL-01 |

## Goal

Open a new ChatGPT planning conversation or attach to the previously bound conversation without mixing task/workspace identity.

## Main flow

1. If a valid conversation reference exists, open it.
2. Verify ChatGPT host/context and that the user is authenticated.
3. If no reference exists, create a new conversation and persist its non-secret reference.
4. Bind the reference to `workspace_id + task_id`.
5. Continue only after the composer is semantically available.

## Referenced business rules

`BR-CTRL-003`, `BR-CTRL-004`, `BR-CTRL-009`, `BR-CTRL-013`.

## Failure flows

- Login required / MFA / CAPTCHA -> `CTRL_USER_AUTH_REQUIRED`, pause for user action.
- Stored conversation missing -> do not silently replace; route to `UC-CTRL-07`.
- Wrong workspace/task binding -> reject.

## Tests

Use a fake control driver for CI. Real Antigravity browser flow is a separate smoke test.

## Acceptance criteria

- [ ] Existing conversation is reused only for its bound task/workspace.
- [ ] Authentication challenge causes explicit user handoff, not bypass.
- [ ] Conversation reference contains no credential material.

## Architecture references

- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
