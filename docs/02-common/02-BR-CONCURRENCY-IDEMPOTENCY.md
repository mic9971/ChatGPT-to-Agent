# Common Concurrency and Idempotency Rules

| ID | Rule |
|---|---|
| BR-CON-001 | Execution record identity is `(workspace_id, task_id, iteration, record_key)`; repeating the same finalized key returns the same logical record instead of duplicating it. |
| BR-CON-002 | Persisted JSON/checkpoint writes are atomic (`write temp -> fsync/flush where practical -> replace`). Partial files are never treated as valid state. |
| BR-CON-003 | One runtime owner per workspace is enforced with an OS-appropriate lock/lease; stale-owner recovery must be explicit. |
| BR-CON-004 | Tunnel start/stop/restart is serialized per workspace runtime. Concurrent `ensure` calls converge to one live tunnel. |
| BR-CON-005 | Refresh-token rotation is single-use; replay of an already consumed refresh token is rejected and audited. |
| BR-CON-006 | Session/checkpoint updates use a monotonic version/iteration check. Older iterations cannot overwrite newer state. |
| BR-CON-007 | Read-only MCP operations may run concurrently but shared caches/state must be immutable or thread-safe. |
| BR-CON-008 | Process shutdown is cooperative first, then bounded forced termination only for processes owned by this runtime. |
