# V0.8A — UC-CTRL-05..08 Implementation Plan

## Scope

This slice completes reusable Control Plane Automation:

- `UC-CTRL-05` Run C2C Iteration Loop
- `UC-CTRL-06` Resume Existing Conversation
- `UC-CTRL-07` Handoff to Replacement Conversation
- `UC-CTRL-08` Recover Control Driver Failure

It must use the already-proven UC-CTRL-01..04 primitives and V0.7 session services.

## UC-CTRL-05 — Run C2C Iteration Loop

The coordinator controls state progression; the execution agent owns code execution.

```text
ensure READY
 -> start/load control session
 -> prepare/send INIT
 -> receive/accept PLAN
 -> agent executes locally
 -> c2c record
 -> session attaches execution
 -> prepare/send EXECUTED
 -> planner reads MCP evidence
 -> receive PLAN | DONE | BLOCKED | ERROR
 -> repeat only on valid PLAN
```

### Rules

- loop has a configurable maximum iteration/elapsed-time guard;
- coordinator never edits files or runs PLAN commands itself;
- no EXECUTED before immutable evidence exists;
- no local DONE because the agent claims success;
- each planner response is accepted through V0.7 validation;
- cancellation preserves the last accepted checkpoint and delivery journal.

### Tests

- one iteration to DONE;
- two iterations with REPLAN;
- evidence persistence failure prevents EXECUTED;
- planner BLOCKED;
- protocol ERROR;
- cancellation mid-loop;
- max iteration guard;
- duplicate response does not create extra iteration.

## UC-CTRL-06 — Resume Existing Conversation

### Recovery inputs

Use three independent facts:

```text
V0.7 session checkpoint
+ V0.8 conversation binding/delivery journal
+ observed latest browser conversation state
```

The browser observation cannot override the persisted protocol checkpoint without parser/validator acceptance.

### Deterministic recovery examples

```text
checkpoint InitSent
last outbound observed sent
no accepted inbound
 -> reopen same conversation, wait/read PLAN

checkpoint PlanReceived
 -> do not resend INIT; execute or continue deterministic next action

checkpoint Executing after crash
 -> inspect worktree/evidence per V0.7 recovery; never blindly rerun PLAN

execution attached but EXECUTED delivery ambiguous
 -> inspect conversation before resend

checkpoint Done
 -> terminal; do not reopen execution
```

### Tests

- restart after INIT send;
- restart after PLAN acceptance;
- restart during EXECUTING;
- restart after evidence but before EXECUTED send;
- restart after ambiguous EXECUTED send;
- restart after DONE.

## UC-CTRL-07 — Handoff to Replacement Conversation

### Required flow

```text
old conversation unavailable/explicitly replaced
 -> request bounded HANDOFF from V0.7
 -> retire old binding
 -> create/open new conversation externally
 -> bind new reference to same workspace/task
 -> send HANDOFF
 -> new planner re-reads evidence through MCP
 -> accept next valid response
```

### Invariants

- same `workspace_id + task_id` continues;
- old conversation reference is never reused for another task;
- HANDOFF contains no transcript/source/diff/raw logs;
- iteration is not incremented solely because a conversation changed;
- if new conversation cannot use the MCP app, stop with setup/auth action required rather than pasting evidence into chat.

### Tests

- successful handoff;
- missing old conversation;
- cross-task bind denied;
- oversized handoff denied;
- MCP unavailable in replacement conversation -> blocked/action required.

## UC-CTRL-08 — Recover Driver Failure

Normalize failures:

```text
CTRL_USER_AUTH_REQUIRED
CTRL_CONVERSATION_UNAVAILABLE
CTRL_COMPOSER_UNAVAILABLE
CTRL_SEND_AMBIGUOUS
CTRL_RESPONSE_TIMEOUT
CTRL_NAVIGATION_FAILED
CTRL_DRIVER_UNAVAILABLE
CTRL_MANUAL_ACTION_REQUIRED
```

### Recovery policy

| Failure | Automatic action |
|---|---|
| user auth required | none; pause for user |
| composer unavailable | bounded semantic retry |
| navigation failed | reopen same reference, bounded retry |
| send ambiguous | inspect before resend |
| response timeout | inspect latest conversation state |
| driver unavailable | switch to manual transport |
| conversation unavailable | HANDOFF |

Retries use bounded exponential/backoff or repository-standard retry helper only when it cannot duplicate a state-changing action.

## Loop budget

Recommended configurable guardrails:

```text
max browser navigation retry: 2-3
max send retry after proven absence: 1-2
max response wait: bounded, cancellable
max automatic PLAN iterations per invocation: small finite number
```

Do not hard-code product policy values before observing V0.8 smoke behavior; expose safe defaults/options where justified.

## Phase A completion

V0.8A is complete only when the deterministic fake/manual path proves:

```text
INIT -> PLAN -> EXECUTED -> DONE
INIT -> PLAN -> EXECUTED -> PLAN -> EXECUTED -> DONE
login required -> safe pause
ambiguous send -> reconciliation
missing conversation -> HANDOFF
transport unavailable -> manual fallback
```

Only then start V0.8B Antigravity integration.
