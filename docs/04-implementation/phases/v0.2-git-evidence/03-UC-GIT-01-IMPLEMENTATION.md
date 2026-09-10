# UC-GIT-01 — Implementation Plan

## Goal

Implement `git_status` as secure, bounded, machine-readable Git metadata for visible workspace paths only.

## Read before coding

1. `AGENTS.md`
2. `.agent/rules/00-GLOBAL.md`
3. `.agent/rules/02-USE-CASE-IMPLEMENTATION.md`
4. `.agent/rules/03-DOTNET.md`
5. `.agent/rules/04-SECURITY.md`
6. `.agent/rules/05-TESTING.md`
7. `skills/dotnet-project-conventions/SKILL.md`
8. `docs/03-use-cases/git/UC-GIT-01-READ-GIT-STATUS.md`
9. every referenced `BR-*`
10. `02-DETAILED-DESIGN.md` in this phase pack
11. nearest existing workspace source/tests

## Scope

In scope:

- Git process abstraction
- repository availability/discovery
- NUL-delimited porcelain status parsing
- workspace visibility filtering
- rename/copy filtering
- stable ordering
- bounded pagination
- stable errors
- MCP `git_status` adapter
- unit/integration/security tests

Out of scope:

- diff bodies
- commit history
- Git mutation
- shell execution
- refactor of existing workspace MCP tools
- V0.3 execution evidence

## Recommended files

Use the project-conventions skill and nearest structure. A likely bounded change is:

```text
src/C2C.Core/Git/
├── IGitReader.cs
├── IGitProcess.cs
├── GitStatusEntry.cs
├── GitProcessRequest.cs
├── GitProcessResult.cs
└── GitErrorCodes.cs              # only if errors are capability-specific

src/C2C.Infrastructure/Git/
├── GitProcess.cs
├── GitReader.cs
└── GitStatusParser.cs

tests/C2C.Core.Tests/Git/
├── GitStatusParserTests.cs
└── GitStatusFilteringTests.cs

tests/C2C.IntegrationTests/Git/
└── GitStatusIntegrationTests.cs

src/C2C.Host/Mcp/Git/
└── GitStatusMcpAdapter.cs        # optional extraction if it improves cohesion
```

Do not create global `Services/`, `Models/`, `Helpers/`, or `Interfaces/` folders.

## Implementation order

### Slice 1 — Contracts

Define only contracts needed by this UC.

Acceptance:

```text
[ ] Core has no Process/MCP/ASP.NET dependency
[ ] status result contains only planner-relevant metadata
[ ] no absolute path appears in public DTO
```

### Slice 2 — Git process adapter

Implement owned process execution using argument lists, cancellation, timeout, bounded stdout/stderr capture, and stable exit classification.

Acceptance:

```text
[ ] no shell
[ ] no concatenated user command
[ ] cancellation terminates owned process safely
[ ] stderr is not exposed remotely
```

### Slice 3 — Porcelain parser

Parse NUL-delimited status output. Cover:

- modified
- added
- deleted
- untracked
- staged/worktree combinations
- rename/copy shape required by chosen porcelain format
- spaces/tabs/newlines in filenames where platform supports them
- malformed data

The parser must not log malformed raw entries.

### Slice 4 — Visibility gate

For every candidate path:

```text
Git path
  ↓
canonical mapping
  ↓
IWorkspaceAccessPolicy
  ↓
allow / deny
```

For rename/copy:

```text
source allowed && destination allowed
```

must be true before any rename metadata is returned.

### Slice 5 — Paging

Apply stable ordering after filtering. Reuse `PageRequest` / `PageResult` conventions if compatible.

The page limit is server-controlled and capped.

### Slice 6 — MCP adapter

Expose `git_status` only after service tests pass.

The adapter should:

- parse schema inputs;
- resolve `IGitReader` and workspace context;
- map `OperationResult` to MCP response;
- contain no Git parsing or security policy logic.

### Slice 7 — Integration verification

Use temporary Git repositories. Do not depend on the developer's real repository state.

## Required tests

Minimum focused matrix:

```text
clean repository                          -> empty success
modified allowed file                     -> returned
staged allowed file                       -> returned
untracked allowed file                    -> returned
removed allowed file                      -> returned
rename allowed -> allowed                 -> returned
modified .env                             -> omitted
modified private key path                 -> omitted
modified .c2cignore denied path           -> omitted
rename allowed -> denied                  -> omitted/fail closed
rename denied -> allowed                  -> omitted/fail closed
workspace is subdirectory of larger repo  -> sibling changes omitted
not a Git repository                      -> GIT_NOT_AVAILABLE
malformed parser input                    -> stable safe error
process timeout                            -> C2C_TIMEOUT
cancellation                              -> canceled without orphan process
large changed-file set                    -> bounded page
```

## Completion report format

```text
STATE: EXECUTED
USE_CASE: UC-GIT-01

FILES_CHANGED:
- ...

CONTRACTS:
- ...

COMMANDS_RUN:
- dotnet build C2CNet.sln -> PASS/FAIL
- focused tests -> PASS/FAIL
- dotnet test C2CNet.sln -> PASS/FAIL

BUSINESS_RULES:
- BR-... -> satisfied/not satisfied

SECURITY_CHECKS:
- denied filenames absent
- no shell
- no Git mutation
- workspace boundary preserved

ACCEPTANCE_CRITERIA:
- [x]/[ ] ...

DEVIATIONS:
- none / ...

REMAINING:
- UC-GIT-02 only
```

Do not start UC-GIT-02 in the same task unless explicitly approved.