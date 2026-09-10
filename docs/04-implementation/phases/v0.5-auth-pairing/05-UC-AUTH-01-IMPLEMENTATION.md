# UC-AUTH-01 Implementation — Create Pairing Session

## Goal

Implement a local pairing capability that creates a short-lived, one-time approval code for the bound workspace without introducing the V0.6 CLI adapter yet.

## Expected placement

```text
src/C2C.Core/Authorization/
  PairingSession.cs
  PairingStatus.cs
  PairingOptions.cs
  PairingCreateResult.cs
  IPairingService.cs
  IPairingStore.cs

src/C2C.Infrastructure/Authorization/
  PairingService.cs
  JsonPairingStore.cs
  AuthorizationServiceCollectionExtensions.cs

tests/C2C.Core.Tests/Authorization/
  PairingServiceTests.cs
  JsonPairingStoreTests.cs
```

Names are illustrative; preserve nearby conventions and avoid redundant abstractions.

## Algorithm

1. Validate current workspace identity and auth profile.
2. Acquire workspace-auth pairing lock.
3. Reload pairing state under lock.
4. Expire stale active session(s).
5. Generate code using `RandomNumberGenerator` with enough entropy for the configured TTL/attempt budget.
6. Compute stored verifier/hash representation.
7. Persist one active session with expiry/attempt count atomically.
8. Return display code once to the local caller.
9. Never log/serialize the display code into ordinary app-state/logs.

## Default policy

- TTL: 5 minutes unless current design/config says otherwise.
- one active session per workspace by default;
- explicit create rotates/replaces prior active session;
- bounded failed attempts;
- consumed/locked/expired session cannot become active again;
- `TimeProvider` used for deterministic expiry tests.

## V0.5/V0.6 boundary

Do not create `C2C.Cli` in this UC. V0.5 tests the pairing capability through service/integration seams. V0.6 later maps `c2c pair` to it.

## Security tests

- generated codes are not repeated in a deterministic test sample;
- expiry is exact under fake time;
- attempt exhaustion locks the session;
- concurrent create leaves one unambiguous active session;
- code is absent from logs and persisted JSON;
- corrupt pairing state fails closed;
- workspace A pairing cannot approve workspace B.

## Completion report

```text
STATE: EXECUTED
TARGET_UC: UC-AUTH-01
FILES_CHANGED: ...
PAIRING_FORMAT: ...
TTL_AND_ATTEMPTS: ...
PERSISTED_SECRET_FORM: ...
CONCURRENCY: ...
TESTS: ...
DEVIATIONS: ...
```

Stop after UC-AUTH-01. Do not implement authorization endpoints yet.
