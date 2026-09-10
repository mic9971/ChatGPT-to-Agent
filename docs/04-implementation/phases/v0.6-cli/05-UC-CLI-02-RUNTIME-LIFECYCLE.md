# V0.6B — UC-CLI-02 Start and Stop Runtime

## Goal

Provide reliable `c2c start` / `c2c stop` for the owned `C2C.Host` runtime and optional tunnel without killing foreign processes or creating duplicate runtimes.

## Existing V0.4 behavior to reuse

V0.4 already has Tunnel process ownership, `ITunnelService`, `IRuntimeEnsurer`, `IBridgeRuntime`, lifecycle locking and health concepts. Preserve their tests and semantics.

The existing `LoopbackBridgeRuntime` is a probe/readiness adapter for an already-running host. It is not sufficient for a CLI that exits after launching a background host.

## New runtime contracts

Recommended:

```csharp
public interface IRuntimeController
{
    Task<OperationResult<RuntimeStartResult>> StartAsync(
        RuntimeStartRequest request,
        CancellationToken cancellationToken);

    Task<OperationResult> StopAsync(
        RuntimeStopRequest request,
        CancellationToken cancellationToken);
}
```

Suggested models:

```text
RuntimeStartRequest
- workspaceId
- localEndpoint/profile
- ensureTunnel

RuntimeStartResult
- runtimeInstanceId
- bridge status
- local endpoint
- tunnel status
- public endpoint?
- alreadyRunning
```

Do not put CLI formatting fields in Core runtime models.

## Background host process

Infrastructure owns process creation.

Requirements:

- executable path resolved explicitly;
- typed `ProcessStartInfo.ArgumentList` or equivalent;
- no shell;
- bounded startup timeout;
- stdout/stderr never left as unbounded deadlock-prone pipes;
- host can outlive the short CLI invocation;
- ownership metadata persisted only after successful readiness;
- failed startup cleans up only the process created by that attempt.

Packaging-specific service managers are out of scope until V0.9 unless required for correctness.

## Ownership proof

Persist and validate more than PID:

```text
workspaceId
runtimeInstanceId
pid
processStartTime
executable identity/fingerprint
expected local endpoint
startedAt
schemaVersion
```

On `stop`, a PID that exists but fails identity validation is foreign. Return conflict and do not kill it.

## Start algorithm

```text
Acquire runtime lifecycle lock
 -> load runtime ownership record
 -> if record exists:
      validate process identity
      probe bridge health
      if owned + healthy => idempotent success
      if stale => mark/clean stale metadata only
      if identity mismatch => conflict
 -> check endpoint ownership/conflict
 -> launch host
 -> wait bounded health probe
 -> persist ownership atomically
 -> if remote profile requested, ensure tunnel
 -> return status
```

If tunnel startup fails after bridge is healthy, report precise degraded/error state according to profile. Do not automatically kill a healthy local bridge unless the selected profile explicitly requires all-or-nothing startup.

## Stop algorithm

```text
Acquire lifecycle lock
 -> load ownership record
 -> if none => idempotent stopped
 -> validate process identity
 -> if mismatch => conflict, foreign untouched
 -> stop owned tunnel first
 -> request graceful bridge shutdown if supported
 -> bounded wait
 -> force terminate only validated owned process when allowed
 -> verify stopped
 -> remove/update ownership record atomically
```

## Endpoint conflict

A listening port is not proof that C2C owns it. If the port is occupied and no valid ownership record/identity exists, return `C2C_CONFLICT`.

## Reuse vs refactor of owned-process primitives

Tunnel and bridge lifecycle now both need process ownership validation. Do not copy `SystemOwnedProcessRunner` logic blindly.

At implementation PLAN time, inspect whether the V0.4 abstractions are semantically generic enough. If yes, promote them to a neutral runtime/process capability in one bounded refactor with all V0.4 tests preserved. If not, keep separate implementations and document why their semantics differ.

Do not move files merely for aesthetics.

## Errors

At minimum:

```text
C2C_CONFLICT
C2C_TIMEOUT
C2C_NOT_READY
RUNTIME_START_FAILED
RUNTIME_STOP_FAILED
DEPENDENCY_UNAVAILABLE
```

Reuse an existing stable common error when semantically identical.

## Tests

- repeated start => one owned host;
- concurrent starts => one owner/one process;
- repeated stop => success;
- start timeout cleans only newly-created process;
- foreign process on configured port untouched;
- stale PID metadata safely recovered;
- PID reuse simulation denied;
- tunnel failure does not corrupt bridge ownership;
- Ctrl+C/cancellation does not leave ambiguous metadata;
- path/executable arguments containing spaces remain typed data;
- no wildcard/public bridge binding introduced.

## Completion gate

```text
[ ] one healthy runtime after repeated/concurrent start
[ ] foreign process never terminated
[ ] ownership record atomic and restart-safe
[ ] stop verifies identity before termination
[ ] tunnel sequencing correct
[ ] all V0.4 tests remain green
[ ] full build/test green
```
