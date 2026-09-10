# 11 — Data Model

V1 uses local filesystem app-state, not a database.

## App-state layout

Conceptual cross-platform root is resolved through an `IAppStatePathProvider`.

```text
C2C.NET/
  config.json
  runtime/
    bridge-<workspace>.json
    admin-token-<workspace>   # restricted permissions
  workspaces/
    <workspace-id>/
      workspace.json
      auth.json               # no raw long-lived bearer token in plain JSON
      session.json
      tunnel.json
  executions/
    <workspace-id>/
      <task-id>/
        <execution-id>.json
        artifacts/
  logs/
```

## WorkspaceIdentity

```text
workspaceId          stable random identifier
rootFingerprint      salted/hashed canonical root identity for diagnostics
createdAt
schemaVersion
```

Do not derive an externally visible id directly from the absolute path without salt; that can leak path information.

## C2CSession

```text
taskId
goal
constraints
protocolState
checkpointState
iteration
waitingFor
lastPlanDigest
lastExecutionId
knownIssues[]
nextExpectedStep
conversationReference?  # opaque/local only
executorLease?
schemaVersion
updatedAt
```

All free-text fields have explicit length caps.

## ExecutionRecord

```text
executionId
taskId
iteration
idempotencyKey
startedAt
finishedAt
exitStatus
commandCategory
changedFiles[]
testSummary
artifactRefs[]
createdAt
schemaVersion
```

## Token/pairing store

Security-sensitive state uses a dedicated persistence abstraction. Raw secrets should go to protected storage where possible. Hash-indexed opaque tokens and verifier metadata are separated from ordinary config.

## Atomic writes

Local state updates use temp-file + flush + atomic replace/rename semantics where supported. Include schema version and reject partial/corrupt JSON rather than silently resetting security state.
