# Skill: Implement C2C.NET Safely

Read `../../README.md`, `../../docs/01-architecture/02-HIGH-LEVEL-ARCHITECTURE.md`, `../../docs/01-architecture/05-WORKSPACE-SECURITY.md`, `../../docs/04-implementation/00-ROADMAP.md`, and the generic `../dotnet-engineering/SKILL.md` before changing code.

## Hard invariants

1. MCP is read-only V1. Never add shell/write/delete/commit tools.
2. One bridge process = one workspace.
3. Every path-bearing capability uses the same canonical workspace access policy.
4. Sensitive deny applies to read/list/search/git/evidence, not only file read.
5. Current MCP baseline is 2026-07-28 stateless core; do not add transport session storage without a spec reason.
6. C2C task checkpoint is application state, not MCP session state.
7. Planner reviews actual diff/test evidence through MCP.
8. Control messages stay bounded and contain no file/diff/log bodies.
9. CIMD-first auth direction; DCR is compatibility-only and must be justified by target-client interoperability evidence.
10. Security ambiguity fails closed.

## Implementation sequence

Work only on the current roadmap slice. Do not prebuild later architecture “just in case”.

For each slice return:

- files changed;
- key invariants preserved;
- exact tests/commands run;
- remaining risks/unknowns;
- no commit unless explicitly requested.

## Forbidden shortcuts

- `Path.GetFullPath` as the only containment control;
- prefix string path authorization without segment/canonical checks;
- broad Git diff then post-hoc filtering of secret bodies;
- logging raw process command/output before redaction;
- `.Result`/`.Wait()` in HTTP/async flow;
- service locator / `BuildServiceProvider` in registrations;
- blanket analyzer/nullable suppression;
- weakening test assertions to match implementation;
- copying original TypeScript line-for-line instead of implementing the documented .NET contract.
