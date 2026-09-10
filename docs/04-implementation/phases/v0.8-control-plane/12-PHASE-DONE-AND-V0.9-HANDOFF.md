# V0.8 Definition of Done and V0.9 Handoff

## V0.8A Done

Control Plane Automation Core is complete only when:

```text
[ ] UC-CTRL-01..08 implemented against actual V0.7 contracts
[ ] conversation binding is workspace/task isolated
[ ] transport metadata persistence is atomic and restart-safe
[ ] delivery journal handles ambiguous send without blind resend
[ ] inbound planner text always passes V0.7 parser/validator
[ ] duplicate identical delivery is idempotent
[ ] conflicting duplicate delivery fails closed
[ ] browser/transport failure cannot advance protocol state
[ ] fake/manual transport proves one-iteration DONE
[ ] fake/manual transport proves two-iteration REPLAN -> DONE
[ ] HANDOFF replacement conversation path is deterministic
[ ] manual fallback uses same protocol/session logic
[ ] no proprietary provider type exists in C2C.Core
[ ] full regression suite green
```

## V0.8B Done

Antigravity integration is complete only when:

```text
[ ] UC-AGT-01 integration pack install/exposure path is usable
[ ] UC-AGT-02 Antigravity can start a C2C task
[ ] UC-AGT-03 accepted PLAN is executed locally by Antigravity
[ ] UC-AGT-04 evidence/EXECUTED/resume/HANDOFF flow is usable
[ ] Browser Agent opens/attaches correct ChatGPT conversation
[ ] user auth challenges pause safely
[ ] EXECUTED contains evidence reference only
[ ] ChatGPT independently reads evidence via MCP
[ ] at least one real/operator-assisted browser smoke reaches terminal state
[ ] at least one resume or HANDOFF recovery smoke passes
[ ] C2C.NET persists no ChatGPT browser credentials
```

## Security gate

The following are release-blocking:

- source/diff/raw-log body appears in control delivery;
- access/refresh/pairing/browser secret appears in binding/journal/CLI output;
- wrong workspace/task conversation can be reused;
- stale iteration planner response accepted;
- send timeout causes blind duplicate submission;
- browser crash re-executes a PLAN automatically;
- MCP write/exec capability introduced;
- manual fallback bypasses C2C validation;
- Antigravity/ChatGPT DOM implementation leaks into Core.

## Operational gate

Before handoff, document known UI fragility separately from protocol correctness.

A browser UI change may make automation `DEGRADED` without invalidating C2C protocol/data-plane correctness. Manual fallback must remain available.

## V0.9 handoff

Only after V0.8 gates pass, begin packaging/distribution work:

```text
V0.9
  -> UC-PKG-01 package/install distribution
  -> UC-PKG-02 state migration only when a real migration is required
```

V0.9 should package the proven runtime and integration assets; it must not redesign protocol/security boundaries.

Expected artifacts entering V0.9:

```text
C2C.Host
C2C.Cli
Core/Infrastructure runtime
read-only MCP + OAuth/tunnel
C2C protocol/session/checkpoint
Control Plane coordination substrate
Antigravity integration pack
operator setup docs
security/regression tests
```

## Do not auto-advance

When V0.8 implementation reports DONE, stop and request review. Do not add installer/updater/autostart/global-agent configuration until V0.9 is explicitly approved.
