# Antigravity Operating Instructions — C2C.NET

You are the first execution agent for C2C.NET. The architecture is already decided in `/docs`. Implement one approved use case/vertical slice at a time; do not redesign the product silently.

## Before any code

Read in this order:

1. `/AGENTS.md`
2. `/.agent/rules/00-GLOBAL.md`
3. `/.agent/rules/02-USE-CASE-IMPLEMENTATION.md`
4. target UC under `/docs/03-use-cases/`
5. every BR referenced by that UC under `/docs/02-common/`
6. only the architecture documents referenced by that UC
7. nearest source/tests
8. `/skills/dotnet-engineering/SKILL.md` and `/skills/c2c-dotnet/SKILL.md` when applicable

Return a bounded file/test plan before editing when the UC crosses a new security, persistence, public-contract or process boundary.

## Implementation discipline

- Work only on the requested UC/slice.
- Keep changes small and compilable.
- Never add MCP write/shell/delete/commit capabilities.
- Do not implement OAuth/tunnel/control-plane automation while working on an earlier workspace/MCP slice.
- Do not create `IControlPlaneDriver` or browser automation before approved V0.8 work; the design exists early, implementation does not.
- Do not add a dependency without explaining why BCL/framework/current package set is insufficient.
- Prefer tests at the same time as behavior.
- Do not copy source from the reference project line-by-line; implement behavior/contracts independently.

## Future V0.8 control-plane behavior

When the target UC is explicitly `UC-CTRL-*` or `UC-AGT-*`:

1. run `c2c ensure --json`;
2. open/attach the task-bound ChatGPT conversation through Antigravity browser/computer-use capability;
3. prefer semantic/accessibility interaction over fixed coordinates;
4. send only validated bounded C2C messages;
5. pause for user-owned password/passkey/CAPTCHA/MFA/2FA challenges;
6. never persist ChatGPT browser credentials/cookies in C2C.NET;
7. execute PLAN locally using Antigravity editor/terminal;
8. persist evidence before relaying EXECUTED;
9. validate planner `TASK_ID`, `ITERATION` and state before advancing;
10. on browser failure keep the previous checkpoint and use bounded recovery/manual fallback;
11. use HANDOFF only when the previous conversation is unavailable.

Read `/docs/01-architecture/20-CONTROL-PLANE-AUTOMATION.md` and `/docs/02-common/09-BR-CONTROL-PLANE.md` for those phases.

## Evidence response

After implementation report:

```text
STATE: EXECUTED
USE_CASE: <UC-ID>
FILES_CHANGED:
- ...

COMMANDS_RUN:
- ... -> PASS/FAIL

BUSINESS_RULES:
- ...

ACCEPTANCE_CRITERIA:
- [x]/[ ] ...

KNOWN_RISKS:
- ...
```

If blocked by interoperability/spec/security uncertainty, stop and report it. Do not invent protocol behavior. Do not commit unless the user explicitly requests it.
