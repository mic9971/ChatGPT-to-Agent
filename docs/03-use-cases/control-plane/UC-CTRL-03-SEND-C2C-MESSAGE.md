# UC-CTRL-03 — Send C2C Control Message

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Control Driver |
| Depends on | UC-CTRL-02, UC-C2C-01 |

## Goal

Relay exactly one validated bounded C2C envelope to the active planner conversation.

## Preconditions

- Active task-bound conversation.
- Envelope passes C2C protocol validation.

## Main flow

1. Validate protocol version/state/task/iteration and size.
2. Compute deterministic content hash.
3. If the same transition/hash was already confirmed, return idempotent success.
4. Locate the composer semantically.
5. Insert the complete message and submit once.
6. Persist pending-send hash/timestamp; do not advance protocol state merely because UI submission was attempted.

## Referenced business rules

`BR-CTRL-001`, `BR-CTRL-002`, `BR-CTRL-005`, `BR-CTRL-007`, `BR-CTRL-009`, `BR-COM-004`.

## Failure flows

- Oversized envelope -> protocol validation error.
- Conflicting duplicate -> `C2C_PROTOCOL_CONFLICT`.
- Browser send timeout -> inspect conversation before resending.

## Tests

- valid send;
- duplicate same hash;
- conflicting duplicate;
- oversized message;
- timeout after submit.

## Acceptance criteria

- [ ] Raw source/diff/log is never relayed by this use case.
- [ ] Retry cannot create a conflicting local state transition.

## Architecture references

- `01-architecture/03-C2C-PROTOCOL.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
