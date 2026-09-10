# V0.7 Antigravity Execution Order

## Principle

Implement V0.7 in bounded slices. Do not let the agent consume the whole phase in one speculative pass.

## Step 0 — Entry verification

Read the phase pack and actual repository. Run build/tests. Confirm V0.6 runtime CLI exists on the implementation branch and previous security gates are green.

Return only:

```text
STATE: BASELINE_VERIFIED | BLOCKED
BASE_COMMIT: ...
BUILD: ...
TESTS: ...
V0_6_CLI: ...
C2C_EXISTING_CODE: ...
RISKS: ...
NEXT: PLAN V0.7A only
```

Do not edit code in Step 0.

## Step 1 — PLAN V0.7A Protocol substrate

Plan exact files/contracts for:

- protocol message/state models;
- parser;
- renderer;
- transition validator;
- session model/store abstraction;
- JSON persistence;
- checkpoint versioning;
- executor lease;
- Core/Infrastructure DI.

No CLI/browser work yet.

## Step 2 — IMPLEMENT V0.7A

Implement only the approved substrate and focused tests. Run full solution tests. Stop with `STATE: EXECUTED` and exact evidence.

## Step 3 — PLAN/IMPLEMENT V0.7B

Implement `UC-C2C-01..03`:

```text
Create task -> INIT
Accept PLAN
Mark EXECUTING
```

Mandatory review points:

- iteration semantics;
- duplicate PLAN conflict behavior;
- prompt-injection isolation;
- crash while EXECUTING.

## Step 4 — PLAN/IMPLEMENT V0.7C

Implement `UC-C2C-04..06`:

```text
Attach execution evidence
 -> EXECUTED
 -> accept REPLAN path
 -> DONE
```

Mandatory review points:

- workspace/task/iteration evidence binding;
- no artifact body in control message;
- stale evidence rejection;
- terminal DONE immutability.

## Step 5 — PLAN/IMPLEMENT V0.7D

Implement `UC-C2C-07..08`:

```text
BLOCKED / ERROR
resume next-action derivation
HANDOFF
```

Mandatory review points:

- no `RESUME` protocol state;
- corrupt checkpoint fail-closed;
- HANDOFF hard cap;
- no source/diff/log/token leakage.

## Step 6 — PLAN/IMPLEMENT V0.7E

Implement `UC-CLI-06` using the already implemented V0.6 CLI substrate.

Do not duplicate business logic in command handlers.

Minimum command capability:

```text
session new
session status
session accept
session executing
session executed
session handoff
```

Adapt command names only when existing CLI conventions make a different shape materially cleaner.

## Step 7 — Final V0.7 gate

Run the full test/security matrix, including canaries and concurrency races.

Return:

```text
STATE: V0_7_COMPLETE | BLOCKED
BASE_COMMIT: ...
HEAD_COMMIT: ...
BUILD: ...
TESTS: ...
UC_STATUS:
- UC-C2C-01: ...
- UC-C2C-02: ...
- UC-C2C-03: ...
- UC-C2C-04: ...
- UC-C2C-05: ...
- UC-C2C-06: ...
- UC-C2C-07: ...
- UC-C2C-08: ...
- UC-CLI-06: ...
SECURITY_GATE: ...
REMAINING_RISKS: ...
NEXT: V0.8A Control Plane Automation PLAN only
```

## Permanent exclusions during V0.7

Antigravity must not implement:

- ChatGPT browser automation;
- browser selectors/coordinates;
- login/CAPTCHA/MFA handling;
- an Antigravity-specific .NET browser driver;
- new MCP write/shell tools;
- automatic Git commit/push;
- V0.8 control-plane transport.

## Prompt for Step 0

```text
Verify Phase V0.7 entry on the current ChatGPT-to-Agent workspace.

Read AGENTS.md, applicable .agent/rules, relevant skills, the V0.7 implementation pack, C2C architecture/BR/use cases, and actual source.

Do not modify files.

Run dotnet build C2CNet.sln and dotnet test C2CNet.sln.

Confirm V0.6 runtime CLI is implemented, not merely documented. Inspect existing C2C code and report any conflicts with the phase pack.

Return exactly STATE: BASELINE_VERIFIED or STATE: BLOCKED, then baseline commit, build/test results, V0.6 CLI evidence, existing C2C code, risks, and NEXT: PLAN V0.7A only.
```
