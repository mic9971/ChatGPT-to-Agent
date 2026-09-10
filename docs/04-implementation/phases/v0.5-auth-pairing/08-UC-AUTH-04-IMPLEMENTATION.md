# UC-AUTH-04 Implementation — Rotate Refresh Token

## Goal

Provide refresh-token renewal with single-use rotation and replay denial while preserving filesystem-only V1 persistence.

## Required state

Each refresh token belongs to a bounded token family:

```text
familyId
grantId
workspaceId
clientId
issuer
resource
currentGeneration
currentTokenHash
expiresAt
status
```

Raw refresh token is never persisted in ordinary JSON or logs.

## Rotation transaction

Under a per-family lock:

1. load family state;
2. validate active grant/family;
3. validate expiry, workspace/client/issuer/resource;
4. verify presented refresh token against current protected/hash state;
5. if predecessor/replayed token is detected, deny and mark/audit safely according to policy;
6. consume current generation;
7. create successor generation/token;
8. atomically persist successor state;
9. issue new access token and new refresh token;
10. release lock.

Exactly one of two concurrent redemption attempts for the same token may succeed.

## Replay policy

Minimum V1 behavior:

- old token is unusable after successful rotation;
- replay is logged only as safe category/client/family id;
- replay never prints token material;
- a detected replay may revoke the token family if selected by explicit policy; document the choice and test it.

## Cleanup

Bound token-family history and expired state. Cleanup must not race with redemption into an accidental allow. Unknown/corrupt state fails closed.

## Tests

- normal refresh rotates token;
- old token fails after rotation;
- two concurrent refresh calls -> exactly one success;
- revoked grant blocks refresh;
- expired family blocks refresh;
- wrong client/workspace/issuer/resource blocks refresh;
- restart preserves rotation/replay semantics;
- corrupt refresh state fails closed;
- raw refresh value absent from disk/logs;
- `offline_access` behavior matches discovery metadata/profile.

## Completion report

```text
STATE: EXECUTED
TARGET_UC: UC-AUTH-04
ROTATION_MODEL: ...
CONCURRENCY_LOCK: ...
REPLAY_POLICY: ...
PERSISTED_TOKEN_FORM: ...
CLEANUP_POLICY: ...
TESTS: ...
DEVIATIONS: ...
```
