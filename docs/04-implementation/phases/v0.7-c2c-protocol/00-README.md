# Phase V0.7 — C2C Protocol, Checkpoint and Session CLI

## Objective

Implement the provider-neutral C2C coordination state machine before any browser/control-plane automation is introduced.

Phase V0.7 owns:

- `UC-C2C-01` Initialize C2C Task
- `UC-C2C-02` Accept Planner PLAN
- `UC-C2C-03` Mark Local Execution Started
- `UC-C2C-04` Submit EXECUTED Evidence Reference
- `UC-C2C-05` Review Evidence and Replan transition
- `UC-C2C-06` Complete Task with DONE
- `UC-C2C-07` Handle BLOCKED or ERROR
- `UC-C2C-08` Resume or HANDOFF Task
- `UC-CLI-06` Manage C2C Session

## Non-goals

V0.7 does **not** implement:

- ChatGPT browser automation;
- Antigravity browser/computer-use driving;
- `IControlPlaneDriver` concrete browser providers;
- automatic code execution;
- new MCP write/exec tools;
- transcript persistence;
- raw source/diff/log transport through C2C messages.

Those belong to V0.8A/V0.8B.

## Implementation waves

```text
V0.7A Protocol substrate + session store
  -> envelope/parser/renderer
  -> state machine
  -> atomic checkpoint store
  -> executor lease

V0.7B INIT / PLAN / EXECUTING
  -> UC-C2C-01..03

V0.7C EXECUTED / review / DONE
  -> UC-C2C-04..06

V0.7D BLOCKED / ERROR / resume / HANDOFF
  -> UC-C2C-07..08

V0.7E CLI session adapter
  -> UC-CLI-06

Final gate
  -> deterministic fake/manual transport tests
  -> V0.8A handoff
```

## Entry gate

Runtime implementation must not begin until the V0.6 CLI runtime gate is complete on the implementation branch being used. Documentation may be merged before that.

## Primary invariants

1. `TASK_ID` is stable for one logical task.
2. `ITERATION` is monotonic.
3. C2C is a control protocol, not a source/diff/log transport.
4. Duplicate identical transitions are idempotent; conflicting duplicates fail closed.
5. One active executor lease may advance a task at a time.
6. `EXECUTED` references immutable execution evidence by id.
7. `DONE` requires current reviewed evidence and satisfied success criteria.
8. Resume is local recovery logic, never a `RESUME` protocol state.
9. HANDOFF is bounded and contains no raw source, diff, logs, token or transcript.
10. Browser/provider automation remains outside the V0.7 core.

## Read order

1. `01-BASELINE-AND-ENTRY-GATE.md`
2. `02-PROTOCOL-AND-SESSION-ARCHITECTURE.md`
3. `03-STATE-MACHINE-AND-TRANSITIONS.md`
4. `04-PERSISTENCE-LEASE-IDEMPOTENCY.md`
5. `05-UC-C2C-01-03-IMPLEMENTATION.md`
6. `06-UC-C2C-04-06-IMPLEMENTATION.md`
7. `07-UC-C2C-07-08-RECOVERY-HANDOFF.md`
8. `08-UC-CLI-06-SESSION-COMMANDS.md`
9. `09-TEST-AND-SECURITY-GATE.md`
10. `10-ANTIGRAVITY-EXECUTION-ORDER.md`
11. `11-PHASE-DONE-AND-V0.8A-HANDOFF.md`
