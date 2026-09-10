# V0.2 Phase Done and Handoff

## Definition of Done

V0.2 is complete only when both `UC-GIT-01` and `UC-GIT-02` satisfy their acceptance criteria and the phase security gate is green.

## Required evidence

The final phase report must include:

```text
PHASE: V0.2 — Workspace discovery + Git evidence
STATE: DONE | BLOCKED

BASELINE:
- existing workspace MCP tools verified

USE_CASES:
- UC-WS-03: verified status
- UC-WS-05: verified status
- UC-GIT-01: DONE/BLOCKED
- UC-GIT-02: DONE/BLOCKED

MCP_TOOLS:
- workspace_info
- read_file
- list_directory
- search_workspace
- git_status
- git_diff

SECURITY:
- traversal/symlink regression: PASS/FAIL
- sensitive path regression: PASS/FAIL
- .c2cignore regression: PASS/FAIL
- denied Git status name leak: PASS/FAIL
- denied Git diff body leak: PASS/FAIL
- rename boundary: PASS/FAIL
- workspace subdirectory boundary: PASS/FAIL
- no shell/mutation capability: PASS/FAIL

BOUNDS:
- status page limit: ...
- diff item/byte limit: ...
- process output/timeout: ...

COMMANDS_RUN:
- ...

DEVIATIONS:
- none / ...

KNOWN_RISKS:
- ...
```

## Blocking criteria

The phase is `BLOCKED`, not `DONE`, if any of the following remain:

- Git status can reveal a denied filename;
- Git diff can capture or return a denied patch body;
- rename old/new visibility is not handled safely;
- workspace configured below Git root can expose sibling repository paths;
- arbitrary shell/Git flags are accepted;
- output is unbounded;
- MCP tool schemas are unstable/undocumented;
- existing Workspace security tests regress;
- full solution build/tests are red.

## What should exist after V0.2

Architecturally:

```text
C2C.Core
├── Common
├── Workspace
└── Git

C2C.Infrastructure
├── Workspace
└── Git

C2C.Host
└── MCP
    ├── Workspace adapters
    └── Git adapters
```

Exact physical files may vary according to the approved implementation plan and the project-conventions skill, but ownership must remain clear.

## Planner capability after V0.2

The planner can independently inspect:

```text
workspace identity
file content
folder structure
workspace search
Git changed-file metadata
Git patch evidence
```

This is the minimum evidence layer required before adding local execution records.

## Handoff to V0.3

Only after V0.2 gate is accepted, start **V0.3 — Execution Evidence**:

```text
UC-EXE-01 Record Execution Evidence
UC-EXE-02 Read Execution Summary
UC-EXE-03 Read Normalized Test Status
UC-EXE-04 Read Sanitized Execution Output
```

V0.3 must reuse the evidence-first principle established here:

```text
Agent claims "done"
      ≠
planner trust

Planner reads:
Git diff + execution summary + test status + sanitized output
      ↓
independent review
```

Do not mix V0.3 persistence/execution-record abstractions into V0.2 Git code.

## Documentation status update

After implementation evidence is reviewed, update the relevant UC implementation-status fields and traceability/release gate documents in a separate, bounded documentation change if required. Do not mark future UCs as implemented early.