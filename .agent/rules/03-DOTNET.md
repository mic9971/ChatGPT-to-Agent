# .NET Engineering Rules

Use `skills/dotnet-engineering/SKILL.md` as the detailed runtime/.NET engineering skill.
Use `skills/csharp-clean-code/SKILL.md` for language-level C# style/clean-code rules.
Use `skills/dotnet-project-conventions/SKILL.md` whenever adding, moving, renaming, reviewing, or designing projects, folders, models, interfaces, DTOs, use-case files, tests, DI registration, or shared/common code.

Mandatory highlights:
- .NET 10 / C# 14 baseline unless project file says otherwise.
- Nullable enabled; no blanket suppressions.
- Async I/O all the way; propagate `CancellationToken`.
- Inject `TimeProvider` for expiry/retry/time tests.
- No `.Result`/`.Wait()` in async/request flow.
- No service locator or `BuildServiceProvider()` during DI registration.
- Strongly typed validated options.
- Public contracts explicit/versionable; infrastructure types do not leak into Core.
- Organize new code by owning business capability/use case inside the approved project boundary; do not create global technical dumping folders.
- Classify new models before placement; transport/provider/persistence shapes do not become Core models by convenience.
- `Common`/shared code must be earned by stable semantic reuse across real capabilities.
- No speculative abstractions without a current UC need.
