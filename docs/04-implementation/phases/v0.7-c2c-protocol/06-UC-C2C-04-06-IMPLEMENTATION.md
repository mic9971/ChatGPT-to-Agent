# V0.7 — UC-C2C-04 to UC-C2C-06 Implementation

## Purpose

Connect the protocol checkpoint to existing immutable execution evidence and enforce evidence-backed completion.

## UC-C2C-04 — Submit EXECUTED Evidence Reference

### Inputs

```text
taskId
iteration
executionId
resultSummary?       # bounded, non-sensitive
```

### Validation order

1. Load session by workspace/task.
2. Validate executor lease when the caller is mutating session state.
3. Validate session iteration equals request iteration.
4. Resolve execution record through existing execution contracts.
5. Verify record belongs to the same workspace, task and iteration.
6. Reject missing, partial/unfinalized or mismatched evidence.
7. Persist `lastExecutionId` and `ExecutionAttached` checkpoint atomically.
8. Render deterministic EXECUTED message.

### EXECUTED output

```text
[C2C]
VERSION: 1
STATE: EXECUTED
TASK_ID: <task>
ITERATION: <n>

EXECUTION_ID:
<id>

RESULT:
Execution finished. Independently inspect diff/test evidence through MCP.
```

No source, patch, test log or artifact body may be embedded.

### Retry behavior

If crash occurs after evidence attachment but before external delivery, the same EXECUTED message is re-rendered from persisted state. Do not create another execution record.

### Tests

- matching evidence accepted;
- missing evidence denied;
- wrong task/workspace/iteration denied;
- identical attach/re-render is idempotent;
- conflicting different execution id for finalized same transition fails closed;
- artifact body never appears in rendered message.

## UC-C2C-05 — Review Evidence and Replan

The actual planner review happens outside C2C.Core by reading MCP tools. V0.7 implements the deterministic acceptance path for the planner's next decision.

### Required behavior

After EXECUTED, the session records:

```text
waitingFor = planner_review
lastExecutionId = exec_x
nextExpectedAction = receive_plan_done_blocked_or_error
```

A returned PLAN is validated through the same UC-C2C-02 path and must advance the iteration exactly once.

### Evidence rule

The bridge must not invent a `review passed` result. It can only persist evidence references/checkpoint facts that are locally provable. Planner behavior remains an integration concern until V0.8.

### Tests

- PLAN after EXECUTED advances n -> n+1;
- old PLAN n rejected;
- planner claim without valid envelope rejected;
- required execution evidence missing prevents transition to review-ready state.

## UC-C2C-06 — Complete Task with DONE

### DONE acceptance prerequisites

A DONE message is accepted only when all deterministic local preconditions hold:

```text
active task matches
iteration matches current reviewed iteration
lastExecutionId exists
execution record exists and matches workspace/task/iteration
session reached review-waiting state
success criteria exist for current accepted plan
DONE message is structurally valid and bounded
```

The local system must not claim semantic test/success criteria are satisfied merely because the planner says DONE when required evidence is missing. Where evidence can be checked deterministically (for example required test-status presence), perform that check.

### Persisted terminal state

```text
protocolState = DONE
checkpointState = Done
terminalSummary = bounded safe summary
nextExpectedAction = none
checkpointVersion++
completedAt = now
```

Terminal state is immutable except explicitly approved archival/retention metadata.

### DONE replay

Same task/iteration/DONE canonical digest is idempotent. Any different mutating transition after terminal DONE fails `PROTOCOL_INVALID_STATE`/protocol conflict according to the final code taxonomy.

### Tests

- valid DONE accepted after reviewed execution;
- DONE before any execution rejected;
- DONE after stale iteration rejected;
- DONE with missing execution record rejected;
- duplicate identical DONE succeeds idempotently;
- PLAN/ERROR/HANDOFF mutation after DONE cannot reopen task;
- terminal summary excludes source/diff/log/token content.

## Integration fixture

Provide an end-to-end transport-neutral test fixture:

```text
create task
 -> render INIT
 -> parse/accept PLAN 1
 -> mark EXECUTING
 -> create or fake finalized ExecutionRecord for iteration 1
 -> attach evidence
 -> render EXECUTED
 -> accept PLAN 2
 -> repeat execution
 -> accept DONE
 -> reload persisted session
 -> remains DONE
```

No live ChatGPT/browser dependency is allowed in CI for V0.7.
