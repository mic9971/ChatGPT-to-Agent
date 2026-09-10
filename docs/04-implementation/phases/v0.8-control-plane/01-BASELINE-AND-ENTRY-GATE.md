# V0.8 Baseline and Entry Gate

## Verified repository baseline

This design was authored against `main` after Phase V0.6 runtime was merged.

The merged V0.6 implementation provides:

- dedicated `C2C.Cli` executable;
- stable JSON envelope and exit-code conventions;
- `c2c setup`, `status`, `doctor`;
- `c2c start`, `stop`, `ensure`;
- `c2c pair`, `unpair`;
- `c2c record`, bounded sanitized `logs`;
- runtime ownership state and PID-reuse protection;
- 385/385 tests reported green in the V0.6 PR.

The repository also contains the V0.7 implementation design pack, but V0.8 runtime work must verify the **actual V0.7 implementation**, not merely the docs.

## Required V0.7 runtime evidence

Before any V0.8 production code, verify all of the following:

```text
[ ] C2C protocol models/parser/renderer implemented
[ ] deterministic transition validator implemented
[ ] atomic session/checkpoint store implemented
[ ] one-active-executor lease implemented
[ ] UC-C2C-01..08 implemented
[ ] UC-CLI-06 implemented
[ ] duplicate identical transitions are idempotent
[ ] conflicting duplicate transitions fail closed
[ ] EXECUTED validates workspace/task/iteration evidence binding
[ ] DONE requires current reviewed evidence
[ ] resume derives deterministic next action
[ ] HANDOFF is bounded and sanitized
[ ] no RESUME wire/protocol state exists
[ ] full build/test green
```

If any item is missing, return `STATE: BLOCKED` for V0.8 implementation and finish V0.7 first.

## Baseline inspection before edits

The implementation agent must inspect:

1. `C2CNet.sln` projects and target frameworks;
2. actual `C2C.Core/C2C` types created in V0.7;
3. actual `C2C.Infrastructure/C2C` persistence/lease types;
4. actual `C2C.Cli` session commands and JSON shapes;
5. `IExecutionRecorder` / execution query contracts;
6. runtime readiness result returned by `c2c ensure --json`;
7. current app-state path conventions;
8. current error-code catalog;
9. current tests and DI composition;
10. `integrations/antigravity/AGENT.md`.

Do not implement against type names proposed by design docs if V0.7 chose different approved names. Preserve semantics, not speculative names.

## Why V0.8 is a new boundary

V0.8 introduces untrusted UI/browser transport around an already-trusted local protocol state machine. That adds new failure classes:

- browser navigation failure;
- logged-out browser session;
- MFA/passkey/CAPTCHA interruption;
- wrong conversation attachment;
- send succeeded but local acknowledgement failed;
- duplicated browser submission;
- stale/partial assistant response;
- wrong task/iteration response;
- conversation deletion/replacement;
- browser crash between send and receive.

None of these failures is allowed to mutate protocol state unless the V0.7 validator accepts a corresponding C2C transition.

## Stop conditions

Stop and request review if implementation would require:

- exposing browser cookies/session storage to C2C.NET;
- storing ChatGPT credentials in app-state;
- adding browser automation to `C2C.Host`;
- coupling `C2C.Core` to Antigravity/Codex/ChatGPT DOM types;
- bypassing user login/MFA/CAPTCHA;
- adding MCP write/exec tools;
- sending raw source/diff/log through ChatGPT control messages;
- implementing a Python runtime dependency solely to control Antigravity IDE;
- changing V0.7 protocol semantics merely to accommodate browser quirks;
- auto-resending an ambiguous message without first inspecting the conversation;
- opening a replacement conversation silently without HANDOFF semantics.

## Entry command evidence

At minimum run:

```bash
dotnet build C2CNet.sln
dotnet test C2CNet.sln
c2c ensure --json
```

For the CLI command, use the actual local execution shape supported by the repository/build output. Do not claim READY from documentation alone.
