# V0.6 Definition of Done and V0.7 Handoff

## V0.6 Definition of Done

Phase V0.6 is complete only when the local CLI is a reliable adapter over accepted lower-level capabilities and is safe for repeated execution-agent use.

Required command surface:

```text
c2c setup
c2c start
c2c stop
c2c status [--json]
c2c doctor [--json]
c2c ensure --json
c2c pair
c2c unpair
c2c record
c2c logs
```

`c2c session` is not part of this phase.

## Required behavior

### CLI substrate

```text
[ ] dedicated C2C.Cli executable exists
[ ] command parser/package pinned centrally
[ ] Ctrl+C/cancellation propagates
[ ] help/usage is deterministic
[ ] no generic shell/exec command exists
[ ] --json mode writes one machine object to stdout
[ ] stable schemaVersion/status/errorCode/actionRequired semantics
[ ] stable exit-code mapping has contract tests
```

### Setup/diagnostics

```text
[ ] setup delegates Workspace configuration/security rules
[ ] setup does not silently expose remote tunnel or create credentials
[ ] status is read-only
[ ] doctor is read-only and bounded
[ ] stable doctor check ids are tested
```

### Runtime

```text
[ ] start launches/reuses exactly one owned bridge runtime
[ ] stop controls only strongly-validated owned runtime
[ ] PID reuse/foreign listener tests pass
[ ] bridge remains loopback-only
[ ] tunnel sequencing/profile semantics are deterministic
[ ] runtime ownership state is atomic/restart-safe
```

### Ensure

```text
[ ] repeated/concurrent ensure converges safely
[ ] healthy runtime is not restarted
[ ] pairing requirement becomes action-required, not auth bypass
[ ] no infinite retry
```

### Auth CLI

```text
[ ] pair/unpair delegate V0.5 services
[ ] no access/refresh token is output/logged
[ ] short-lived pairing code only appears in approved explicit pair output
[ ] unpair remains workspace-bound and revoke semantics are proven
```

### Evidence/logs

```text
[ ] record reuses V0.3 recorder/idempotency/sanitizer
[ ] record returns execution id without echoing artifact body
[ ] logs reads only C2C-owned diagnostic sink
[ ] no arbitrary path option exists
[ ] log retention/read bounds and pre-persistence redaction are proven
```

### Regression

```text
[ ] Workspace suite green
[ ] Git suite green
[ ] Execution suite green
[ ] Tunnel suite green
[ ] Authorization suite green
[ ] CLI integration/security suite green
[ ] full solution builds with zero warnings/errors
```

## Handoff to V0.7

V0.7 owns C2C protocol/checkpoint/session semantics. The CLI built in V0.6 becomes the transport surface for those future services but must not predetermine their business rules.

Expected next sequence:

```text
V0.6 complete
  -> UC-C2C-01 task/session creation
  -> deterministic protocol transitions
  -> execution/evidence references
  -> checkpoint/idempotency/executor lease
  -> HANDOFF/recovery
  -> UC-CLI-06 c2c session adapter
```

## What V0.7 may add to the CLI

Only after C2C protocol contracts exist:

```text
c2c session --json
c2c session create/read/update as approved
```

The CLI command remains an adapter. Session transition validation belongs to the C2C protocol/application capability.

## Do not carry these into V0.7 as hidden debt

- unstable JSON field names;
- ambiguous process ownership;
- placeholder pairing readiness;
- unbounded logs;
- duplicate runtime state machines;
- CLI-specific copies of Workspace/Auth/Execution policy.

If any remain, V0.6 is BLOCKED rather than DONE.
