# .NET Engineering Rules

Use `skills/dotnet-engineering/SKILL.md` as the detailed coding skill.

Mandatory highlights:
- .NET 10 / C# 14 baseline unless project file says otherwise.
- Nullable enabled; no blanket suppressions.
- Async I/O all the way; propagate `CancellationToken`.
- Inject `TimeProvider` for expiry/retry/time tests.
- No `.Result`/`.Wait()` in async/request flow.
- No service locator or `BuildServiceProvider()` during DI registration.
- Strongly typed validated options.
- Public contracts explicit/versionable; infrastructure types do not leak into Core.
- No speculative abstractions without a current UC need.
