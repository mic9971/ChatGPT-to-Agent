# Common Test Rules

1. Every use case has happy-path, boundary/failure and authorization/security tests where relevant.
2. Security-critical path/pairing/token/state-machine behavior is unit-tested exhaustively and additionally exercised through an integration boundary.
3. No test depends on the developer's home directory, current Git repository, local timezone or Internet unless explicitly classified as smoke/manual.
4. Use temporary workspaces and controlled symlink/junction fixtures for filesystem tests.
5. Use injected `TimeProvider`; do not sleep to test expiry/backoff.
6. Process/tunnel/network dependencies use adapters and deterministic fakes for normal CI. Real cloud/tunnel tests are separate smoke suites.
7. A regression fix adds a test that fails before the fix and passes after it.
8. Do not weaken a test assertion merely to match implementation behavior; reconcile implementation with the approved use-case/BR contract.
