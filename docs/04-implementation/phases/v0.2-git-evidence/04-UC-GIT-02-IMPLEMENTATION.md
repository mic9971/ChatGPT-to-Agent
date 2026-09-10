# UC-GIT-02 — Implementation Plan

## Goal

Implement `git_diff` so the planner can inspect bounded patch evidence for allowed workspace files without ever retrieving denied patch bodies.

## Dependency

`UC-GIT-01` must already be complete and its Git process/status candidate path logic should be reused rather than duplicated.

## Read before coding

- `AGENTS.md`
- applicable `.agent/rules/*`
- `skills/dotnet-project-conventions/SKILL.md`
- `docs/03-use-cases/git/UC-GIT-02-READ-GIT-DIFF.md`
- every referenced `BR-*`
- phase `02-DETAILED-DESIGN.md`
- UC-GIT-01 implementation and tests

## Critical invariant

The implementation is rejected if it does this:

```text
git diff --all-relevant-files
        ↓
capture patch including .env/private-key body
        ↓
redact/filter afterwards
```

Required design:

```text
changed-path metadata
        ↓
workspace visibility policy
        ↓
allowed paths only
        ↓
git diff -- <allowed pathspecs>
        ↓
bounded response
```

## Scope

In scope:

- allowed changed-path candidate reuse
- optional approved base/revision handling
- explicit allowed pathspec diff retrieval
- rename security
- binary metadata handling
- byte/record bounds
- continuation metadata
- MCP `git_diff` adapter
- unit/integration/adversarial tests

Out of scope:

- history browser
- arbitrary rev expressions as command fragments
- remote branches
- patch application
- Git mutation
- diff for denied files even if user explicitly asks

## Recommended files

Extend the existing Git capability only:

```text
src/C2C.Core/Git/
├── GitDiffRequest.cs
├── GitDiffItem.cs
└── GitDiffPage.cs

src/C2C.Infrastructure/Git/
├── GitReader.cs                 # extend existing capability
└── GitDiffParser.cs             # only if a parser is actually needed

tests/C2C.Core.Tests/Git/
├── GitDiffFilteringTests.cs
└── GitDiffPagingTests.cs

tests/C2C.IntegrationTests/Git/
└── GitDiffIntegrationTests.cs

src/C2C.Host/Mcp/Git/
└── GitDiffMcpAdapter.cs         # if Git MCP adapters are extracted
```

Do not create a second Git process abstraction or second workspace security policy.

## Implementation order

### Slice 1 — Diff request/result contract

Keep the request bounded. The optional `base` value is data, never command text.

Internal code should normalize revision input before process invocation.

Initial support should remain minimal: the working-tree default and only explicitly approved revision values. Unsupported values fail with a stable invalid-argument error.

### Slice 2 — Candidate discovery

Reuse status/machine-readable metadata to obtain changed candidate paths.

Before patch retrieval:

```text
[ ] candidate inside configured workspace
[ ] candidate passes sensitive-path policy
[ ] candidate not denied by .c2cignore
[ ] for rename, source passes policy
[ ] for rename, destination passes policy
```

If any required visibility check fails, the path is excluded before diff invocation.

### Slice 3 — Allowed-only patch retrieval

Invoke Git with explicit pathspecs containing only the allowed paths.

Do not use a shell.

Avoid command-line length problems by batching allowed paths when necessary. Each batch remains bounded.

### Slice 4 — Patch bounds

Apply:

- hard total response bytes;
- server maximum file count;
- per-item byte cap if required;
- deterministic ordering;
- truncation/continuation metadata.

Do not buffer an unbounded child-process stream before applying limits.

### Slice 5 — Binary handling

Do not request binary patch payloads.

For an allowed binary change, expose only the approved metadata and `isBinary=true`/equivalent. The planner does not need binary bytes.

### Slice 6 — Race/revalidation

A file may change between candidate discovery and diff retrieval.

If the selected path becomes invalid/denied/unavailable, skip or fail closed according to the shared error policy. Never broaden the command to recover convenience.

### Slice 7 — MCP adapter

Expose `git_diff` only after Core/Infrastructure tests pass. The adapter performs transport mapping only.

## Required adversarial tests

The following are blocking:

```text
normal.cs modified + .env modified
  -> normal patch returned
  -> .env filename/body absent everywhere in MCP result

normal.cs modified + id_rsa modified
  -> normal patch returned
  -> key path/body absent

allowed file renamed to .env
  -> rename/diff omitted

.env renamed to allowed name
  -> rename/diff omitted

workspace subdirectory + sibling repository secret modified
  -> sibling path/body absent

huge allowed patch
  -> bounded/truncated result

binary file
  -> no binary payload

path with spaces
  -> safe pathspec handling

Git timeout/cancellation
  -> stable safe outcome
```

Use canary secret strings in tests and assert they never appear in returned body, errors, or captured structured logs where testable.

## Completion report format

```text
STATE: EXECUTED
USE_CASE: UC-GIT-02

FILES_CHANGED:
- ...

TWO_STAGE_DIFF:
- candidate discovery: ...
- visibility gate: ...
- allowed-only retrieval: ...

COMMANDS_RUN:
- ...

SECURITY_CANARIES:
- .env secret absent: PASS/FAIL
- private-key secret absent: PASS/FAIL
- denied rename metadata absent: PASS/FAIL

BOUNDS:
- max items: ...
- max bytes: ...
- continuation strategy: ...

ACCEPTANCE_CRITERIA:
- [x]/[ ] ...

DEVIATIONS:
- none / ...
```

Do not start V0.3 in the same task.