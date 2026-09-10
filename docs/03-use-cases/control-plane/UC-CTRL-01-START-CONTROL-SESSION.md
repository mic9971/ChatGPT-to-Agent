# UC-CTRL-01 — Start Control Session

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent |
| Depends on | UC-CLI-04, UC-CLI-06, UC-C2C-01 |

## Goal

Create or load a task-bound control session after confirming the local C2C runtime is ready.

## Main flow

1. Agent runs `c2c ensure --json`.
2. READY is required; NEED_SETUP/NEED_PAIRING/BLOCKED is surfaced to the user.
3. Load/create task checkpoint.
4. Acquire the task executor lease.
5. Create a control-session record bound to workspace/task, without browser secrets.
6. Continue to `UC-CTRL-02`.

## Referenced business rules

`BR-CTRL-003`, `BR-CTRL-004`, `BR-CTRL-011`, `BR-CTRL-013`, `BR-C2C-001`.

## Failure flows

- Runtime not ready -> stop without opening browser automation.
- Active lease owned by another executor -> `C2C_EXECUTOR_LEASE_CONFLICT`.
- Persisted task/workspace mismatch -> fail closed.

## Tests

- new session;
- idempotent restart for same task/workspace;
- workspace mismatch;
- concurrent executor conflict;
- no credential material in persisted record.

## Acceptance criteria

- [ ] A control session cannot exist without a valid workspace/task binding.
- [ ] Starting the same pending session is idempotent.
- [ ] No ChatGPT credential/cookie material is persisted.

## Architecture references

- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
- `01-architecture/03-C2C-PROTOCOL.md`
