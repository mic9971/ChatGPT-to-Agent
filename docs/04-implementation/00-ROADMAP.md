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

**Gate:** protocol/session transitions, evidence references, idempotency, executor lease and HANDOFF are deterministic without browser automation.

## Phase V0.8A — Control Plane Automation Core

25. `UC-CTRL-01..04` session/conversation/send/receive
26. `UC-CTRL-05..08` loop/resume/handoff/recovery
27. Implement the transport abstraction only now, after V0.7 contracts are accepted.

**Gate:** deterministic fake driver proves INIT -> PLAN -> EXECUTED -> DONE, duplicate safety, auth-required behavior and manual fallback. No live ChatGPT credential is required in CI.

## Phase V0.8B — Execution Agent Integration

28. `UC-AGT-01..04` (Antigravity first)
29. Antigravity real browser/computer-use smoke: open/attach conversation, bounded C2C relay, local code execution, evidence record, review loop.

**Gate:** Antigravity can drive the full control loop while ChatGPT independently reads source/diff/test evidence through MCP. Password/passkey/CAPTCHA/MFA remain user-owned actions.

## Phase V0.9 — Packaging

30. `UC-PKG-01`
31. `UC-PKG-02` only when the first real state migration is needed.

Do not implement a later phase merely because an agent has spare context. Each gate must be reviewed before the next security boundary is exposed. The Control Plane Automation design may be documented early, but runtime code must not be introduced before its V0.8 slice.
