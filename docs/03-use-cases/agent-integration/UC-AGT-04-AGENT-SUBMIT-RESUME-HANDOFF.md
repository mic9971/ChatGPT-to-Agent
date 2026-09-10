# UC-AGT-04 — Agent Submit / Resume / Handoff

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Execution Agent Integration |
| Primary actor | Execution Agent |
| Depends on | UC-C2C-04, UC-C2C-06, UC-C2C-08, UC-CTRL-05, UC-CTRL-06, UC-CTRL-07, UC-CTRL-08 |

## Goal

After local execution, record evidence, relay EXECUTED through the validated control transport, follow planner DONE/PLAN/BLOCKED, and recover safely after browser/agent interruptions.

## Main flow

1. Persist execution evidence (`c2c record`).
2. `UC-CTRL-05` relays EXECUTED and receives planner review outcome.
3. PLAN -> execute next iteration via `UC-AGT-03`.
4. DONE -> close task/session.
5. Restart with conversation available -> `UC-CTRL-06`.
6. Conversation unavailable -> `UC-CTRL-07` HANDOFF.
7. Browser/control-driver failure -> `UC-CTRL-08` recovery/manual fallback.

## Referenced business rules

- `BR-C2C-004`
- `BR-C2C-005`
- `BR-C2C-008`
- `BR-C2C-009`
- `BR-CTRL-006`
- `BR-CTRL-007`
- `BR-CTRL-008`
- `BR-CTRL-012`

## Security

No raw artifacts in control messages. Browser account authentication is user-owned. Driver failure never changes the accepted protocol checkpoint.

## Concurrency / idempotency

Evidence record and control-message relay are independently idempotent. On restart inspect the persisted checkpoint and conversation before retrying either side.

## Tests

- E2E two-iteration scenario;
- restart between record and EXECUTED relay;
- duplicate planner response;
- browser crash;
- missing conversation -> HANDOFF;
- manual fallback.

## Acceptance criteria

- [ ] Task identity/iteration preserved across restart.
- [ ] EXECUTED is never sent before evidence persistence succeeds.
- [ ] Browser failure cannot produce a false DONE/PLAN transition.

## Architecture references

- `01-architecture/10-AGENT-INTEGRATION.md`
- `01-architecture/12-ERROR-RECOVERY.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`

## Agent implementation rule

Implement only this UC and its explicit dependencies. Do not pre-build later use cases.
