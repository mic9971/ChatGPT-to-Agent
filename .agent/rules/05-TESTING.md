# Testing Rules

- Follow `docs/02-common/08-COMMON-TEST-RULES.md` and the target UC Test Design.
- Path/security state-machine rules require adversarial cases.
- ASP.NET/MCP/Auth endpoints require integration tests, not only mocked unit tests.
- Process/tunnel tests use deterministic fakes in CI; real external smoke tests are separate.
- No sleeps for time; use `TimeProvider`.
- Every bug fix adds a regression test.
