# 03 — C2C Control Protocol

## Purpose

C2C is an agent coordination contract, not a network transport. Messages may be typed/pasted through ChatGPT UI, transported by browser automation, or carried by a future adapter. The protocol never carries source bodies, diffs or raw logs.

## States

```text
INIT -> PLAN -> EXECUTING? -> EXECUTED -> REVIEW -> PLAN | DONE | BLOCKED | ERROR
                                               \
                                                -> HANDOFF (when replacing conversation)
```

- `INIT`: executor opens a task and requests inspection/plan.
- `PLAN`: planner returns one finite executable iteration.
- `EXECUTING`: optional local checkpoint/progress signal.
- `EXECUTED`: executor states iteration finished and references evidence id(s).
- `REVIEW`: logical planner activity; usually implicit.
- `DONE`: success criteria are met.
- `BLOCKED`: progress requires unavailable input/capability.
- `ERROR`: protocol/infrastructure fault.
- `HANDOFF`: bounded continuation brief to a replacement conversation.

There is deliberately no `RESUME` protocol state. Resume is local checkpoint logic.

## Common envelope

```text
[C2C]
VERSION: 1
STATE: INIT|PLAN|EXECUTED|DONE|BLOCKED|ERROR|HANDOFF
TASK_ID: <stable id>
ITERATION: <non-negative integer>
WORKSPACE_ID: <optional public/salted identifier>
```

Control messages SHOULD stay under 1 KB. Hard parser cap: 4 KB. Unknown headers are ignored only when protocol version allows forward-compatible extension; unknown required fields fail validation.

## INIT

```text
[C2C]
VERSION: 1
STATE: INIT
TASK_ID: c2c_ab12
ITERATION: 0

GOAL:
Implement X.

CONSTRAINTS:
No API/UI changes.

INSTRUCTION:
Inspect the connected workspace through MCP and return one finite plan.
```

## PLAN

PLAN must be bounded and executable, not a 40-step epic.

```text
[C2C]
VERSION: 1
STATE: PLAN
TASK_ID: c2c_ab12
ITERATION: 1

RATIONALE:
...

ACTIONS:
1. ...
2. ...

FILES_LIKELY_INVOLVED:
...

TESTS:
...

SUCCESS_CRITERIA:
...
```

## EXECUTED

```text
[C2C]
VERSION: 1
STATE: EXECUTED
TASK_ID: c2c_ab12
ITERATION: 1

EXECUTION_ID:
exec_01J...

RESULT:
Execution finished. Independently inspect diff/test evidence through MCP.
```

Do not paste changed file bodies or test logs. The execution record is local evidence, and the planner reads it through MCP.

## Idempotency

- `(task_id, iteration, state)` transitions are checked against the current checkpoint.
- Duplicate delivery of a message that matches persisted content hash is idempotent.
- Conflicting duplicate messages for the same transition are rejected as `C2C_PROTOCOL_CONFLICT`.
- `record` requires a caller-generated or runtime-generated idempotency key for retries.

## Concurrency

V1 permits one active executor lease per task. The local session store maintains a short lease with owner id and heartbeat/expiry. A second agent may read state but cannot advance the task until the lease expires or is explicitly taken over.

## HANDOFF

HANDOFF contains only bounded task state: goal, constraints, last accepted plan summary, iteration, known issues, success criteria and next expected action. It never includes raw logs, code or diff bodies. The new planner re-reads workspace evidence over MCP.
