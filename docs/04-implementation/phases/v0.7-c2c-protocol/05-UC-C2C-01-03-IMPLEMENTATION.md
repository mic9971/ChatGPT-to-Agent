# V0.7 — UC-C2C-01 to UC-C2C-03 Implementation

## Scope

This wave establishes task creation, planner PLAN acceptance, and the optional local EXECUTING checkpoint.

## UC-C2C-01 — Initialize C2C Task

### Inputs

```text
workspaceId
goal
constraints[]
requestKey?          # for caller retry idempotency
```

### Required behavior

1. Validate workspace binding exists.
2. Validate goal/constraints bounds before persistence.
3. Generate stable opaque task id.
4. Create session at iteration 0.
5. Persist atomically.
6. Render deterministic INIT envelope.
7. Return task/session metadata plus rendered control message.

### INIT renderer

Required headers:

```text
[C2C]
VERSION: 1
STATE: INIT
TASK_ID: <id>
ITERATION: 0
WORKSPACE_ID: <safe id when configured by protocol profile>
```

Body contains only bounded `GOAL`, `CONSTRAINTS` and planner instruction. Never include file bodies, Git diff, execution output, auth token or local absolute path.

### Tests

- create task and reload;
- caller request-key retry returns same logical task;
- same request key with conflicting payload fails;
- goal at limits;
- oversized goal/constraints fail before write;
- goal containing fake `[C2C]`, `STATE: DONE`, prompt-injection text remains escaped/data and cannot alter envelope headers;
- task bound to correct workspace.

## UC-C2C-02 — Accept Planner PLAN

### Parse then validate

Planner text is untrusted. Required sequence:

```text
bounded raw text
 -> parse
 -> validate VERSION
 -> require STATE=PLAN
 -> task id match
 -> expected iteration
 -> state transition
 -> required PLAN sections
 -> bounded section counts/lengths
 -> canonical digest
 -> atomic session update
```

Minimum semantic PLAN sections:

```text
RATIONALE
ACTIONS
TESTS
SUCCESS_CRITERIA
```

`FILES_LIKELY_INVOLVED` may be optional if the plan legitimately requires no file change, but the selected schema must be documented and deterministic.

### Bounded-plan rules

The validator should use structural limits, not subjective AI judgment alone. Examples:

- maximum action count;
- maximum test item count;
- maximum success-criteria count;
- per-section character caps;
- total 4 KB protocol hard cap.

The agent/policy layer may apply stricter semantic review, but the parser must remain deterministic.

### Iteration

First PLAN after INIT is iteration 1. A later PLAN after review is exactly previous executable iteration + 1.

### Duplicate handling

Same canonical PLAN digest for the same task/iteration/state returns idempotent success. A different PLAN for the already accepted same task/iteration fails `C2C_PROTOCOL_CONFLICT`.

### Tests

- valid first PLAN;
- valid replan;
- wrong task;
- stale/future iteration;
- missing ACTIONS/TESTS/SUCCESS_CRITERIA;
- duplicate identical PLAN;
- conflicting duplicate PLAN;
- unsupported version;
- line-ending canonicalization;
- malicious plan cannot change workspace/auth/security state.

## UC-C2C-03 — Mark Local Execution Started

### Required behavior

`MarkExecutingAsync` validates:

- current checkpoint is executable (`PlanReceived` or explicitly allowed equivalent);
- requested iteration equals current PLAN iteration;
- caller holds executor lease;
- terminal session cannot transition;
- no command/source body is recorded.

Persist:

```text
checkpointState = Executing
executionStartedAt
checkpointVersion++
nextExpectedAction = record_execution_evidence
```

### Crash semantics

`Executing` after restart means **uncertain local execution**. Recovery must require worktree/evidence inspection and must never automatically run the PLAN again.

### Tests

- normal mark executing;
- duplicate same-owner same-iteration call is safe;
- stale iteration denied;
- second executor lease denied;
- crash/reload preserves Executing state;
- no command text is persisted automatically.

## Expected file placement

Use capability-first placement, for example:

```text
C2C.Core/C2C/Protocol/
C2C.Core/C2C/Sessions/
C2C.Infrastructure/C2C/Persistence/
C2C.Infrastructure/C2C/Leasing/
C2C.Core.Tests/C2C/
C2C.IntegrationTests/C2C/
```

Do not implement browser/ChatGPT transport in this wave.
