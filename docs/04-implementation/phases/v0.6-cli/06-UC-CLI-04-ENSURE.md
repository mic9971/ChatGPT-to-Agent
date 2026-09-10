# V0.6B — UC-CLI-04 Ensure Runtime Ready

## Goal

Make `c2c ensure --json` the primary execution-agent readiness command. It converges infrastructure when safe and returns a precise action requirement when human/auth intervention is needed.

## Existing service

V0.4 already provides `IRuntimeEnsurer` and `RuntimeEnsureResult`. V0.6 should adapt and complete that state machine rather than create a second ensure implementation inside the CLI.

After V0.5, the pairing/auth readiness probe must be backed by real authorization state.

## Command contract

```text
c2c ensure --json
```

No interactive prompt is allowed in agent JSON mode.

Expected high-level results:

```text
ready
not_configured
pairing_required
repair_required
degraded
conflict
failed
cancelled
```

## Flow

```text
load workspace binding
 -> if missing: NOT_CONFIGURED / run_setup
 -> invoke runtime ensure
 -> bridge convergence
 -> tunnel convergence according to configured profile
 -> auth/pairing readiness probe
 -> map typed result to CLI envelope + exit code
```

## Required semantics

### Idempotency

Agents are expected to call `ensure` repeatedly, including before every C2C task. Repeated calls on a ready runtime must not restart healthy bridge/tunnel processes.

### No auth bypass

If authorization requires pairing:

```text
status = pairing_required
actionRequired = run_pair
exit = 3
```

Do not create credentials, approve pairing or downgrade auth automatically.

### Local-only/degraded mode

If the selected configuration explicitly allows local-only operation, tunnel/auth unavailability can map to `degraded` with exact capability states. It must never be mislabeled `ready` for a workflow that requires remote ChatGPT MCP access.

### Repair required

Missing required executable/config corruption/stale endpoint that cannot be safely self-healed should return `repair_required`, not endless retry.

## Overall timeout

`ensure` has an overall deadline in addition to bounded lower-level bridge/tunnel probes. Cancellation propagates through every service call.

No infinite retry/backoff loop.

## Concurrency

Concurrent `ensure` calls must converge through existing lifecycle locks/ownership semantics. The CLI must not add a second unsynchronized startup path.

## JSON example

```json
{
  "schemaVersion": 1,
  "command": "ensure",
  "status": "pairing_required",
  "errorCode": "C2C_NOT_READY",
  "actionRequired": "run_pair",
  "data": {
    "bridge": "healthy",
    "tunnel": "healthy",
    "authorization": "pairing_required",
    "publicEndpoint": "https://example.trycloudflare.com"
  },
  "warnings": []
}
```

## Tests

- no workspace => deterministic setup action;
- healthy runtime => no new process;
- stopped bridge => starts once;
- stopped tunnel => recovers according to profile;
- missing pairing => action required, no credential creation;
- concurrent ensure => one lifecycle transition;
- foreign port => conflict and no mutation of foreign process;
- cancelled/timeout => bounded exit;
- JSON stdout contains no progress noise/secrets;
- exact exit-code/status/action mapping snapshot.

## Completion gate

```text
[ ] safe to call before every agent task
[ ] no healthy-process restart
[ ] no auth bypass
[ ] no infinite retry
[ ] deterministic JSON and exit mapping
[ ] V0.4/V0.5 lifecycle/security tests remain green
```
