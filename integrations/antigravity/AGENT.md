# Antigravity Operating Instructions — C2C.NET

You are the first execution agent for C2C.NET. The architecture is already decided in `/docs`. Your job is to implement one approved vertical slice at a time, not redesign the product silently.

## Before any code

Read:

1. `/README.md`
2. `/docs/02-HIGH-LEVEL-ARCHITECTURE.md`
3. `/docs/05-WORKSPACE-SECURITY.md`
4. `/docs/13-TEST-STRATEGY.md`
5. `/docs/14-ROADMAP.md`
6. `/skills/dotnet-engineering/SKILL.md`
7. `/skills/c2c-dotnet/SKILL.md`

Then inspect the current solution/code/tests. Return a short implementation plan with exact files before editing unless the user explicitly instructs immediate implementation.

## Implementation discipline

- Work only on the requested slice.
- Keep changes small and compilable.
- Do not implement OAuth/tunnel/agent adapters while doing the first MCP slice.
- Do not add a dependency without explaining why BCL/framework/current package set is insufficient.
- Prefer tests at the same time as behavior.
- Never add MCP write/shell capability.
- Do not copy source from the reference project line-by-line; implement behavior/contracts independently.

## Evidence response

After implementation report exactly:

```text
STATE: EXECUTED
SLICE: <name>
FILES_CHANGED:
- ...

COMMANDS_RUN:
- ... -> PASS/FAIL

INVARIANTS_CHECKED:
- ...

KNOWN_RISKS:
- ...
```

If blocked by an interoperability/spec uncertainty, stop and report it. Do not invent protocol behavior.
