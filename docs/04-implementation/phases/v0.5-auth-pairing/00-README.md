# Phase V0.5 — Authorization, Pairing and Protected MCP

## Purpose

This pack turns the existing V0.5 auth use cases into an implementation-ready sequence without pulling V0.6 CLI or V0.7 C2C work forward.

Target use cases:

- `UC-AUTH-01` Create Pairing Session
- `UC-AUTH-02` Approve Pairing and Authorize Client
- `UC-AUTH-03` Issue and Validate Access Token
- `UC-AUTH-04` Rotate Refresh Token
- `UC-AUTH-05` Revoke / Unpair Client
- finish the protected-call path in `UC-MCP-02`

## Entry gate

Do not start V0.5 runtime implementation until V0.4 Tunnel is merged and its gate is green. The phase may be designed early, but OAuth must not be exposed remotely before the tunnel/public-surface boundary is understood and tested.

Current implementation baseline at authoring time is V0.3 on `main`; V0.4 is the immediate runtime phase before this one.

## Implementation order

```text
V0.5A Auth substrate + interoperability spike
  -> detailed auth architecture
  -> UC-AUTH-01 Pairing
  -> UC-AUTH-02 Authorize
  -> UC-AUTH-03 Token + protected MCP
  -> UC-AUTH-04 Refresh rotation
  -> UC-AUTH-05 Revoke/unpair capability
  -> V0.5 security/interoperability gate
  -> V0.6 CLI adapters
```

The spike is mandatory because MCP authorization changed materially in the 2026-07-28 specification and target-client behavior must be verified before freezing a compatibility profile.

## Non-goals

- no password grant, implicit grant, device flow or client credentials for ChatGPT browser use;
- no write/shell scope;
- no global `C2C.Security` project unless a later ADR proves it necessary;
- no database in V1: auth state follows `11-DATA-MODEL.md` local filesystem rules;
- no V0.6 CLI command implementation in this phase;
- no browser automation/control-plane implementation;
- no DCR endpoint unless interoperability evidence proves it is required.

## Read order

1. `AGENTS.md` and applicable `.agent/rules`
2. `skills/dotnet-project-conventions/SKILL.md`
3. `docs/01-architecture/06-OAUTH-PAIRING.md`
4. `docs/01-architecture/11-DATA-MODEL.md`
5. `docs/02-common/04-BR-AUTH.md`
6. target `UC-AUTH-*` and `UC-MCP-02`
7. this pack in numeric order

## Files in this pack

- `00-README.md`
- `01-BASELINE-AND-ENTRY-GATE.md`
- `02-CURRENT-SPEC-AND-INTEROP.md`
- `03-AUTH-SUBSTRATE-SPIKE.md`
- `04-DETAILED-AUTH-DESIGN.md`
- `05-UC-AUTH-01-IMPLEMENTATION.md`
- `06-UC-AUTH-02-IMPLEMENTATION.md`
- `07-UC-AUTH-03-IMPLEMENTATION.md`
- `08-UC-AUTH-04-IMPLEMENTATION.md`
- `09-UC-AUTH-05-IMPLEMENTATION.md`
- `10-MCP-AUTHORIZATION-INTEGRATION.md`
- `11-TEST-AND-SECURITY-GATE.md`
- `12-ANTIGRAVITY-EXECUTION-ORDER.md`
- `13-PHASE-DONE-AND-HANDOFF.md`
