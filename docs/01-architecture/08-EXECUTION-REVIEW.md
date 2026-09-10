# 08 — Execution Evidence and Independent Review

## Principle

The planning/review model does not trust an execution agent's assertion that code or tests are correct. It independently inspects actual workspace evidence through MCP.

## Execution record

A record is metadata, not a raw log dump.

```text
ExecutionRecord
- executionId
- taskId
- iteration
- executorName/version (optional)
- startedAt / finishedAt
- exitStatus
- commandCategory
- changedFiles[]
- testSummary
- artifactRefs[]
- idempotencyKey
- schemaVersion
```

Store timestamps as `DateTimeOffset` UTC values; use `TimeProvider` in testable services.

## `c2c record`

Example conceptual flow:

```text
Agent executes dotnet test
  -> writes local log
  -> c2c record --task ... --iteration ... --output-file ...
  -> runtime validates workspace/task/lease
  -> creates execution record
  -> sanitizer classifies artifact
  -> returns execution id
```

## Artifact classification

- `readable`: body can be requested after sanitization.
- `restricted`: metadata visible, body unavailable.
- `missing`: referenced file unavailable.
- `expired`: retention removed body; metadata may remain.

## Sanitization

Perform sanitization locally and deterministically. Never ask the remote model to “ignore secrets.” The sanitizer is the boundary.

Required checks include bearer/API-token patterns, pairing-code patterns, local absolute home paths, private-key blocks, excessive size/line count and invalid/binary content.

## Review sequence

Recommended planner review:

1. `execution_summary(executionId)`
2. `git_status`
3. `git_diff` on relevant allowed changes
4. `test_status(taskId, iteration)`
5. `execution_output(list)` then read selected safe output only if needed
6. return `DONE`, another finite `PLAN`, or `BLOCKED`

## Retention

V1 default: retain record metadata longer than artifact bodies. Example configurable policy: metadata 30 days, sanitized output 7 days. Limits must be storage-bounded and user-configurable.
