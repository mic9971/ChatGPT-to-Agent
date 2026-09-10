# Test Structure

## Mirror behavior/capability

Test project boundaries may remain technical (`Core.Tests`, `IntegrationTests`, `Security.Tests`), but folders inside should mirror the production capability/use case.

Example:

```text
tests/C2C.Core.Tests/
└── Workspace/
    ├── WorkspaceConfiguratorTests.cs
    ├── WorkspaceAccessPolicyTests.cs
    └── ReadFileTests.cs
```

Do not create global test buckets such as `ServiceTests/`, `HandlerTests/`, or `ModelTests/` that mix capabilities.

## Test at the right boundary

- pure policy/value/state -> unit test;
- real filesystem/path behavior -> filesystem-backed integration/security fixture where needed;
- ASP.NET/MCP/Auth endpoint -> host integration test;
- process/tunnel/network provider -> deterministic fake for CI plus separately marked smoke test.

## Naming

Follow the nearest test naming convention. If none exists, use behavior-oriented names that communicate operation, condition, and expected outcome.

## Regression locality

A bug fix adds the regression test nearest the capability that owns the failed invariant. Do not create a new “RegressionTests” dumping folder.

## Shared fixtures

Share fixtures only when semantics/setup are genuinely common. Keep capability-specific fixtures under that capability even if the fixture helper could technically be reused.