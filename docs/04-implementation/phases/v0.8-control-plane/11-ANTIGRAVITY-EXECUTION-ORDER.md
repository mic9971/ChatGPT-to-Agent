# V0.8 Antigravity Execution Order

## Purpose

Keep Phase V0.8 implementation bounded. Do not let the implementation agent jump from control metadata directly into live browser automation before the reusable substrate is proven.

## Step 0 — Verify entry gate only

Read:

```text
AGENTS.md
.agent/rules/* applicable
skills/csharp-clean-code/SKILL.md
skills/dotnet-engineering/SKILL.md
skills/dotnet-project-conventions/SKILL.md

V0.7 implementation pack
this V0.8 implementation pack
actual C2C/CLI source and tests
```

Run build/tests and inspect actual V0.7 implementation.

Return:

```text
STATE: BASELINE_VERIFIED | BLOCKED
V0_7_GATE:
...
CURRENT_PROJECTS:
...
CURRENT_SESSION_COMMANDS:
...
TESTS:
...
RISKS:
...
NEXT_RECOMMENDED_ACTION:
Prepare V0.8A-1 PLAN only.
```

Do not edit files.

## Step 1 — PLAN V0.8A-1 only

Scope:

```text
ControlPlane provider-neutral models/contracts
conversation binding persistence
delivery journal
UC-CTRL-01
UC-CTRL-02
```

Do not implement send/receive loop yet.

Plan must name exact files, reused V0.7 contracts, persistence implications, error codes and tests.

## Step 2 — IMPLEMENT V0.8A-1

Implement only the approved plan.

Required gate:

```text
build green
tests green
binding/task/workspace isolation proven
no credentials persisted
no Antigravity dependency in Core
```

Stop for review.

## Step 3 — PLAN V0.8A-2 only

Scope:

```text
UC-CTRL-03 send
UC-CTRL-04 receive/validate
ambiguous-send reconciliation
fake/manual transport seam
```

Must explicitly show how V0.7 parser/validator remains authoritative.

## Step 4 — IMPLEMENT V0.8A-2

Gate:

```text
fake transport tests green
duplicate/conflict tests green
ambiguous send does not auto-resend
browser/transport failure cannot advance protocol state
```

Stop for review.

## Step 5 — PLAN + IMPLEMENT V0.8A-3

Scope:

```text
UC-CTRL-05 iteration loop
UC-CTRL-06 resume
UC-CTRL-07 handoff
UC-CTRL-08 recovery/manual fallback
```

Keep loop bounded and cancellation-safe.

Required deterministic E2E fixtures:

```text
one-iteration DONE
two-iteration REPLAN -> DONE
login-required pause
missing conversation -> HANDOFF
transport unavailable -> manual fallback
```

Stop after V0.8A gate passes.

## Step 6 — PLAN V0.8B integration pack

No production source code first. Prepare exact file plan for:

```text
integrations/antigravity/AGENT.md
integrations/antigravity/C2C-WORKFLOW.md
integrations/antigravity/setup.md
optional validated mcp_config.template.json
any version/manifest metadata required by UC-AGT-01
```

Describe how Antigravity uses its own editor/terminal/browser capability and existing C2C CLI/session commands.

Do not create `.NET AntigravityBrowserControlDriver` unless a separate approved ADR proves a concrete callable API is required and fits the generic architecture.

## Step 7 — IMPLEMENT V0.8B integration pack

Implement `UC-AGT-01..04` as bounded integration docs/skills/adapters.

The integration must not change C2C protocol semantics.

## Step 8 — Operator-assisted Antigravity smoke

Run a harmless fixture task, not a broad production change.

Required path:

```text
ensure
 -> session new
 -> Browser Agent opens ChatGPT
 -> INIT
 -> PLAN
 -> local fixture edit/test
 -> record evidence
 -> EXECUTED
 -> ChatGPT MCP evidence review
 -> DONE or one REPLAN
```

Exercise one recovery case:

```text
browser restart/resume
or
conversation HANDOFF
```

User handles any login/password/passkey/MFA/CAPTCHA challenge.

## Step 9 — Final review

Return:

```text
STATE: EXECUTED
PHASE: V0.8

V0_8A:
- ...

V0_8B:
- ...

FILES_CHANGED:
- ...

TESTS:
- ...

REAL_SMOKE:
- ...

SECURITY_GATES:
- [x]/[ ] no source/diff/log control relay
- [x]/[ ] no ChatGPT credential persistence
- [x]/[ ] wrong task/iteration rejected
- [x]/[ ] ambiguous send reconciled
- [x]/[ ] manual fallback validated
- [x]/[ ] Antigravity integration stays outside Core

KNOWN_RISKS:
- ...
```

Do not start V0.9 automatically.
