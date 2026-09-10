# V0.5 Baseline and Entry Gate

## Baseline at authoring time

`main` currently contains V0.1 Workspace/MCP, V0.2 Git Evidence and V0.3 Execution Evidence. The MCP host exposes the nine intended read-only V1 tools. Runtime still targets `net8.0` / C# 12 in `Directory.Build.props`; V0.5 must not silently retarget the framework as part of auth work.

Current project shape is intentionally small:

```text
src/
  C2C.Core
  C2C.Infrastructure
  C2C.Host

tests/
  C2C.Core.Tests
  C2C.IntegrationTests
```

V0.5 should extend this shape by capability:

```text
C2C.Core/Authorization/
C2C.Infrastructure/Authorization/
C2C.Host/Auth/
tests/.../Authorization/
```

Do not create a new project merely because auth is security-sensitive. Add a project only through an explicit ADR showing a real dependency/isolation need.

## Mandatory V0.4 predecessor gate

Before runtime V0.5 begins, verify:

```text
[ ] V0.4 Tunnel merged to main
[ ] bridge remains loopback-only
[ ] public tunnel endpoint is HTTPS
[ ] tunnel cannot expose local-admin-only routes
[ ] owned process/session lifecycle tests pass
[ ] full solution build/test is green
```

If V0.4 is not complete, V0.5 stays DESIGN-ONLY.

## V0.5 preflight

At implementation start the agent must inspect actual `main`, not trust this snapshot. It must report:

- target framework and package versions;
- actual MCP SDK version;
- current tunnel/public endpoint shape;
- existing app-state path/store abstractions;
- whether any auth code already exists;
- current test count and baseline result.

Run:

```text
dotnet build C2CNet.sln
dotnet test C2CNet.sln
```

A failing pre-existing baseline blocks auth implementation.

## Design consistency checks

V1 data model says local filesystem app-state, not a database. Therefore adding EF Core/SQLite merely because an OAuth library defaults to database-backed storage is not allowed without an approved ADR.

V0.6 owns CLI commands. In V0.5, pairing/unpair functionality is implemented as Core/Infrastructure capability plus Host/test seams. `c2c pair` and `c2c unpair` adapters are completed in V0.6; do not create a temporary CLI project in V0.5.

## Exit from entry gate

Only proceed to the auth substrate spike when:

```text
STATE: V0_5_ENTRY_READY
V0_4_GATE: PASS
BASELINE_BUILD: PASS
BASELINE_TESTS: PASS
AUTH_EXISTING_CODE: none | documented
FRAMEWORK_CHANGE_REQUIRED: no
```
