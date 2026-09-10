# V0.7 — UC-C2C-07 and UC-C2C-08 Recovery / HANDOFF

## UC-C2C-07 — BLOCKED and ERROR

The two states have different meanings and must remain machine-distinguishable.

```text
BLOCKED
= task cannot progress without external input/capability/approval

ERROR
= protocol/runtime/infrastructure fault prevents normal progression
```

Examples:

```text
BLOCKED:
- user approval required
- required external credential/capability unavailable
- planner needs missing product requirement

ERROR:
- corrupt checkpoint
- protocol version unsupported
- persistence failure
- runtime/tunnel failure preventing required operation
```

Do not use ERROR for an ordinary implementation defect that should be handled by another PLAN.

### Persisted safe failure state

```text
checkpointState
reasonCode
safeMessage
recoverable
nextExpectedAction?
correlationId?
updatedAt
```

Never persist raw exception stack, bearer/refresh token, pairing code, source, diff or raw log body in the control checkpoint.

### Recovery

Recovery from BLOCKED/ERROR is explicit. The service must validate the actual prerequisite or requested transition rather than simply changing the state back to active.

## UC-C2C-08 — Resume or HANDOFF

### Resume algorithm

```text
load task
 -> validate schema
 -> validate workspace binding
 -> validate checkpoint version/iteration
 -> inspect lease status
 -> derive next expected action
 -> return local recovery result
```

Resume does **not** emit `STATE: RESUME`.

Recommended next-action mapping:

| Checkpoint | Next action |
|---|---|
| InitReady | send_init |
| InitSent | wait_for_plan |
| PlanReceived | begin_execution |
| Executing | inspect_execution_state |
| ExecutionAttached / ExecutedReady | send_executed |
| ExecutedSent / WaitingForReview | wait_for_review |
| Blocked | satisfy_blocker_or_handoff |
| Error | repair_then_resume |
| HandoffReady | open_replacement_conversation |
| Done | none |

### EXECUTING crash rule

This is a hard safety invariant:

```text
restart while Executing
  != automatically execute plan again
```

The caller must inspect worktree/execution evidence and explicitly reconcile before advancing.

## HANDOFF builder

HANDOFF is used when a planning conversation is unavailable or intentionally replaced.

Allowed bounded content:

```text
task id
workspace safe id if needed
goal
constraints
current iteration
last accepted plan summary/digest
last accepted execution evidence id
success criteria summary
known open issues
next expected action
```

Forbidden content:

```text
full transcript
source file body
Git patch/diff body
raw test/build logs
execution artifact body
access/refresh token
pairing code
cookies/browser session data
local secret paths
```

Recommended envelope:

```text
[C2C]
VERSION: 1
STATE: HANDOFF
TASK_ID: <task>
ITERATION: <n>

GOAL:
...

CONSTRAINTS:
...

LAST_EVIDENCE:
exec_x

OPEN_ISSUES:
...

NEXT_ACTION:
Inspect the connected workspace/evidence through MCP and continue from the current checkpoint.
```

HANDOFF remains under the protocol hard cap. If the bounded data does not fit, summarize deterministically rather than truncating in a way that can corrupt required headers.

## Conversation reference

V0.7 may preserve an opaque `conversationReference` supplied by the integration layer, but it must not interpret browser URLs/cookies or drive the browser. V0.8A owns conversation transport semantics.

## Tests

- BLOCKED vs ERROR classification mapping;
- safe reason persistence and redaction;
- expired/stale lease recovery;
- resume after INIT/PLAN/EXECUTING/EXECUTED/DONE;
- EXECUTING crash never auto-reexecutes;
- corrupt session fails closed and is not reset silently;
- stale checkpoint cannot overwrite current state;
- HANDOFF bounded under hard cap;
- HANDOFF excludes secret canaries, source, diff and logs;
- no `RESUME` state is parsed/rendered as valid protocol state;
- conversation reference is treated as opaque local metadata.
