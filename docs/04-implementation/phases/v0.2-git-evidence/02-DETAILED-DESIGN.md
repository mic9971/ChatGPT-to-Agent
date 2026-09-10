# V0.2 Git Evidence — Detailed Design

## 1. Design goal

Provide independent, read-only Git evidence to the planner while preserving the workspace security boundary.

The design must satisfy two different disclosure levels:

```text
Git status
  -> metadata only

Git diff
  -> metadata + patch body
```

Because diff bodies can contain secrets, the visibility decision must happen **before** patch retrieval.

## 2. Component model

```text
C2C.Host
└── MCP Git adapter
      │
      ▼
C2C.Core/Git
├── IGitReader
├── IGitProcess
├── Git status/diff request/result contracts
└── parser/policy-facing abstractions
      │
      ▼
C2C.Infrastructure/Git
├── GitProcess
├── GitReader
├── GitStatusParser
└── Git repository discovery/helper internals
      │
      ├──────────────► git executable
      │
      └──────────────► IWorkspaceAccessPolicy
```

Dependency direction stays:

```text
Core <- Infrastructure
Core <- Host adapter
```

`C2C.Core` must not depend on `System.Diagnostics.Process`, ASP.NET Core, MCP SDK types, or concrete Git CLI details.

## 3. Recommended contracts

Exact names may follow the nearest code convention, but the semantic separation should remain.

### 3.1 Git reader

```csharp
public interface IGitReader
{
    Task<OperationResult<PageResult<GitStatusEntry>>> GetStatusAsync(
        IWorkspaceContext context,
        PageRequest? pageRequest,
        CancellationToken cancellationToken);

    Task<OperationResult<GitDiffPage>> GetDiffAsync(
        IWorkspaceContext context,
        GitDiffRequest request,
        CancellationToken cancellationToken);
}
```

### 3.2 Git process boundary

```csharp
public interface IGitProcess
{
    Task<OperationResult<GitProcessResult>> RunAsync(
        GitProcessRequest request,
        CancellationToken cancellationToken);
}
```

`GitProcessRequest` must carry an executable working directory and a typed/list-based argument collection. It must not carry a pre-concatenated shell command.

### 3.3 Status model

A status entry should carry only normalized, workspace-relative metadata needed by the planner, for example:

```text
path
original_path?       # rename/copy only and only if also visible
index_status
worktree_status
is_rename
```

Do not return repository-root absolute paths.

### 3.4 Diff model

A diff item should contain bounded planner evidence, for example:

```text
path
change_kind
is_binary
patch?               # null for unsupported/binary/omitted body cases
truncated
byte_count
```

The phase should reuse existing versioning/pagination conventions rather than inventing a second cursor system.

## 4. Git invocation rules

### 4.1 No shell

Use `ProcessStartInfo.ArgumentList` or equivalent typed arguments.

Forbidden:

```text
/bin/sh -c "git ..."
cmd.exe /c "git ..."
string concatenation of user input into a shell command
```

### 4.2 Timeouts and cancellation

All Git process calls must:

- accept `CancellationToken`;
- have an explicit timeout;
- terminate only the process instance owned by the current operation;
- bound stdout/stderr capture;
- never log raw diff output.

### 4.3 Repository discovery

A C2C workspace may be a subdirectory of a larger Git repository.

Git repository discovery may find an ancestor Git root, but that must **not** expand the C2C workspace boundary.

Conceptually:

```text
GitRoot
└── repo/
    ├── backend/       <- C2C workspace
    ├── frontend/
    └── .env
```

Even if Git sees the whole repository, C2C may expose only files that resolve inside the configured workspace and pass the shared workspace policy.

The Git root itself must never be returned remotely as an absolute path.

## 5. UC-GIT-01 status flow

Use NUL-delimited porcelain output so filenames containing spaces/tabs/newlines do not rely on line parsing.

```text
git status metadata
      ↓
parse NUL-delimited entries
      ↓
for each candidate path
      ↓
map candidate to canonical filesystem target
      ↓
IWorkspaceAccessPolicy
      ↓
allowed?
  ├── no -> discard before DTO/log
  └── yes -> normalize workspace-relative status entry
      ↓
stable sort
      ↓
server-side page bound
```

For a rename/copy entry, both old and new path are security-relevant.

Rule:

```text
old allowed AND new allowed -> may disclose rename metadata
otherwise                  -> omit/fail closed
```

This prevents leaking a denied filename through rename metadata.

## 6. UC-GIT-02 two-stage diff flow

The critical invariant is:

> Never obtain a broad diff body containing denied files and then filter it afterward.

Required flow:

```text
Stage A — candidate discovery
  machine-readable Git metadata
          ↓
  candidate changed paths
          ↓
  workspace/security filtering
          ↓
  allowed path set

Stage B — body retrieval
  explicit allowed pathspec(s)
          ↓
  git diff only for allowed paths
          ↓
  bounded patch
          ↓
  response
```

If a rename has either an old or new denied path, omit that diff entirely because patch headers may reveal the denied path.

## 7. Diff target / base handling

The public UC mentions an optional `base` input. It must never become an arbitrary shell fragment.

Implementation guidance:

- normalize the optional value through a typed/internal revision representation;
- V0.2 should support only the minimum revision behavior required by the UC and tests;
- omission should represent the approved working-tree default;
- if `HEAD` is supported, treat it as a validated revision value, not a command string;
- unsupported revision syntax returns a stable invalid-argument error;
- adding commit-history exploration is out of scope.

Do not broaden this phase into a generic Git query language.

## 8. Binary and large diffs

Do not request Git binary payloads.

For an allowed binary file, the response may expose safe metadata indicating that the file is binary, but should not return binary patch data.

For large text patches:

- enforce hard total-byte limits;
- enforce per-record limits if needed;
- return deterministic continuation/truncation metadata;
- never load unbounded diff output into memory.

## 9. Stable ordering and cursor safety

Pagination must be based on a stable, normalized ordering of visible candidates.

A cursor must not contain raw absolute paths or secret names. Reuse the existing page/cursor pattern when practical.

A continuation should detect incompatible query changes rather than silently applying a cursor from a different request shape.

## 10. Error model

At minimum, preserve the UC error semantics:

```text
GIT_NOT_AVAILABLE
C2C_TIMEOUT
OUTPUT_LIMIT_EXCEEDED
INVALID_ARGUMENT / existing common invalid-argument code
```

Infrastructure exceptions and stderr text are not remote error contracts.

Do not return raw Git stderr if it can contain local paths.

## 11. Observability

Allowed structured fields include:

```text
operation = status|diff
duration_ms
exit_classification
candidate_count
allowed_count
denied_count
returned_count
truncated = true|false
```

Forbidden logs:

```text
raw patch body
raw status line containing denied path
absolute repository path
secret filename/body
git authorization material
```

## 12. Concurrency model

Git status/diff are read-only and may run while the editor modifies files. Results are an observed snapshot, not a transaction.

The implementation must fail safely when a file changes/disappears/becomes denied between candidate discovery and patch retrieval.

No distributed lock or database is needed for V0.2.