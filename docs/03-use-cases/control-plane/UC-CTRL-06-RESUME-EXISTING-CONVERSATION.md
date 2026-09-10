# UC-CTRL-06 — Resume Existing Conversation

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-08, UC-CTRL-02, UC-CTRL-04 |

## Goal

Resume safely after agent/browser/process restart using the persisted checkpoint and the same bound ChatGPT conversation.

## Main flow

1. Load session and conversation binding.
2. Reacquire executor lease.
3. Reopen the stored conversation reference.
4. Inspect the last accepted local state and latest relevant planner message.
5. Reconcile duplicate/pending send using message hashes.
6. Continue only from the deterministic next action returned by session state.

## Referenced business rules

`BR-CTRL-004`, `BR-CTRL-006`, `BR-CTRL-007`, `BR-C2C-007`, `BR-C2C-008`.

## Failure flows

- Conversation unavailable -> route to `UC-CTRL-07`.
- Ambiguous EXECUTING checkpoint -> inspect worktree/evidence; do not blindly repeat commands.
- Lease conflict -> stop.

## Tests

- restart after PLAN;
- restart after browser submit before acknowledgement;
- restart after evidence record before EXECUTED;
- missing conversation.

## Acceptance criteria

- [ ] Resume does not invent a `RESUME` C2C protocol state.
- [ ] No command/message is blindly duplicated when checkpoint is ambiguous.

## Architecture references

- `01-architecture/12-ERROR-RECOVERY.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
