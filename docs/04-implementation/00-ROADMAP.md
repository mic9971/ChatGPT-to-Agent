# Detailed Implementation Roadmap

## Phase V0.1 — Foundation / secure local read

1. `UC-WS-01` Configure and Bind Workspace
2. `UC-WS-04` Read File (Core service + adversarial path tests)
3. `UC-MCP-01` Stateless MCP endpoint
4. Expose `workspace_info` (`UC-WS-02`) and `read_file`

**Gate:** local MCP read works; traversal/absolute/symlink/sensitive cases fail closed; no write/exec tool exists.

## Phase V0.2 — Workspace discovery + Git evidence

5. `UC-WS-03` List Directory
6. `UC-WS-05` Search Workspace
7. `UC-GIT-01` Git Status
8. `UC-GIT-02` Git Diff

Detailed implementation pack: `phases/v0.2-git-evidence/00-README.md`.

**Gate:** denied names/content never leak through list/search/Git.

## Phase V0.3 — Execution evidence

9. `UC-EXE-01` Record Execution
10. `UC-EXE-02` Summary
11. `UC-EXE-03` Test Status
12. `UC-EXE-04` Sanitized Output

**Gate:** immutable evidence records, idempotency, sanitizer/restricted artifacts proven.

## Phase V0.4 — Runtime/tunnel

13. `UC-TUN-01..03`

**Gate:** bridge stays loopback-only, owned tunnel lifecycle is restart-safe, concurrent ensure converges, foreign processes are untouched, and tunnel/session output leaks no secrets.

## Phase V0.5 — Authorization/pairing

14. `UC-AUTH-01..05`
15. Finish protected-call path in `UC-MCP-02`

Detailed implementation pack: `phases/v0.5-auth-pairing/00-README.md`.

Implementation order inside the phase is deliberately gated:

```text
V0.5A auth substrate/interoperability spike
  -> pairing
  -> authorize + PKCE
  -> token + protected MCP
  -> refresh rotation
  -> revoke/unpair capability
```

**Gate:** current MCP auth profile is evidenced, Protected Resource Metadata/discovery is accurate, PKCE/issuer/resource/workspace/client binding is enforced, all nine tools have exact scopes, refresh rotation/replay and revoke semantics are proven, and no auth secret leaks. DCR remains compatibility-only.

## Phase V0.6 — CLI & Local Lifecycle

16. CLI substrate + stable machine contract
17. `UC-CLI-01` Setup CLI
18. `UC-CLI-03` Status and Doctor
19. `UC-CLI-02` Start and Stop Runtime
20. `UC-CLI-04` Ensure Runtime Ready
21. `UC-CLI-05` Pair and Unpair CLI
22. `UC-CLI-07` Record Evidence and Read Local Logs

Detailed implementation pack: `phases/v0.6-cli/00-README.md`.

`UC-CLI-06 Manage C2C Session` is intentionally deferred to V0.7 because its declared dependencies are C2C protocol/checkpoint use cases.

Implementation order:

```text
V0.6A CLI substrate + setup + read-only diagnostics
  -> V0.6B owned bridge lifecycle + ensure
  -> V0.6C pair/unpair adapters
  -> V0.6D record/logs adapters
```

**Gate:** the dedicated CLI exposes stable `--json`/exit-code contracts, setup does not silently create remote exposure, status/doctor are read-only, start/stop prove process ownership beyond PID, repeated/concurrent ensure converges without auth bypass, pair/unpair expose no long-lived credential, record reuses V0.3 evidence semantics, logs are bounded C2C-owned diagnostics only, and all prior security suites remain green.

## Phase V0.7 — C2C protocol/checkpoint

23. `UC-C2C-01..08`
24. `UC-CLI-06` Manage C2C Session, only after the protocol/session contracts exist

Detailed implementation pack: `phases/v0.7-c2c-protocol/00-README.md`.

**Gate:** protocol/session transitions, evidence references, idempotency, executor lease and HANDOFF are deterministic without browser automation.

## Phase V0.8A — Control Plane Coordination Substrate

25. `UC-CTRL-01..02` start control session + task-bound conversation binding
26. `UC-CTRL-03..04` bounded send + receive/validate
27. `UC-CTRL-05..08` iteration loop + resume + HANDOFF + recovery
28. conversation delivery journal + ambiguous-send reconciliation
29. deterministic fake/manual transport proof

Detailed implementation pack for both V0.8A and V0.8B: `phases/v0.8-control-plane/00-README.md`.

Implementation order:

```text
V0.8A-1 conversation binding + UC-CTRL-01..02
  -> V0.8A-2 send/receive + fake/manual transport
  -> V0.8A-3 iteration/resume/handoff/recovery
```

**Gate:** INIT -> PLAN -> EXECUTED -> DONE and two-iteration REPLAN are deterministic under fake/manual transport; browser/transport failure cannot advance C2C protocol state; ambiguous sends are inspected before resend; conversation binding is workspace/task isolated; no live ChatGPT credential is required in CI.

## Phase V0.8B — Execution Agent Integration

30. `UC-AGT-01..04` (Antigravity first)
31. install/expose the Antigravity integration pack
32. use Antigravity's own Browser Agent for ChatGPT Web transport
33. run real/operator-assisted Antigravity smoke for INIT -> PLAN -> local execution -> evidence -> EXECUTED -> review -> DONE/REPLAN
34. run one restart/resume or HANDOFF recovery smoke

The V0.8 IDE path deliberately does **not** require a concrete `.NET AntigravityBrowserControlDriver`. C2C.NET exposes provider-neutral C2C/control contracts; the Antigravity agent owns editor/terminal/browser execution. A future programmatic SDK adapter may be added only behind a separate ADR and outside `C2C.Core`.

**Gate:** Antigravity can drive the full control loop while ChatGPT independently reads source/diff/test evidence through MCP; user-owned password/passkey/CAPTCHA/MFA actions pause safely; manual fallback remains usable; C2C.NET stores no ChatGPT browser credentials.

## Phase V0.9 — Packaging

35. `UC-PKG-01`
36. `UC-PKG-02` only when the first real state migration is needed.

Do not implement a later phase merely because an agent has spare context. Each gate must be reviewed before the next security boundary is exposed. Provider UI automation may change over time; browser/UI churn must not redefine C2C protocol or security semantics.
