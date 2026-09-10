# Phase V0.2B — Git Evidence Completion

## Purpose

This phase completes the V0.2 **Workspace discovery + Git evidence** gate after the current workspace read/list/search foundation is in place.

The repository already exposes the read-only MCP tools `workspace_info`, `read_file`, `list_directory`, and `search_workspace`. The remaining V0.2 work is to add secure, bounded Git evidence through:

- `UC-GIT-01` — Read Git Status
- `UC-GIT-02` — Read Git Diff
- MCP tools `git_status` and `git_diff`

This phase is intentionally limited to **read-only Git evidence**. It does not add commit, add, checkout, reset, clean, branch mutation, shell execution, history browsing, remote Git operations, OAuth, tunnel, execution evidence, or control-plane automation.

## Why this is a separate phase pack

Git introduces a new security boundary even though it is read-only. A naïve implementation can disclose denied filenames or secret file bodies by running a broad `git status`/`git diff` and filtering only after output has already been captured.

The phase therefore uses a strict rule:

```text
Git metadata candidate discovery
        ↓
canonical workspace visibility policy
        ↓
allowed path set
        ↓
Git body retrieval using allowed pathspecs only
        ↓
bounded response
```

For `git_diff`, the system must never retrieve a broad patch containing denied files and then redact it later.

## Entry conditions

Before implementation, Antigravity SHALL verify the current baseline rather than trusting documentation status fields:

```bash
dotnet build C2CNet.sln
dotnet test C2CNet.sln
```

It SHALL also inspect:

- `src/C2C.Core/Workspace/`
- `src/C2C.Infrastructure/Workspace/`
- `src/C2C.Host/Mcp/McpServerConfigurator.cs`
- existing workspace tests
- `UC-GIT-01`
- `UC-GIT-02`
- all referenced `BR-*`
- `skills/dotnet-project-conventions/SKILL.md`

If baseline tests are not green, stop and report the existing failure before starting Git work.

## Phase structure

Read in this order:

1. `01-CURRENT-BASELINE.md`
2. `02-DETAILED-DESIGN.md`
3. `03-UC-GIT-01-IMPLEMENTATION.md`
4. `04-UC-GIT-02-IMPLEMENTATION.md`
5. `05-MCP-ADAPTER-AND-SCHEMAS.md`
6. `06-TEST-AND-SECURITY-GATE.md`
7. `07-ANTIGRAVITY-EXECUTION-ORDER.md`
8. `08-PHASE-DONE-AND-HANDOFF.md`

## High-level target

```text
ChatGPT / MCP planner
        ↓
C2C.Host MCP adapter
        ↓
IGitReader
        ↓
Git process adapter
        ↓
Git repository
        │
        └── every candidate path
              ↓
        IWorkspaceAccessPolicy
              ↓
        visible paths only
```

## Phase gate

V0.2 is complete only when all of the following are proven:

- `git_status` returns stable machine-readable status for visible paths only;
- `git_diff` returns patch bodies only for paths authorized before diff-body retrieval;
- `.env`, private-key paths, `.c2cignore` paths, and other denied names/bodies never appear in Git responses;
- renames involving a denied source or destination are omitted/fail closed;
- outputs are bounded and cancellable;
- Git commands are executed without a shell and cannot mutate repository state;
- workspace subdirectory inside a larger Git repository remains confined to the configured C2C workspace;
- MCP remains read-only;
- full build and tests pass.

After this gate, the next phase is **V0.3 Execution Evidence**.