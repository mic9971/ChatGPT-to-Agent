# V0.2 Antigravity Execution Order

## Operating rule

Implement one UC at a time. Git evidence is security-sensitive and must not be bundled with future V0.3 execution-evidence work.

## Step 0 — Baseline verification

Prompt:

```text
Verify the current ChatGPT-to-Agent baseline before new implementation.

Read AGENTS.md and applicable rules/skills.
Do not modify files.

Inspect:
- current MCP tool registration
- Workspace Core/Infrastructure contracts
- current tests

Run:
- dotnet build C2CNet.sln
- dotnet test C2CNet.sln

Confirm the current four workspace MCP tools and report any failing baseline test.
Stop after verification.
```

Expected result: current baseline is green before Git code is added.

## Step 1 — UC-GIT-01 PLAN

Prompt:

```text
Prepare the implementation PLAN for UC-GIT-01 only.

Read:
1. AGENTS.md
2. applicable .agent/rules
3. skills/dotnet-engineering/SKILL.md
4. skills/csharp-clean-code/SKILL.md
5. skills/dotnet-project-conventions/SKILL.md
6. UC-GIT-01
7. every BR referenced by UC-GIT-01
8. docs/04-implementation/phases/v0.2-git-evidence/00-README.md
9. 01-CURRENT-BASELINE.md
10. 02-DETAILED-DESIGN.md
11. 03-UC-GIT-01-IMPLEMENTATION.md
12. 06-TEST-AND-SECURITY-GATE.md
13. nearest existing source/tests

Do not edit yet.

Return:
- exact files to create/change
- contracts
- Git process strategy
- porcelain format/parser strategy
- workspace security filtering flow
- rename handling
- pagination/limits
- errors
- test matrix
- non-goals

Do not include UC-GIT-02 implementation.
```

Review the plan before allowing edits if it changes a security boundary, public schema, dependency direction, or process lifecycle.

## Step 2 — UC-GIT-01 IMPLEMENT

Prompt:

```text
Implement the approved UC-GIT-01 plan only.

Requirements:
- no shell
- NUL-delimited machine-readable status parsing
- every candidate path uses the shared workspace visibility policy
- both source and destination of rename/copy must be visible
- stable ordering and bounded paging
- safe errors
- MCP git_status adapter only after Core/Infrastructure behavior is proven
- no unrelated refactor
- no Git mutation
- do not commit

Run focused tests, full build, and full test suite.
Return the phase completion format from 03-UC-GIT-01-IMPLEMENTATION.md.
Stop after UC-GIT-01.
```

## Step 3 — UC-GIT-01 review gate

Do not start diff until these are green:

```text
[ ] allowed statuses correct
[ ] sensitive filenames omitted
[ ] rename boundary secure
[ ] workspace-subdirectory boundary secure
[ ] no shell
[ ] no mutation
[ ] output bounded
[ ] MCP integration test green
[ ] full build/test green
```

## Step 4 — UC-GIT-02 PLAN

Prompt:

```text
Prepare the implementation PLAN for UC-GIT-02 only.

UC-GIT-01 is the dependency and must be reused.

Read:
- AGENTS.md and applicable rules/skills
- UC-GIT-02 and referenced BRs
- phase 02-DETAILED-DESIGN.md
- phase 04-UC-GIT-02-IMPLEMENTATION.md
- phase 05-MCP-ADAPTER-AND-SCHEMAS.md
- phase 06-TEST-AND-SECURITY-GATE.md
- actual UC-GIT-01 source/tests

Do not edit yet.

Explain specifically how the implementation guarantees that denied patch bodies are never retrieved before filtering.
Return exact files, contracts, two-stage diff flow, pathspec strategy, revision validation, bounds, race handling, and tests.
Do not implement V0.3.
```

If the plan proposes `git diff` across all changed files followed by post-filtering, reject the plan.

## Step 5 — UC-GIT-02 IMPLEMENT

Prompt:

```text
Implement the approved UC-GIT-02 plan only.

Critical invariant:
Candidate paths are filtered through workspace security before any patch body is retrieved.

Required:
- reuse UC-GIT-01 process/candidate infrastructure
- explicit allowed pathspec diff retrieval
- rename old/new visibility enforcement
- no binary payload
- hard diff byte/item bounds
- safe optional base validation
- cancellation/timeout
- MCP git_diff adapter
- secret canary adversarial tests
- workspace subdirectory isolation test
- no unrelated refactor
- no Git mutation
- no commit

Run focused tests, full build, and full test suite.
Return the completion format from 04-UC-GIT-02-IMPLEMENTATION.md.
Stop after UC-GIT-02.
```

## Step 6 — Phase review

After both UCs finish, run one final review task without code changes:

```text
Review Phase V0.2 Git Evidence against:
- UC-GIT-01
- UC-GIT-02
- all referenced BRs
- 06-TEST-AND-SECURITY-GATE.md
- MCP read-only invariant
- dotnet-project-conventions placement rules

Inspect actual diff/source/tests.
Run build/tests if needed.
Return PASS/BLOCKED with exact missing acceptance criteria.
Do not start V0.3.
```

## Stop conditions

Antigravity must stop rather than improvise if:

- Git behavior/spec interpretation is ambiguous in a security-sensitive way;
- an existing test is failing before the target change;
- a proposed implementation requires a new broad shell/write capability;
- the workspace boundary cannot be proven for a parent Git repository;
- a denied path/body is observed in any returned/captured evidence;
- completing the UC requires redesign outside approved scope.

Design changes are handled first through documentation/ADR, then implementation resumes.