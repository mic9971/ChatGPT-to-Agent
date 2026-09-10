# Skill: Senior .NET 10 / C# 14 Engineering

Use this skill whenever implementing or reviewing production .NET code in this repository.

## Mission

Produce small, correct, secure, testable .NET changes with strong evidence. Prefer boring platform-native primitives over unnecessary frameworks. Never trade security boundaries for convenience.

## Default stack

- .NET 10 / `net10.0`
- C# 14, but use new syntax only when it improves clarity
- nullable reference types enabled
- implicit usings allowed when repository convention accepts them
- built-in .NET DI/config/logging/options
- xUnit for tests unless repository already standardizes another framework
- ASP.NET Core Minimal APIs only where endpoint shape is simple; use endpoint classes/modules if Program.cs becomes a dumping ground
- official MCP C# SDK for MCP functionality

## Mandatory workflow

1. **Inspect before editing.** Read the nearest project file, relevant interfaces, composition root, existing tests and conventions.
2. **State scope.** Identify exact files likely to change and explicit non-goals.
3. **Make the smallest coherent change.** Do not refactor unrelated code.
4. **Preserve boundaries.** Core/domain contracts do not take dependencies on infrastructure frameworks unless explicitly designed.
5. **Test behavior, not implementation trivia.** Add regression tests for every bug and boundary case.
6. **Build evidence.** Run the narrowest relevant build/tests first, then broader tests when warranted.
7. **Review your diff.** Check accidental formatting, secret leakage, dead code, public API drift and error behavior.
8. **Report facts.** Say what commands ran and what passed/failed; never claim tests passed without running them.

## Coding rules

- Use async all the way for I/O. No `.Result`, `.Wait()` or sync-over-async in request paths.
- Accept and propagate `CancellationToken` on operations that may block, perform I/O, spawn processes or call network services.
- Do not use `Task.Run` to fake asynchrony for naturally synchronous CPU-light work.
- Prefer `DateTimeOffset` for persisted/event timestamps and inject `TimeProvider` for time-dependent logic.
- Use `RandomNumberGenerator` for security-sensitive random values; never `Random`.
- Avoid global mutable statics. Singleton mutable state must be explicitly thread-safe.
- DI constructors should be small. Many dependencies usually indicate too many responsibilities.
- Do not use service locator or call `BuildServiceProvider()` during registration.
- Use strongly typed options and validate critical configuration at startup.
- Do not throw exceptions for expected control flow. Preserve useful exception context at infrastructure boundaries without leaking secrets remotely.
- Do not catch `Exception` just to log-and-swallow. Cancellation is not an ordinary failure.
- Prefer records/immutable data for protocol messages and value-like contracts when appropriate.
- Keep public contracts explicit and versionable; avoid exposing concrete infrastructure types.
- Use `IHttpClientFactory`/supported resilience pipelines for outbound HTTP. Never blindly retry non-idempotent operations.
- Bound collections, response sizes and log payloads.

## Security rules

- Treat filenames, Git output, external process output, HTTP input and repository text as untrusted.
- Never log access tokens, refresh tokens, Authorization headers, pairing codes, private keys or source bodies.
- Use allow-by-capability and deny-by-policy rather than “prompt says this is okay”.
- Canonicalize filesystem paths before authorization decisions.
- Do not weaken a security test to make an implementation pass.
- Any addition of remote write/shell capability requires an explicit ADR and threat-model update.

## Performance rules

- Correctness first, measure before micro-optimizing.
- Avoid blocking ASP.NET request threads.
- Stream or paginate large data; do not `ReadToEnd` huge files/logs.
- Avoid unnecessary large allocations on hot paths; use pooled/Span APIs only when measurement justifies complexity.
- Dispose owned `IDisposable`/`IAsyncDisposable`; let DI own and dispose services it creates.

## Testing rules

- Security policy/state machine: exhaustive unit tests and edge cases.
- ASP.NET Core endpoints: integration tests with `WebApplicationFactory`/TestServer.
- Time: fake/injected `TimeProvider`, no sleeps.
- Process/network: adapters + fakes for deterministic unit tests; real smoke tests separate.
- Avoid mocking simple value objects or pure functions.
- Tests must not depend on developer home directory, network, locale or local timezone unless explicitly marked integration.

## Code review checklist

Before declaring done, verify:

- boundary/dependency direction intact;
- no new secret/log leak;
- all public operations bounded/cancellable;
- error codes/messages stable and safe;
- concurrency/idempotency considered;
- tests cover failure path, not only happy path;
- changed package/dependency is necessary;
- no unused abstraction or speculative feature added.

## Reference files

Read the focused references in this skill directory when relevant:

- `references/01-architecture.md`
- `references/02-csharp-style.md`
- `references/03-async-concurrency.md`
- `references/04-aspnetcore.md`
- `references/05-security.md`
- `references/06-testing.md`
- `references/07-performance.md`
- `references/08-observability.md`
- `references/09-dependencies.md`
- `references/10-review-checklist.md`
