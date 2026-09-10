# V0.6 Baseline and Entry Gate

## Repository baseline

V0.6 is planned against the repository after V0.4 Tunnel completion. The current runtime already has:

- secure Workspace services and four workspace MCP tools;
- Git Evidence and `git_status` / `git_diff`;
- Execution Evidence and `execution_summary` / `test_status` / `execution_output`;
- Tunnel capability including `ITunnelService`, `IRuntimeEnsurer`, `IBridgeRuntime`, owned-process validation and Cloudflare Quick Tunnel provider;
- a loopback-only `C2C.Host` MCP endpoint;
- no dedicated `C2C.Cli` executable yet.

The current V0.4 bridge runtime is intentionally in-host: `LoopbackBridgeRuntime.EnsureStartedAsync` assumes the configured local endpoint is active. V0.6 therefore owns the missing **external local lifecycle adapter** required by `c2c start` / `c2c stop`; do not incorrectly treat the current in-host probe as a complete background process supervisor.

## Hard dependency gate

Runtime implementation of the full V0.6 phase starts only when V0.5 is complete.

Required before V0.6 implementation:

```text
[ ] V0.5 auth substrate decision accepted
[ ] pairing create/consume proven
[ ] PKCE/issuer/resource/workspace/client validation proven
[ ] all 9 MCP tools protected by exact scopes
[ ] refresh rotation/replay behavior proven if refresh tokens are enabled
[ ] revoke/unpair behavior proven
[ ] no auth secrets leak in logs/output
[ ] full build/test green
```

Documentation may be merged earlier.

## CLI-specific baseline verification

Before any edits, the implementation agent must inspect actual `main` and report:

- solution/project list;
- current `C2C.Host` startup composition;
- Workspace configuration/store contracts;
- Tunnel/Runtime contracts and process ownership model;
- Authorization service contracts produced by V0.5;
- Execution recorder/query contracts;
- current logging sinks;
- package/version baseline;
- full build/test result.

Do not trust this document when code has advanced. Actual source wins unless it violates an approved BR/ADR.

## Important design gaps V0.6 must close

### 1. Dedicated CLI executable

Create `src/C2C.Cli` as an adapter executable. It may reference Core and Infrastructure/composition as needed, but it must not become a policy owner.

### 2. Bridge process lifetime

`c2c start` must launch a background/owned `C2C.Host` runtime that survives the CLI command. `c2c stop` must stop only that owned instance. PID alone is insufficient because of PID reuse; ownership validation must use additional identity such as start time/process fingerprint/runtime instance id.

### 3. Stable machine contract

Every agent-relevant command supports deterministic `--json`, stable status values, schema version and documented exit codes.

### 4. Local diagnostics persistence

`c2c logs` requires a bounded C2C-owned diagnostic source. If the repository still has no file/structured diagnostic sink at implementation time, add a minimal local diagnostics capability; never let `logs` accept an arbitrary filesystem path.

### 5. Real pairing probe

After V0.5, `IRuntimeEnsurer` must use an authorization-aware pairing/readiness probe rather than a placeholder/default that always assumes readiness or need-pairing incorrectly.

## Stop conditions

Stop and request design/ADR review if implementation requires any of the following:

- exposing a public/wildcard bridge bind;
- killing processes by name or PID without ownership validation;
- storing raw auth tokens in CLI config/output;
- adding shell execution for convenience;
- reading arbitrary files through `c2c logs`;
- implementing C2C session/checkpoint early;
- duplicating Workspace/Auth/Tunnel/Execution business rules in command handlers;
- changing an existing public JSON/MCP contract unrelated to the CLI;
- adding a database solely for CLI state.
