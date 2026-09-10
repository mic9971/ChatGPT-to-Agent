# V0.7 Persistence, Executor Lease and Idempotency

## Session record

Recommended persisted shape:

```text
schemaVersion
workspaceId
taskId
goal
constraints[]
protocolState
checkpointState
iteration
checkpointVersion
waitingFor
lastPlanDigest
lastPlanSummary
lastExecutionId
lastReviewedExecutionId
successCriteria[]
knownIssues[]
nextExpectedAction
conversationReference?
terminalSummary?
lastMessageDigests{}
createdAt
updatedAt
```

All free-text fields and collections have explicit caps.

## Atomic persistence

Reuse the repository's established app-state write discipline:

```text
serialize validated record
 -> temp file in same filesystem
 -> flush/fsync where practical
 -> atomic replace/rename
 -> optional previous-version backup according to existing convention
```

Never silently reset corrupt security/session state. A corrupt checkpoint is a recovery error and should remain available for local diagnostics.

## Optimistic concurrency

Every mutation includes an expected `checkpointVersion` (or equivalent monotonic token).

```text
loaded version = 12
writer expects = 12
persist new version = 13
```

If another writer already committed version 13, the stale writer fails with a conflict instead of overwriting newer state.

This is in addition to iteration monotonicity; version and iteration solve different problems.

## Executor lease

V1 permits one active executor to advance one task at a time.

Recommended lease shape:

```text
workspaceId
taskId
ownerId
leaseId
acquiredAt
heartbeatAt
expiresAt
process/runtime identity when useful
```

Rules:

- readers do not need the executor lease;
- state-changing execution-agent actions require a valid lease;
- lease acquisition is atomic;
- the same owner may renew safely;
- another owner cannot advance until expiry or explicit takeover policy;
- expiry is based on `TimeProvider`/monotonic-safe logic where possible;
- stale lease recovery must not replay execution automatically.

Do not couple the lease to ChatGPT/browser identity. It represents the local executor authority for a task.

## Crash scenarios

### Crash after PLAN accepted, before execution

Resume returns `nextExpectedAction = begin_execution`.

### Crash while EXECUTING

Resume returns an uncertain state such as `inspect_execution_state` and requires worktree/evidence inspection. It must not blindly rerun a command.

### Crash after execution record persisted, before session attach

Recovery may locate the explicitly known/finalized execution id only through deterministic caller context or a safe reconciliation operation. Do not scan and guess an arbitrary latest record.

### Crash after session attach, before EXECUTED delivered

Re-render the same EXECUTED message from persisted state. No new execution record is created.

### Crash after DONE persisted

Reload returns terminal DONE. No planner message can reopen it.

## Idempotency keys

Recommended identities:

```text
task create:
(workspaceId, requestKey)

protocol transition:
(taskId, iteration, state, canonicalMessageDigest)

execution attach:
(taskId, iteration, executionId)

CLI mutation retry:
command-specific caller request id when needed
```

## Session history

V1 does not need an event-sourcing subsystem. Persist the current authoritative checkpoint plus only bounded metadata/digests required for duplicate/conflict detection.

If transition history is retained for diagnostics, it must be bounded and contain no source/diff/log/token bodies.

## Retention

Terminal sessions may be retained under an explicit local cleanup policy. Cleanup must never delete execution evidence still referenced by an active or retained session unless the evidence retention contract explicitly permits it.

## Error codes

Use/extend stable common codes rather than free-text branching. Expected examples:

```text
PROTOCOL_INVALID_STATE
PROTOCOL_VERSION_UNSUPPORTED
C2C_PROTOCOL_CONFLICT
C2C_NOT_READY
C2C_CONFLICT
EXECUTION_NOT_FOUND
OUTPUT_LIMIT_EXCEEDED
```

Add a new code only when callers need a distinct machine decision.
