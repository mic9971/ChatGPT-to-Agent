# V0.2 Current Baseline

## Observed repository state

At the time this phase pack was written, the current implementation already contains:

```text
src/C2C.Core/
├── Common/
└── Workspace/

src/C2C.Infrastructure/
└── Workspace/

src/C2C.Host/Mcp/
└── McpServerConfigurator.cs
```

`McpServerConfigurator` currently registers these read-only tools:

```text
workspace_info
read_file
list_directory
search_workspace
```

The repository also contains workspace contracts such as:

- `IWorkspaceFileReader`
- `IWorkspaceDirectoryReader`
- `ISearchBackend`
- `IWorkspaceAccessPolicy`

No dedicated `Core/Git` or `Infrastructure/Git` capability is present yet. Therefore the remaining V0.2 implementation target is Git evidence, not another rewrite of workspace read/search.

## Important status rule

Documentation status fields may lag behind implementation. Antigravity must treat the source tree and executed tests as the current implementation evidence.

Do not change a UC to `DONE` merely because matching files exist. A UC is considered complete only after its acceptance criteria, referenced BRs, build, focused tests, integration tests, and relevant security tests are verified.

## Baseline verification checklist

Before Git implementation:

```text
[ ] solution builds
[ ] existing tests pass
[ ] workspace_info works
[ ] read_file works
[ ] list_directory works
[ ] search_workspace works
[ ] path traversal tests remain green
[ ] symlink escape tests remain green
[ ] sensitive path tests remain green
[ ] .c2cignore tests remain green
[ ] no MCP write/shell tool exists
```

## V0.2 split

For implementation management, treat V0.2 as two logical slices:

```text
V0.2A — Workspace discovery
  UC-WS-03 List Directory
  UC-WS-05 Search Workspace
  Current source indicates this capability exists; verify, do not rewrite.

V0.2B — Git evidence
  UC-GIT-01 Git Status
  UC-GIT-02 Git Diff
  This is the next implementation target.
```

## Structural rule from project conventions

The new Git capability should be owned by `Git`, not placed into generic folders such as `Services`, `Helpers`, or `Models`.

Recommended shape:

```text
src/C2C.Core/Git/
src/C2C.Infrastructure/Git/
tests/C2C.Core.Tests/Git/
tests/C2C.IntegrationTests/Git/
```

The MCP adapter belongs to the Host transport boundary. If Git-specific MCP code is extracted from the existing configurator, place it under a Git-specific adapter folder rather than reorganizing unrelated workspace tools in the same change.

## Compatibility constraints

This phase must preserve current public behavior of the existing four MCP tools. No existing schema should be changed merely to make Git implementation aesthetically consistent.

The phase is additive:

```text
existing 4 MCP tools
        +
git_status
git_diff
        =
6 read-only tools after V0.2
```

Execution evidence tools are deferred to V0.3.