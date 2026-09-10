# C2C.NET Design Pack v0.2 — Use-Case / Agent-Rule Edition

**Status:** detailed design ready for implementation planning; production code not included.  
**Target:** .NET 10 / C# 14.  
**Detailed use cases:** 42.  

This version reorganizes the earlier architecture pack into an **agent-friendly, use-case-driven design** similar to the rule/use-case workflow used in large agent-assisted projects.

## Folder model

```text
C2C.NET-Design-Pack-v0.2/
├── AGENTS.md
├── .agent/
│   └── rules/                    # mandatory agent operating rules
├── docs/
│   ├── 00-governance/            # read order, UC catalog, traceability, template
│   ├── 01-architecture/          # system-wide design
│   ├── 02-common/                # reusable BR-* rules/contracts
│   ├── 03-use-cases/             # one detailed design file per UC
│   │   ├── workspace/
│   │   ├── git/
│   │   ├── mcp/
│   │   ├── execution/
│   │   ├── tunnel/
│   │   ├── auth/
│   │   ├── c2c/
│   │   ├── cli/
│   │   ├── agent-integration/
│   │   └── packaging/
│   ├── 04-implementation/        # roadmap, DoD, release gates
│   └── 05-testing/               # test matrix/adversarial/E2E
├── skills/                       # reusable .NET and C2C skills
└── integrations/                 # Antigravity/Codex instruction packs
```

## How an agent should work

A coding agent should **not** read the whole repository design first. Start from the target UC, then follow its explicit BR/dependency/architecture references. Common rules live under `docs/02-common` and are normative.

Example:

```text
Implement UC-WS-04
  -> read UC-WS-04
  -> read BR-COM / BR-SEC referenced by it
  -> read only MCP + Workspace Security architecture refs
  -> inspect nearest source/tests
  -> implement/test
  -> verify UC acceptance checklist
```

## Recommended first coding task

`UC-WS-01` then the security core required by `UC-WS-04`; after adversarial boundary tests pass, expose `workspace_info` and `read_file` through `UC-MCP-01`.
