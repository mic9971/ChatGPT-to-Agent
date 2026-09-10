# V0.6 Test and Security Gate

## Test strategy

V0.6 adds a local executable boundary. Tests must verify command parsing, machine contracts, process ownership and integration with existing capabilities. Do not rely only on command-handler unit tests.

## Required layers

### Command/contract tests

Verify:

- known commands/options parse correctly;
- unknown command/option returns usage error;
- mutually-exclusive/invalid values fail before service invocation;
- `--json` emits exactly one valid JSON object on stdout;
- exit code mapping is stable;
- Ctrl+C/cancellation propagates;
- human formatting changes do not alter JSON fields.

### Integration tests

Run the CLI entry point/process against isolated temp app-state and fake/controlled dependencies where possible.

Cover:

```text
setup
status
doctor
start
stop
ensure
pair
unpair
record
logs
```

Live Cloudflare/ChatGPT is not required in CI; fake provider/auth implementations must prove orchestration deterministically.

## Runtime ownership adversarial matrix

```text
1. valid owned PID + matching start time + matching executable -> may control
2. same PID + different start time -> deny/foreign
3. same PID + different executable identity -> deny/foreign
4. stale ownership file + no process -> recover metadata safely
5. foreign listener on bridge port -> conflict; do not kill
6. concurrent start xN -> one owned bridge
7. concurrent stop xN -> idempotent/no foreign kill
8. startup timeout -> only newly-created owned process cleaned
9. tunnel failure after bridge ready -> ownership record remains coherent
10. corrupt runtime state -> fail closed/doctor diagnostic, no blind process kill
```

## Secret-canary matrix

Inject unique canaries representing:

```text
Bearer/access token
refresh token
authorization code
PKCE verifier
pairing code
GitHub/OpenAI-like token
private key block
home-directory path
provider credential
```

Capture:

- stdout;
- stderr;
- persisted diagnostic logs;
- exception/error objects;
- JSON output;
- runtime state files.

Long-lived auth/token/private-key canaries must not appear. The short-lived pairing code may appear only in the explicit `pair` result according to its documented contract and must not appear in logs/errors/other commands.

## Command-specific security assertions

### setup

- does not silently start tunnel/pairing;
- repo path/metacharacters never become shell syntax;
- no secret environment value printed.

### status/doctor

- strictly non-mutating;
- no token/code/config-secret dump;
- optional dependency missing does not crash;
- malformed app-state returns safe diagnostic.

### start/stop

- loopback-only bridge invariant remains green;
- no kill-by-name;
- PID reuse protected;
- only verified owned runtime can be terminated.

### ensure

- repeated/concurrent calls converge;
- pairing requirement never bypassed;
- no infinite retry;
- action-required status deterministic.

### pair/unpair

- no long-lived token output;
- pairing code not logged;
- cross-workspace client operation denied;
- revoke integration proves future access/refresh denied according to V0.5 policy.

### record/logs

- execution sanitizer preserved;
- arbitrary file log read impossible;
- output/time/line/byte limits enforced.

## Regression gate

All existing suites from Workspace, Git, Execution, Tunnel and Authorization must remain green.

Minimum final verification:

```text
dotnet build C2CNet.sln
dotnet test C2CNet.sln
```

Warnings are errors under the repository baseline.

## Phase PASS criteria

V0.6 is PASS only when:

```text
[ ] all V0.6 commands have deterministic help/usage
[ ] all agent-relevant commands have stable --json schema tests
[ ] exit-code mapping has contract tests
[ ] setup/status/doctor security tests pass
[ ] runtime ownership adversarial matrix passes
[ ] ensure concurrency/idempotency passes
[ ] auth CLI canary tests pass
[ ] record/logs bounds and redaction pass
[ ] no prior security regression
[ ] full build/test green
```

Any observed foreign-process kill, auth-secret leak, arbitrary-file log read, wildcard bridge bind or auth bypass is an immediate BLOCKED result.
