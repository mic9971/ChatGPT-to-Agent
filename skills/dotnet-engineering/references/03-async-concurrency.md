# Async and Concurrency Reference

- Async I/O from boundary to boundary; no sync-over-async.
- Propagate cancellation; distinguish cancellation from fault.
- External process/network operations require explicit timeout/cancellation policy.
- Use `SemaphoreSlim`, channels, locks or immutable snapshots based on the actual coordination need; do not invent lock-free code casually.
- A DI singleton is not automatically thread-safe.
- Prefer per-task immutable state plus atomic persistence over shared mutable dictionaries when durability matters.
- Idempotency keys are required for retried state-changing local commands such as execution record creation.
- Avoid fire-and-forget Tasks unless owned by a hosted/background service with observable failure and shutdown semantics.
- Use `Task.WhenAll` only for independent work and understand failure/cancellation aggregation.
