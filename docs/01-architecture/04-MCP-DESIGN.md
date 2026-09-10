# 04 — MCP Design

## Baseline

Target **MCP 2026-07-28** and the official C# SDK. This matters because the core is now stateless; new implementations should not build around the old initialize/session-id model or legacy HTTP+SSE assumptions.

Use `ModelContextProtocol.AspNetCore` for HTTP-based MCP hosting unless a concrete interoperability test demonstrates a reason not to.

## Endpoint

```text
POST /mcp
```

The host should accept the protocol/version headers required by the current SDK/spec. Tool logic must remain transport-independent.

## Tool contracts

### `workspace_info`
Returns safe workspace metadata: stable/salted workspace id, repository presence, current branch if safe, tool capabilities, limits. Do not return home directory or raw secrets.

### `list_directory`
Input: relative path, page/cursor, max items.  
Output: filtered entries only. Sensitive/ignored entries are not leaked as names.

### `read_file`
Input: relative path plus bounded start line/offset and max lines/bytes.  
Output: text chunk, continuation cursor, encoding metadata. Reject binary/oversized/denied paths unless a future explicit safe-binary mode exists.

### `search_workspace`
Input: query, optional safe path scope, match cap.  
Output: path + line/range + bounded snippet. Every result path must pass the same policy as `read_file`.

### `git_status`
Return bounded machine-readable status for allowed paths only.

### `git_diff`
Never run a broad diff and blindly stream it. First determine changed paths, filter denied paths, then request diff bodies only for allowed paths. Paginate by byte/record cursor with a hard total cap.

### `test_status`
Return normalized test summaries attached to execution records; do not infer success from text alone when structured metadata exists.

### `execution_summary`
Return bounded metadata for an execution id/iteration.

### `execution_output`
Two-step design: list available sanitized/restricted artifacts, then read one allowed artifact by id. Restricted artifacts expose metadata/status only, never body.

## Error model

Tool errors are stable and typed, for example:

- `WORKSPACE_PATH_DENIED`
- `WORKSPACE_PATH_OUTSIDE_ROOT`
- `WORKSPACE_ITEM_NOT_FOUND`
- `SENSITIVE_CONTENT_DENIED`
- `OUTPUT_LIMIT_EXCEEDED`
- `GIT_NOT_AVAILABLE`
- `EXECUTION_NOT_FOUND`
- `INSUFFICIENT_SCOPE`
- `MCP_PROTOCOL_UNSUPPORTED`

Do not expose stack traces or local absolute paths to remote callers.

## Pagination / limits

All unbounded results require pagination. Defaults should be conservative; limits are server-enforced, not client-trusted. Cursors should be opaque and integrity-protected or derived deterministically from request + offset.

## Caching

MCP 2026-07-28 allows cache hints on list/read results. Use short TTL for tool catalog/capability results. Workspace data should use conservative/no caching unless a safe content version (Git index/worktree generation or file metadata fingerprint) is part of the cache key.

## No MCP task/session coupling

The C2C task id is not an MCP session. The bridge must be able to answer an MCP tool request without a prior initialize handshake or sticky server instance.
