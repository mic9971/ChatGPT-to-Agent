# Async / Concurrency Placement Rules

Language/runtime details live in `dotnet-engineering`; this file covers ownership/structure.

## Async follows I/O boundaries

Filesystem, process, network, persistence, and long-running adapter operations accept and propagate `CancellationToken` when they can block.

Do not create `AsyncHelpers`, `TaskUtils`, or a generic concurrency manager. Concurrency policy belongs to the capability that owns the resource/state.

Examples:

```text
Execution/ExecutionLeaseService
Tunnel/TunnelProcessOwner
Workspace/WorkspaceConfigStore
```

## Idempotency ownership

Idempotency keys, leases, locks, and atomic-write semantics live with the operation/state they protect, not in a generic global utility package.

## Shared synchronization

A reusable lock primitive may be shared only if it has no capability semantics and its behavior is stable/tested. Otherwise keep synchronization local to the owning capability.

## Time

Time-dependent state machines use injected `TimeProvider`. Expiry/lease/retry policy stays next to the capability policy rather than in a global scheduler helper.