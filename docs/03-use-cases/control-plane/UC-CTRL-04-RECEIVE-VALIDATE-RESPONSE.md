# UC-CTRL-04 — Receive and Validate Planner Response

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Control Driver / C2C Protocol Service |
| Depends on | UC-CTRL-03, UC-C2C-02, UC-C2C-07 |

## Goal

Read the latest completed planner response and accept it only when it is a valid transition for the active task and iteration.

## Main flow

1. Wait with bounded timeout/cancellation.
2. Identify the latest completed assistant response for the active conversation.
3. Parse the C2C envelope.
4. Validate version, task id, iteration and allowed next state.
5. Compute response hash.
6. Treat an identical previously accepted hash as duplicate/idempotent.
7. Persist the accepted transition only after validation.

## Referenced business rules

`BR-CTRL-005`, `BR-CTRL-006`, `BR-CTRL-007`, `BR-C2C-003`, `BR-C2C-006`.

## Failure flows

- Plain non-C2C response -> bounded retry/manual intervention, not implicit PLAN.
- Wrong task id or stale iteration -> reject.
- Browser crash -> checkpoint unchanged.
- Conflicting response -> `C2C_PROTOCOL_CONFLICT`.

## Tests

- PLAN accepted;
- DONE accepted after EXECUTED;
- wrong task id;
- stale iteration;
- duplicate response;
- browser crash before parse.

## Acceptance criteria

- [ ] Browser/UI output never bypasses C2C state validation.
- [ ] A failed read cannot advance the task.

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
