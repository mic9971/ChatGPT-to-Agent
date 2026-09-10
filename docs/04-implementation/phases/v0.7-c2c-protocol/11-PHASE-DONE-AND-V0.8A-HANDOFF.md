# V0.7 Definition of Done and V0.8A Handoff

## Phase V0.7 is DONE only when

```text
[ ] UC-C2C-01 implemented and tested
[ ] UC-C2C-02 implemented and tested
[ ] UC-C2C-03 implemented and tested
[ ] UC-C2C-04 implemented and tested
[ ] UC-C2C-05 transition behavior implemented and tested
[ ] UC-C2C-06 implemented and tested
[ ] UC-C2C-07 implemented and tested
[ ] UC-C2C-08 implemented and tested
[ ] UC-CLI-06 implemented on the V0.6 CLI contract
[ ] parser/renderer deterministic
[ ] 4 KB hard input cap enforced
[ ] bounded PLAN/HANDOFF sections enforced
[ ] task/workspace/iteration binding proven
[ ] duplicate identical transition idempotent
[ ] conflicting duplicate rejected
[ ] atomic session/checkpoint persistence proven
[ ] stale writer cannot overwrite new checkpoint
[ ] one active executor lease proven under race
[ ] crash-in-EXECUTING never auto-reruns work
[ ] execution evidence binding proven
[ ] DONE without current evidence rejected
[ ] terminal DONE cannot reopen
[ ] no RESUME protocol state
[ ] control messages contain no source/diff/raw-log/token bodies
[ ] full build/test suite green
```

## V0.7 output contract for V0.8A

V0.8A Control Plane Automation may rely on these stable capabilities:

```text
Create task
 -> obtain INIT text

Submit planner text
 -> parse/validate
 -> session advances or returns stable error

Query session
 -> task id
 -> iteration
 -> checkpoint
 -> next expected action

Mark execution started
 -> local checkpoint only

Attach execution evidence
 -> obtain EXECUTED text

Resume
 -> deterministic next action

Handoff
 -> bounded HANDOFF text
```

This allows a future transport to be extremely thin:

```text
open/attach conversation
send already-rendered C2C message
receive text
submit text to V0.7 protocol service
```

## Boundary to preserve

V0.8A must not move provider/browser logic backward into C2C.Core.

```text
C2C.Core
  knows protocol/session
  does NOT know ChatGPT browser

Control Plane Integration
  knows conversation transport
  calls Core protocol/session contracts
```

## Suggested V0.8A starting sequence

After V0.7 is accepted:

```text
1. Fake/manual control-plane driver contract
2. Open/attach conversation abstraction
3. Send bounded rendered message
4. Receive bounded planner response
5. Feed response into V0.7 parser/session service
6. Prove INIT -> PLAN -> EXECUTED -> DONE using fake driver
7. Add resume/handoff/recovery
8. Only then add Antigravity/browser integration in V0.8B
```

## No live ChatGPT dependency at V0.7 gate

V0.7 must be fully testable without:

- ChatGPT account;
- browser login;
- CAPTCHA/MFA;
- Cloud browser automation;
- Antigravity IDE.

This separation is intentional: protocol/session correctness must be deterministic before UI automation is allowed to depend on it.
