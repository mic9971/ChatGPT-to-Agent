# UC-AUTH-05 Implementation — Revoke / Unpair Client

## Goal

Revoke one client's authorization for the current workspace and make future protected access fail according to the selected access-token validation policy.

## V0.5/V0.6 boundary

V0.5 implements revocation capability and Host/test seam. V0.6 later exposes `c2c unpair`. Do not add a temporary CLI implementation here.

## Core behavior

1. Resolve target by safe client/grant identifier scoped to the current workspace.
2. Acquire per-client/grant auth lock.
3. Mark active authorization grant revoked.
4. Revoke associated refresh families.
5. Invalidate active access as soon as practical according to the chosen token model.
6. Clear pairing/client association where applicable.
7. Persist atomically.
8. Return idempotent safe result.

## Immediate access revocation

The phase plan must explicitly state how active access tokens behave after unpair:

- if validation checks grant/token state per request, the next request must fail;
- if self-contained access tokens are accepted until expiry, document the maximum revocation delay and verify it is consistent with BR-AUTH-006;
- prefer immediate local grant-state validation because the authorization server and resource server are in the same host.

Do not claim immediate revocation unless a test proves a previously valid access token fails after revoke.

## Cross-workspace isolation

A revoke request from workspace A must never modify client/grant state owned by workspace B. Safe unknown/not-found behavior must not disclose unrelated client metadata.

## Tests

- revoke active grant;
- repeated revoke is idempotent;
- refresh fails after revoke;
- previously valid access fails after revoke according to documented policy;
- wrong workspace cannot revoke;
- concurrent revoke/refresh has deterministic fail-closed outcome;
- restart preserves revocation;
- no token values appear in audit/log output.

## Completion report

```text
STATE: EXECUTED
TARGET_UC: UC-AUTH-05
REVOCATION_ANCHOR: ...
ACTIVE_ACCESS_INVALIDATION: ...
REFRESH_FAMILY_INVALIDATION: ...
IDEMPOTENCY: ...
CROSS_WORKSPACE_TEST: ...
TESTS: ...
DEVIATIONS: ...
```
