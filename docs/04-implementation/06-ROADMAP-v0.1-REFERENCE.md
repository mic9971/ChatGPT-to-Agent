# 14 — Roadmap

## V0.1 — Repository foundation
- .NET 10 solution, global.json, Central Package Management.
- coding/analyzer/test baseline.
- Core/Host/Infrastructure/CLI minimal projects.
- no remote auth/tunnel yet.

## V0.2 — Secure local MCP slice
- loopback Host;
- MCP 2026-07-28 via official C# SDK;
- `workspace_info`, `read_file`;
- canonical path resolver + sensitive deny + tests.

**Gate:** MCP client reads safe file and fails all escape tests.

## V0.3 — Workspace evidence
- list/search;
- Git status/diff;
- bounded pagination/cursors;
- `.c2cignore`;
- expanded adversarial tests.

## V0.4 — Execution evidence
- session/record CLI;
- execution summary/test status/output;
- sanitizer;
- idempotency and executor lease.

## V0.5 — Tunnel/lifecycle
- `ITunnelProvider`;
- Cloudflare Quick Tunnel;
- start/stop/status/doctor/ensure;
- child-process ownership/recovery.

## V0.6 — Remote authorization
- Protected Resource Metadata;
- OAuth auth-code + PKCE;
- issuer validation;
- CIMD-first client registration strategy;
- revocation/refresh rotation;
- pairing bootstrap;
- optional DCR compatibility profile only if needed by target client.

## V0.7 — C2C protocol integration
- parser/validator;
- checkpoint state;
- HANDOFF;
- machine-readable session contract.

## V0.8 — Antigravity integration
- instruction pack;
- full INIT/PLAN/EXECUTED/REVIEW loop;
- end-to-end task on sample .NET repo.

## V0.9 — Packaging
- single-file/self-contained distributions;
- macOS arm64 first, then win-x64/linux-x64/linux-arm64/osx-x64;
- install/update/uninstall scripts with integrity checks.

## V1.0 acceptance
- remote planner can securely inspect local repo;
- execution agent can implement/test and record evidence;
- planner independently reviews diff/test evidence;
- resume/handoff works;
- no MCP write/shell capability;
- security suite passes;
- primary platforms publish successfully.

## Post-V1 candidates
- stable named tunnel wizard;
- OS keychain integration hardening;
- additional agent packs;
- enterprise-managed auth profile;
- richer diagnostics/telemetry opt-in;
- signed release artifacts/auto-update;
- do not add web dashboard or write/exec MCP without a new threat model/ADR.
