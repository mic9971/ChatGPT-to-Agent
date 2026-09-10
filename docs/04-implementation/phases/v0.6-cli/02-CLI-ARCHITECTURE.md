# V0.6 CLI Architecture

## Boundary

`C2C.Cli` is a local presentation/orchestration adapter. It converts command-line input into calls to existing application/runtime services and converts results into human or machine output.

It must not own:

- workspace path authorization;
- tunnel provider logic;
- OAuth/token validation;
- execution artifact sanitization;
- future C2C protocol state transitions.

## Proposed project

```text
src/C2C.Cli/
  C2C.Cli.csproj
  Program.cs
  Composition/
    CliServiceCollectionExtensions.cs
    CliApplication.cs
  Output/
    CliEnvelope.cs
    CliJsonWriter.cs
    CliHumanWriter.cs
    CliExitCode.cs
  Setup/
    SetupCommand.cs
  Runtime/
    StartCommand.cs
    StopCommand.cs
    EnsureCommand.cs
  Diagnostics/
    StatusCommand.cs
    DoctorCommand.cs
    LogsCommand.cs
  Authorization/
    PairCommand.cs
    UnpairCommand.cs
  Execution/
    RecordCommand.cs
```

Exact file names can vary if the agent proves a simpler shape, but capability grouping is mandatory.

## Command parsing

Use a maintained .NET CLI parser such as `System.CommandLine` rather than hand-parsing `args`. Pin the selected stable package centrally in `Directory.Packages.props` after verifying compatibility with the repository target framework.

Requirements:

- unknown command/option returns usage error, never internal exception;
- mutually exclusive options are validated before service invocation;
- cancellation from Ctrl+C propagates;
- no shell interpolation;
- all filesystem values remain data, never command fragments.

## Composition

Prefer one composition root for the CLI. Commands receive narrow service dependencies instead of resolving `IServiceProvider` directly.

```text
Program
  -> parse command
  -> resolve bounded command handler
  -> call Core/Infrastructure service
  -> map OperationResult/domain status
  -> output writer
  -> stable exit code
```

## Runtime orchestration

V0.6 introduces a local runtime-control capability because `c2c start` and `c2c stop` manage the `C2C.Host` process itself.

Recommended contracts:

```csharp
public interface IRuntimeController
{
    Task<OperationResult<RuntimeStartResult>> StartAsync(RuntimeStartRequest request, CancellationToken ct);
    Task<OperationResult> StopAsync(RuntimeStopRequest request, CancellationToken ct);
}

public interface IRuntimeDiagnostics
{
    Task<RuntimeStatusSnapshot> GetStatusAsync(CancellationToken ct);
    Task<DoctorReport> RunDoctorAsync(CancellationToken ct);
}
```

These are runtime/application contracts, not CLI contracts. Place them under a business-capability owner such as `C2C.Core/Runtime/` if no existing owner fits cleanly.

## Bridge process controller

The current V0.4 `LoopbackBridgeRuntime` is a health/readiness probe for an already-running host. Do not mutate it into a catch-all daemon manager.

Prefer a dedicated infrastructure component for background bridge ownership:

```text
C2C.Infrastructure/Runtime/
  BridgeProcessController
  RuntimeStateStore
  RuntimeProcessValidator
```

It may reuse the **semantics** of the V0.4 owned-process implementation. If code duplication would be required, first evaluate whether the existing owned-process abstraction has now earned promotion into a neutral runtime/process capability. Any move must be bounded and all V0.4 tests must remain green.

## Runtime ownership record

Persist only safe local metadata outside the workspace, for example:

```text
workspaceId
runtimeInstanceId
pid
processStartTime
executableFingerprint or normalized executable identity
localEndpoint
startedAt
schemaVersion
```

PID alone never proves ownership.

## Start flow

```text
c2c start
  -> load/validate workspace binding
  -> acquire lifecycle lock
  -> inspect persisted runtime state
  -> validate existing process ownership + health
  -> if healthy owned instance: idempotent success
  -> if foreign/conflicting listener: fail closed
  -> launch C2C.Host with typed arguments/env
  -> wait bounded health readiness
  -> optionally ensure tunnel only when command/profile explicitly requests remote mode
  -> persist runtime state atomically
  -> return result
```

Default `setup` must not imply remote exposure. `start` may use configured profile, but any transition from local-only to remote exposure must be explicit/configured and visible in output.

## Stop flow

```text
c2c stop
  -> lifecycle lock
  -> load owned runtime state
  -> validate process identity
  -> stop tunnel first if owned/managed
  -> graceful host shutdown when supported
  -> bounded wait
  -> force terminate only the validated owned process if policy allows
  -> clear/mark runtime state atomically
```

Never kill by process name.

## Status vs Doctor vs Ensure

- `status`: cheap, read-only snapshot; no repair.
- `doctor`: deeper bounded diagnostics; still no mutation.
- `ensure`: mutating convergence command for agents; may start/recover bridge/tunnel but never creates credentials or bypasses pairing.

Keep these semantics distinct.

## Config precedence

Normative precedence:

```text
CLI argument
  > approved environment variable
  > workspace app-state config
  > global app-state default
```

Repository files must not silently override security-sensitive global settings.

## Security rule

No CLI command may become a hidden general-purpose local execution API. V0.6 commands are a closed allowlist with typed parameters and bounded behavior.
