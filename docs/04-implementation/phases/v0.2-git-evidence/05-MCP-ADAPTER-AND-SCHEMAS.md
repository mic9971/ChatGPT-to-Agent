# V0.2 Git MCP Adapter and Schemas

## Purpose

Expose Git evidence through MCP without moving Git logic into the transport layer.

The Host adapter is intentionally thin:

```text
MCP input
  ↓
validate/normalize transport fields
  ↓
IGitReader
  ↓
OperationResult
  ↓
MCP output
```

No Git process construction, porcelain parsing, path visibility decisions, diff filtering, or secret redaction belongs in the MCP handler.

## Tool set after V0.2

```text
workspace_info
read_file
list_directory
search_workspace
git_status
git_diff
```

V0.3 later adds execution/test evidence tools.

## `git_status`

Recommended input shape:

```json
{
  "cursor": "optional opaque cursor",
  "limit": 100
}
```

Do not expose a raw command, arbitrary Git flags, cwd, repository path, or shell fragment.

Recommended response semantics:

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "path": "src/Foo.cs",
      "indexStatus": "M",
      "worktreeStatus": " ",
      "isRename": false,
      "originalPath": null
    }
  ],
  "nextCursor": null
}
```

Exact field names should follow existing repository serialization conventions. Absolute local paths must never be returned.

## `git_diff`

Recommended input shape derived from the existing UC contract:

```json
{
  "base": "optional approved revision value",
  "cursor": "optional opaque cursor",
  "limit": 50
}
```

`base` is not a command fragment. The adapter passes it as data to application logic, where it is validated/normalized.

Recommended response semantics:

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "path": "src/Foo.cs",
      "changeKind": "modified",
      "isBinary": false,
      "patch": "...bounded text patch...",
      "truncated": false,
      "byteCount": 1234
    }
  ],
  "nextCursor": null,
  "truncated": false
}
```

The adapter must never fabricate a patch for a denied path.

## Registration structure

The current repository centralizes MCP registration in `McpServerConfigurator.cs`.

For this phase, either of these is acceptable:

```text
Option A
McpServerConfigurator registers schemas/handlers directly

Option B
McpServerConfigurator delegates new Git-specific adapter code to:
src/C2C.Host/Mcp/Git/
```

Prefer Option B if adding Git makes the existing configurator meaningfully harder to navigate. However, do **not** refactor the existing Workspace MCP tools just to make the folder tree symmetrical.

A bounded extraction may look like:

```text
src/C2C.Host/Mcp/
├── McpServerConfigurator.cs
└── Git/
    ├── GitMcpSchemas.cs
    └── GitMcpHandlers.cs
```

Transport-specific types stay in Host.

## Error mapping

The MCP layer maps stable application errors; it does not pass through raw Git stderr or exception messages.

Examples:

```text
GIT_NOT_AVAILABLE       -> MCP error result with stable code/message
C2C_TIMEOUT             -> MCP error result with stable code/message
OUTPUT_LIMIT_EXCEEDED   -> bounded error/result according to current convention
INVALID_ARGUMENT        -> invalid base/cursor/limit
```

No response should include:

- absolute working directory;
- Git executable path;
- raw process command line;
- raw stderr containing local paths;
- denied filename;
- denied patch body.

## Pagination

Use the existing `PageRequest` / `PageResult` convention when practical.

Cursor requirements:

- opaque to the client;
- bounded length;
- tied to the effective query shape;
- contains no absolute paths or raw secret names;
- invalid/stale cursor returns a stable error rather than guessing.

## Capability advertisement

`workspace_info` or any capability list should only advertise `git_status`/`git_diff` once their adapters are actually registered and usable.

Do not advertise future V0.3 tools early.

## MCP integration tests

At minimum test through the real MCP Host boundary:

```text
list tools contains git_status and git_diff
unknown Git-like tool rejected
git_status returns allowed status
git_status omits denied path
git_diff returns allowed patch
git_diff response contains no denied canary
invalid base rejected safely
large diff remains bounded
```

The security gate is not satisfied by Core unit tests alone.