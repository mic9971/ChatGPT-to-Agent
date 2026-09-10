# V0.6A — UC-CLI-01 Setup + UC-CLI-03 Status/Doctor

## Why these come first

These commands establish the CLI executable, composition root, output contract and safe diagnostics before any runtime mutation is added.

## UC-CLI-01 — Setup

### Required reuse

- existing `IWorkspaceConfigurator` / workspace store and canonicalization rules;
- existing app-state path policy;
- existing dependency/config models where available.

The command must not reproduce workspace path validation in CLI code.

### Suggested flow

```text
parse args
 -> resolve workspace input (default current directory only when explicit design allows)
 -> call workspace configurator
 -> validate app-state/runtime directory permissions
 -> detect Git/cloudflared/host prerequisites
 -> write safe config only through existing stores
 -> optional local self-check
 -> return stable result
```

### Default behavior

`c2c setup` configures local state only. It must not silently create remote credentials, pair a client or expose a public tunnel.

If future UX offers `--start` or `--remote`, those transitions are explicit flags and delegate to the corresponding services.

### Expected files

```text
src/C2C.Cli/
  Setup/SetupCommand.cs
  Output/*
  Composition/*
```

Only create a Core setup orchestrator if command logic would otherwise contain real policy/orchestration beyond simple adapter composition.

### Tests

- same workspace setup is idempotent;
- conflicting rebind remains denied by existing workspace policy;
- path with spaces/unicode/metacharacters is treated as data;
- missing optional dependency returns warning/degraded, not crash;
- JSON contains no canonical absolute path by default;
- no tunnel/auth side effect on default setup.

## UC-CLI-03 — Status

`status` is cheap and read-only.

Expected observations:

```text
workspace configured?
bridge process/state + health
tunnel session + health
authorization/pairing readiness
app-state health
```

The command must not start/stop/repair anything.

### Status service

Prefer a single application/runtime diagnostics contract such as:

```csharp
Task<RuntimeStatusSnapshot> GetStatusAsync(CancellationToken ct);
```

The snapshot contains typed states; CLI maps them to JSON/human output.

## UC-CLI-03 — Doctor

Doctor performs deeper bounded checks but remains read-only.

Initial checks:

```text
runtime.dotnet
workspace.config
workspace.permissions
workspace.policy
git.binary
host.binary
bridge.port
bridge.health
tunnel.provider
tunnel.state
auth.metadata
auth.pairing
appstate.integrity
logs.writable
```

Independent checks may run concurrently, each with its own timeout. Aggregate severity must be deterministic.

### Severity

```text
PASS     -> no action
WARN     -> optional/degraded capability
ERROR    -> required capability unavailable or invalid
BLOCKED  -> security/ownership/config conflict requiring user action
```

Doctor never performs repair automatically.

### Security

Do not print:

- auth tokens/codes/verifiers;
- environment variable values;
- full provider command line if it can include credentials;
- raw app-state security files;
- arbitrary absolute paths in machine output.

### Completion gate V0.6A

```text
[ ] C2C.Cli builds as a dedicated executable
[ ] help/usage deterministic
[ ] --json stdout contains one JSON object only
[ ] stable exit mapper implemented once
[ ] setup delegates to workspace services
[ ] status is non-mutating
[ ] doctor is non-mutating and bounded
[ ] secret-output capture tests green
[ ] full solution build/test green
```
