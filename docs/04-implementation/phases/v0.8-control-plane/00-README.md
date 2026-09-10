# Phase V0.8 — Control Plane Automation + Antigravity Integration

## Objective

Turn the provider-neutral C2C protocol from V0.7 into a safe end-to-end control loop between an execution agent and ChatGPT Web without moving source, diff or raw log bodies onto the control plane.

Phase V0.8 is deliberately split into two gates:

```text
V0.8A — Control Plane Coordination Substrate
  -> UC-CTRL-01..04
  -> UC-CTRL-05..08
  -> conversation binding + delivery journal
  -> fake/manual transport proof

V0.8B — Execution Agent Integration
  -> UC-AGT-01..04
  -> Antigravity integration pack
  -> Antigravity Browser Agent workflow
  -> real operator-assisted smoke
```

## Critical architecture decision

C2C.NET does **not** embed or control the Antigravity IDE browser from .NET.

The production Antigravity path is agent-owned:

```text
Antigravity Agent
  ├─ editor / terminal -> executes accepted PLAN locally
  ├─ c2c CLI          -> runtime/session/evidence/control metadata
  └─ Browser Agent    -> opens ChatGPT and relays bounded C2C messages

ChatGPT
  └─ MCP/OAuth        -> independently reads workspace/evidence from C2C.NET
```

A provider-neutral transport seam may exist for deterministic tests, manual fallback and future SDK adapters, but `C2C.Core` must not depend on Antigravity, ChatGPT DOM details or a proprietary browser SDK.

## Entry gate

The implementation pack may be merged before V0.7 runtime is complete, but V0.8 runtime work starts only after the V0.7 gate is proven on the target implementation branch.

Required before V0.8 implementation:

- V0.6 CLI runtime is green;
- V0.7 C2C session/protocol runtime is implemented;
- `UC-CLI-06` session commands exist and are stable;
- session/checkpoint recovery is restart-safe;
- executor lease semantics are proven;
- `EXECUTED` binds to immutable evidence;
- `DONE` is evidence-gated;
- full solution build/test is green.

## Primary invariants

1. The data plane remains MCP; the control plane never relays source/diff/raw-log bodies.
2. Browser/account authentication belongs to the user.
3. Conversation binding is exactly one `workspace_id + task_id`.
4. Browser failure never advances C2C protocol state.
5. Send ambiguity is reconciled before resend.
6. Identical delivery is idempotent; conflicting duplicate content fails closed.
7. `EXECUTED` is sent only after evidence persistence succeeds.
8. Antigravity executes code; the control coordinator does not.
9. Manual fallback preserves the same parser, validator, task identity and security rules.
10. No live ChatGPT credential is required in CI.

## Read order

1. `01-BASELINE-AND-ENTRY-GATE.md`
2. `02-PHASE-ARCHITECTURE-AND-BOUNDARIES.md`
3. `03-CONTROL-TRANSPORT-CONTRACTS.md`
4. `04-CONVERSATION-BINDING-AND-DELIVERY.md`
5. `05-UC-CTRL-01-04-IMPLEMENTATION.md`
6. `06-UC-CTRL-05-08-IMPLEMENTATION.md`
7. `07-MANUAL-FALLBACK-AND-RECOVERY.md`
8. `08-ANTIGRAVITY-INTEGRATION-DESIGN.md`
9. `09-CHATGPT-MCP-AND-BROWSER-SETUP.md`
10. `10-TEST-AND-SECURITY-GATE.md`
11. `11-ANTIGRAVITY-EXECUTION-ORDER.md`
12. `12-PHASE-DONE-AND-V0.9-HANDOFF.md`
13. `13-RESEARCH-BASIS-2026-09.md`

## Non-goals

- no MCP write/shell/delete/commit tool;
- no automatic Git commit/push;
- no storing ChatGPT cookies/password/passkeys/MFA material;
- no CAPTCHA or MFA bypass;
- no browser coordinate scripts as a stable product contract;
- no .NET dependency on Antigravity SDK/IDE internals;
- no Python sidecar required for the V0.8 IDE path;
- no provider-specific browser implementation in `C2C.Core`;
- no hidden transcript persistence;
- no Phase V0.9 packaging work.
