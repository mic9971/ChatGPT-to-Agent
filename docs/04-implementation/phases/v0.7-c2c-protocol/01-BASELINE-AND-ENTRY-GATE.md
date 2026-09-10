# V0.7 Baseline and Entry Gate

## Repository baseline to verify

Before runtime work, Antigravity must inspect the actual branch and confirm the previous boundaries are present and green.

Expected prerequisites:

- V0.1 secure workspace read and stateless MCP;
- V0.2 list/search/Git evidence;
- V0.3 immutable execution evidence;
- V0.4 tunnel/runtime connectivity;
- V0.5 authorization/pairing/protected MCP;
- V0.6 CLI foundation and runtime lifecycle.

The phase must use repository evidence, not roadmap text alone, to mark a prerequisite complete.

## Required V0.6 capabilities

The implementation branch entering V0.7 must provide a usable CLI substrate for machine-readable commands and must have stable conventions for:

- JSON envelope and `schemaVersion`;
- exit codes;
- `c2c setup`;
- `c2c start` / `c2c stop`;
- `c2c status` / `c2c doctor`;
- `c2c ensure --json`;
- `c2c pair` / `c2c unpair`;
- `c2c record` / bounded `c2c logs`.

`UC-CLI-06` is intentionally excluded from V0.6 and implemented in this phase because it depends on the C2C session model.

## Entry verification

Run at minimum:

```bash
dotnet build C2CNet.sln
dotnet test C2CNet.sln
```

Then inspect:

- current projects in `C2CNet.sln`;
- `C2C.Core` capability folders;
- `C2C.Infrastructure` capability folders;
- `C2C.Cli` project and output contract if already implemented;
- execution record/query contracts;
- workspace identity/context;
- atomic app-state persistence patterns already used by Workspace/Tunnel/Execution/Auth;
- existing error codes;
- current DI registrations.

## Stop conditions

Return `STATE: BLOCKED` and do not implement V0.7 when any of these is true:

- build/test baseline is red for unrelated reasons;
- V0.6 runtime CLI substrate is absent on the target implementation branch;
- no stable workspace identity is available to bind sessions;
- execution evidence cannot be resolved by workspace/task/iteration;
- the proposed design would require adding browser automation early;
- implementing C2C would require weakening workspace/auth/MCP boundaries;
- session persistence location conflicts with existing app-state ownership.

## Existing design that must remain authoritative

Read before implementation:

```text
AGENTS.md
.agent/rules/* applicable to changed files
skills/csharp-clean-code/SKILL.md
skills/dotnet-engineering/SKILL.md
skills/dotnet-project-conventions/SKILL.md

docs/01-architecture/03-C2C-PROTOCOL.md
docs/01-architecture/08-EXECUTION-REVIEW.md
docs/01-architecture/09-CLI-DESIGN.md
docs/01-architecture/11-DATA-MODEL.md
docs/01-architecture/12-ERROR-RECOVERY.md

docs/02-common/00-BR-COMMON.md
docs/02-common/02-BR-CONCURRENCY-IDEMPOTENCY.md
docs/02-common/03-BR-C2C-PROTOCOL.md
docs/02-common/05-BR-EXECUTION-EVIDENCE.md
docs/02-common/06-BR-OBSERVABILITY-ERRORS.md
```

## Current-main note

At the time this phase pack was authored, latest `main` contained the V0.5 runtime implementation and V0.6 implementation documentation. Therefore this pack is implementation-ready documentation, but the agent must still prove V0.6 runtime completion on the branch used for coding before advancing V0.7.
