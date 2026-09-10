# V0.7 State Machine and Transition Contract

## Separate protocol state from local checkpoint state

The protocol states and persisted local checkpoint states are related but not identical.

Protocol-visible states:

```text
INIT
PLAN
EXECUTED
DONE
BLOCKED
ERROR
HANDOFF
```

`EXECUTING` is an optional local checkpoint/progress concept. `REVIEW` is logical planner activity and does not need to be serialized as a mandatory wire state. There is no `RESUME` protocol state.

Recommended local checkpoints:

```text
Created
InitReady
InitSent
PlanReceived
Executing
ExecutionAttached
ExecutedReady
ExecutedSent
WaitingForReview
Done
Blocked
Error
HandoffReady
```

Exact enum names may follow repository naming conventions, but semantics must remain explicit.

## Canonical happy path

```text
Create task
  -> INIT iteration 0
  -> accept PLAN iteration 1
  -> mark EXECUTING iteration 1
  -> attach execution evidence iteration 1
  -> render/send EXECUTED iteration 1
  -> planner reviews via MCP
       -> PLAN iteration 2 -> ...
       -> DONE iteration 1 or current reviewed iteration
       -> BLOCKED
       -> ERROR
```

## Transition table

| Current checkpoint | Accepted event/message | Result | Notes |
|---|---|---|---|
| none | Create task | InitReady | generates stable task id |
| InitReady/InitSent | matching INIT replay | unchanged | identical duplicate only |
| InitSent | PLAN(n+1) | PlanReceived | first executable iteration is 1 |
| PlanReceived | mark executing | Executing | local only |
| Executing/PlanReceived | attach execution | ExecutionAttached | evidence must match task/iteration/workspace |
| ExecutionAttached/ExecutedReady | render/replay EXECUTED | ExecutedReady/Sent | same evidence is idempotent |
| ExecutedSent/WaitingForReview | PLAN(next) | PlanReceived | REPLAN path, monotonic iteration |
| ExecutedSent/WaitingForReview | DONE(current) | Done | requires reviewed evidence |
| active nonterminal | BLOCKED | Blocked | safe reason + next action |
| active nonterminal | ERROR | Error | protocol/runtime fault only |
| recoverable Blocked/Error | valid recovery event | derived checkpoint | explicit, never guessed |
| any recoverable nonterminal | build HANDOFF | HandoffReady | bounded continuation brief |
| Done | duplicate matching DONE | Done | idempotent |
| Done | any mutating different transition | reject | terminal immutable |

## Iteration semantics

- INIT uses iteration `0`.
- First accepted PLAN uses iteration `1`.
- Execution and EXECUTED reference the same PLAN iteration.
- A REPLAN increments iteration exactly once.
- Re-rendering/re-sending the same state does not increment iteration.
- Resume/reload does not increment iteration.
- HANDOFF does not invent a new iteration.

## Duplicate semantics

For each state-changing message persist a canonical content digest.

```text
same task + same iteration + same state + same canonical digest
  -> idempotent replay

same task + same iteration + same state + different digest
  -> C2C_PROTOCOL_CONFLICT
```

Canonicalization must be deterministic and versioned. Do not hash raw platform-dependent line endings if the parser normalizes them differently.

## Version rules

V1 parser rules:

- hard message size cap: 4 KB;
- recommended rendered size: <= 1 KB;
- `VERSION`, `STATE`, `TASK_ID`, `ITERATION` required where applicable;
- incompatible major version fails with `PROTOCOL_VERSION_UNSUPPORTED`;
- unknown required fields fail;
- safe optional extension fields may be preserved/ignored only when the declared version permits it.

## Validation order

```text
raw text size
 -> lexical parse
 -> protocol version
 -> required headers
 -> state-specific schema
 -> task/workspace binding
 -> iteration relation
 -> current-state transition
 -> evidence/precondition checks
 -> persist atomically
```

Do not persist a partially validated transition.

## Terminal behavior

`DONE` is terminal. `BLOCKED` and `ERROR` may be recoverable depending on reason classification, but recovery must be explicit and must derive the next action from the persisted checkpoint. A stale planner response must never revive a completed task.
