# V0.5 Antigravity Execution Order

## Operating rule

V0.5 is security-sensitive. Work one bounded slice at a time. Every plan must be reviewed before implementation if it changes auth library profile, persistence, client registration, token format, public endpoints, or scope semantics.

## Step 0 — Entry verification

```text
Verify V0.5 entry only. Do not modify files.

Read AGENTS.md, applicable rules/skills, V0.5 phase pack and current main.
Confirm V0.4 Tunnel gate is merged/green before runtime auth work.
Inspect target framework, package versions, current public endpoint, app-state stores and existing auth code.
Run dotnet build C2CNet.sln and dotnet test C2CNet.sln.
Return V0_5_ENTRY_READY or BLOCKED and stop.
```

## Step 1 — V0.5A auth substrate spike PLAN

```text
Prepare PLAN for V0.5A Auth Substrate/Interoperability Spike only.
Do not create production auth endpoints.

Evaluate current stable OpenIddict server/validation integration against:
- current repository target framework;
- filesystem-only V1 persistence;
- authorization code + PKCE S256;
- issuer (`iss`) requirements;
- MCP resource binding;
- protected-resource metadata;
- authorization-server metadata;
- refresh behavior;
- CIMD-first registration direction;
- DCR disabled by default.

Return exact prototype/test files, questions to prove, risks and stop conditions.
Do not implement UC-AUTH-01 yet.
```

## Step 2 — Execute spike

Implement only the approved test/prototype spike. No public production exposure. Return the decision report required by `03-AUTH-SUBSTRATE-SPIKE.md`.

If `RECOMMENDATION != PROCEED`, stop.

## Step 3 — UC-AUTH-01 Pairing

Plan, review, then implement `UC-AUTH-01` only using `05-UC-AUTH-01-IMPLEMENTATION.md`.

Gate:

```text
[ ] CSPRNG code
[ ] TTL + attempt cap
[ ] one active unambiguous session
[ ] atomic store
[ ] no display code in persisted ordinary state/logs
[ ] no CLI project added
```

## Step 4 — UC-AUTH-02 Authorize

Plan first. Explicitly explain PKCE, issuer/resource/client/redirect checks, pairing consume atomicity and registration profile. Implement only after plan acceptance.

If plan adds DCR without interoperability evidence + ADR, reject it.

## Step 5 — UC-AUTH-03 Token + protected MCP

Plan and implement token exchange/validation plus `UC-MCP-02` protected-call integration.

Before approval require exact scope matrix for all nine tools and proof authorization fails before service invocation.

## Step 6 — UC-AUTH-04 Refresh rotation

Plan and implement single-use refresh rotation. Exactly one concurrent redemption succeeds. No raw refresh token at rest/logs.

## Step 7 — UC-AUTH-05 Revoke/unpair capability

Plan and implement revocation capability without V0.6 CLI. Prove previously-authorized future access is denied according to documented policy.

## Step 8 — Final phase review

```text
Review V0.5 against:
- UC-AUTH-01..05
- UC-MCP-02
- BR-AUTH-001..008
- referenced BR-SEC/BR-CON/BR-OBS
- 2026-07-28 MCP auth direction
- filesystem-only V1 data model
- 10-TEST-AND-SECURITY-GATE.md

Run full build/tests.
Return PASS or BLOCKED with exact missing criteria.
Do not start V0.6.
```

## Stop conditions

Antigravity must stop rather than improvise when:

- V0.4 is not complete;
- OpenIddict/custom-store boundary is unclear;
- implementation would silently add a database;
- implementation would hand-roll PKCE/token cryptography unnecessarily;
- client registration profile is guessed rather than evidenced;
- CIMD can reach arbitrary/private network targets;
- DCR is proposed without explicit compatibility evidence;
- issuer/resource/workspace binding cannot be proven;
- any bearer/refresh/pairing/PKCE secret appears in logs;
- auth failure still invokes an MCP capability;
- implementation requires V0.6/V0.7 scope.
