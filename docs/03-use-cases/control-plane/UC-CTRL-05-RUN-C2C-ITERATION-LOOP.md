# UC-CTRL-05 — Run C2C Iteration Control Loop

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent |
| Depends on | UC-CTRL-03, UC-CTRL-04, UC-EXE-01, UC-C2C-04, UC-C2C-05, UC-C2C-06 |

## Goal

Drive one or more bounded PLAN -> local execution -> evidence -> EXECUTED -> review iterations until a terminal state.

## Main flow

1. Send INIT or continue from the last accepted checkpoint.
2. Receive PLAN.
3. Execution agent edits/builds/tests locally; the control driver does not execute code itself.
4. Persist evidence via `c2c record`.
5. Relay EXECUTED referencing evidence id.
6. Receive PLAN, DONE, BLOCKED or ERROR.
7. Repeat only for a new valid PLAN; stop on a terminal state.

## Referenced business rules

`BR-CTRL-002`, `BR-CTRL-005`, `BR-CTRL-008`, `BR-COM-003`, `BR-EXE-001`, `BR-C2C-004`.

## Failure flows

- Evidence persistence fails -> do not send EXECUTED.
- Planner cannot read evidence -> do not mark DONE locally.
- Agent interrupted during EXECUTING -> resume uses checkpoint inspection before repeating commands.

## Tests

- one-iteration DONE;
- two-iteration replan;
- evidence write failure;
- BLOCKED terminal;
- restart between record and EXECUTED relay.

## Acceptance criteria

- [ ] Every EXECUTED references persisted evidence.
- [ ] Planner review remains evidence-first through MCP.
- [ ] Control automation never mutates workspace through MCP.

## Architecture references

- `01-architecture/08-EXECUTION-REVIEW.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
