---
name: .NET Project Conventions
description: >
  Use this skill whenever adding, moving, renaming, reviewing, or designing .NET files, folders,
  models, interfaces, DTOs, use cases, projects, tests, DI registrations, or shared/common code.
  It governs where code belongs and how business capabilities are organized. It complements,
  but does not replace, csharp-clean-code and dotnet-engineering.
---

# .NET Project Conventions

## Mission

Keep the repository easy for humans and coding agents to navigate by making placement deterministic.
The default organization is:

```text
Clean dependency boundaries
        +
Business capability first
        +
Use-case / vertical-slice cohesion
        +
Strict model and shared-code taxonomy
```

This skill is about **where code belongs and why**. For language-level C# style use `../csharp-clean-code/SKILL.md`. For runtime/.NET engineering rules use `../dotnet-engineering/SKILL.md`.

## Mandatory placement workflow

Before creating or moving a file:

1. Identify the **business capability** it belongs to (`Workspace`, `Execution`, `Authorization`, `Tunnel`, `ControlPlane`, ...).
2. Identify its **architectural role** (domain/core, use-case contract/orchestration, adapter, persistence, provider integration, transport schema, configuration, test).
3. Prefer the nearest existing capability folder and local convention.
4. Use a use-case subfolder when it improves cohesion; do not create technical dumping grounds.
5. Classify every new data type using the model taxonomy before naming it.
6. Promote code to `Common`/shared only when reuse is semantic, stable, and crosses real capability boundaries.
7. Place interfaces next to the abstraction owner, not in a global `Interfaces/` folder.
8. Mirror production capability/use-case structure in tests.
9. If two placements are both plausible and no local rule decides, stop and report the alternatives instead of inventing a new repository pattern.

## Hard rules

- Do not create root/global `Models`, `Services`, `Repositories`, `Handlers`, `Managers`, `Helpers`, or `Utils` folders that mix unrelated business capabilities.
- Do not move existing code merely to satisfy this skill unless the target UC explicitly includes structural refactoring.
- Business capability is the first grouping axis inside an architectural project unless the project itself is a transport adapter; adapter projects may group by transport first and capability second.
- A DTO/data shape is not automatically a Domain Model.
- Same shape does not imply same semantic type. Do not share models only because their properties match.
- Do not create interfaces only to make mocking easier. An interface represents a boundary/port, replaceable strategy/provider, or nondeterministic/external dependency.
- Do not create speculative `Base*`, `SharedKernel`, generic repository, or cross-cutting abstraction without a current use case and clear ownership.
- Keep dependency direction consistent with the approved architecture. Placement never overrides architecture/BR/security rules.

## Repository-specific default

For C2C.NET, project boundaries remain architectural while folders inside each project are capability-oriented:

```text
src/
├── C2C.Core/
│   ├── Workspace/
│   ├── Execution/
│   ├── C2C/
│   └── Common/             # only truly cross-capability primitives
├── C2C.Infrastructure/
│   ├── Workspace/
│   ├── Execution/
│   ├── Git/
│   └── Tunnel/
├── C2C.Security/
│   └── Authorization/
├── C2C.Host/
│   ├── Mcp/<Capability>/
│   └── Auth/
└── C2C.Cli/
    └── <Capability-or-command-group>/
```

Do not reorganize the repository wholesale to match this example. Preserve nearby structure unless an approved UC/ADR requires migration.

## Decision order / precedence

When rules disagree, follow this precedence:

```text
Security BR / approved ADR
>
Target UC detailed design
>
Repository architecture docs
>
This project-conventions skill
>
Generic dotnet-engineering skill
>
Generic C# clean-code skill
```

## Reference map

Read only the focused references needed for the change:

- `references/01-PROJECT-STRUCTURE.md`
- `references/02-FEATURE-FOLDER-RULES.md`
- `references/03-MODEL-TAXONOMY.md`
- `references/04-DOMAIN-MODEL.md`
- `references/05-USE-CASE-CONVENTIONS.md`
- `references/06-CONTRACT-DTO-RULES.md`
- `references/07-COMMON-SHARED-RULES.md`
- `references/08-INTERFACE-PLACEMENT.md`
- `references/09-NAMING-CONVENTIONS.md`
- `references/10-DEPENDENCY-RULES.md`
- `references/11-TEST-STRUCTURE.md`
- `references/12-DI-CONVENTIONS.md`
- `references/13-ERROR-RESULT-RULES.md`
- `references/14-ASYNC-CONCURRENCY.md`
- `references/15-ANTI-PATTERNS.md`
- `references/16-PLACEMENT-DECISION-TREE.md`
- `references/17-RESEARCH-BASIS.md`

## Agent completion check

Before reporting a code task complete, verify:

- every new file has a named capability owner;
- every new model has a taxonomy category;
- no new dumping-ground folder appeared;
- shared/common promotion is justified;
- interfaces live with abstraction owners;
- tests mirror the behavior/capability;
- no unrelated structural cleanup was included.
