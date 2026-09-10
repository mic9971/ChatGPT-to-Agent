# V0.5 Test and Security Gate

## Test layers

### Core/unit

Cover pure policy/state transitions:

- scope allowlist/tool mapping;
- pairing state transitions;
- expiry/attempt budget with fake time;
- grant workspace/client/resource binding;
- refresh-family rotation/replay;
- revoke idempotency;
- corrupt-state fail-closed behavior.

### Infrastructure

Cover filesystem and network boundaries:

- atomic auth-state writes;
- concurrent writers/locks;
- restart/reload;
- user-only file permissions where testable;
- CIMD bounded HTTP fetch, timeout and SSRF defenses;
- no raw secret persistence.

### Host/integration

Cover real ASP.NET + selected OAuth library:

- discovery metadata;
- authorization code + PKCE;
- token exchange;
- refresh;
- revoke;
- protected `/mcp` calls;
- 401/403 challenges;
- every tool scope mapping.

## Security matrix

| Attack / failure | Expected result |
|---|---|
| missing token | 401 + safe resource metadata challenge |
| expired token | 401 |
| wrong issuer | 401 |
| wrong MCP resource/audience | 401 |
| token for workspace A used on B | deny before capability invocation |
| missing scope | 403 + exact minimum scope |
| scope escalation request | authorization denied |
| missing/`plain` PKCE where S256 required | denied |
| redirect mismatch | denied |
| pairing expired | denied |
| pairing brute-force attempt budget exhausted | locked/denied |
| pairing used twice | exactly one success |
| auth code replay | denied |
| refresh token replay | denied; exactly one concurrent success |
| revoked grant refresh | denied |
| old access after revoke | denied according to documented immediate-revocation policy |
| corrupt auth JSON | fail closed, no silent reset |
| CIMD HTTP URL | denied |
| CIMD private/link-local/loopback destination | denied by default |
| CIMD oversized body | denied |
| CIMD redirect to private address | denied |
| CIMD `client_id` mismatch | denied |
| DCR endpoint when compatibility disabled | absent/denied |
| token/header in logs | canary absent |

## Secret canaries

Integration tests should use synthetic values and assert they never appear in logs/errors/persisted ordinary state, for example:

```text
C2C_TEST_ACCESS_TOKEN_CANARY_73af
C2C_TEST_REFRESH_TOKEN_CANARY_b162
C2C_TEST_PAIR_CODE_91d3
C2C_TEST_PKCE_VERIFIER_44ae
```

Never use real provider credentials in CI.

## Concurrency tests

At minimum:

- concurrent pairing create;
- concurrent pairing consume;
- concurrent auth-code redeem if library seam allows deterministic exercise;
- concurrent refresh redeem;
- revoke racing refresh;
- state writer restart/crash simulation where existing test utilities support it.

## Manual interoperability lane

CI must not depend on ChatGPT credentials. A manual smoke, after V0.4 tunnel exists, verifies current target-client behavior without recording secrets.

The manual smoke is evidence for compatibility, not a replacement for deterministic protocol tests.

## Phase gate

V0.5 passes only when:

```text
[ ] auth substrate decision accepted
[ ] no unsafe custom OAuth crypto/protocol shortcut
[ ] PRM + AS discovery accurate
[ ] PKCE S256 enforced
[ ] issuer/resource/workspace/client binding proven
[ ] pairing one-time + TTL + attempts proven
[ ] authorization code one-time proven
[ ] all 9 MCP tools protected with exact scope mapping
[ ] refresh rotation/replay proven when refresh enabled
[ ] revoke/unpair behavior proven
[ ] CIMD SSRF gate green if CIMD advertised
[ ] DCR remains off unless explicit compatibility ADR
[ ] no secret canary leakage
[ ] full build/tests green
[ ] no V0.6/V0.7 runtime work included
```
