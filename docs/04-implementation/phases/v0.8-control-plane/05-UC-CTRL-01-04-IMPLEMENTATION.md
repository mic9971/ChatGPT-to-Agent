# V0.8A — UC-CTRL-01..04 Implementation Plan

## Scope

This slice implements the first half of Control Plane Automation:

- `UC-CTRL-01` Start Control Session
- `UC-CTRL-02` Open/Attach Planner Conversation
- `UC-CTRL-03` Send C2C Message
- `UC-CTRL-04` Receive/Validate Planner Response

The implementation must remain transport-neutral and must reuse V0.7 session/protocol services.

## UC-CTRL-01 — Start Control Session

### Required behavior

1. Verify local runtime readiness through the existing readiness service/CLI semantics.
2. Load the V0.7 task session.
3. Verify workspace/task identity.
4. Acquire/reacquire the task executor lease according to V0.7 rules.
5. Load/create bounded conversation binding metadata.
6. Return a control-session snapshot with deterministic next action.

### Must not

- create a browser session;
- authenticate the user;
- change C2C iteration;
- invent a new protocol checkpoint;
- copy auth tokens into control state.

### Tests

- new control session;
- repeated start is idempotent;
- runtime not ready;
- task not found;
- workspace mismatch;
- lease conflict;
- corrupted binding;
- credential canary absent from persisted state.

## UC-CTRL-02 — Open/Attach Conversation

### Responsibility split

C2C.NET owns only the validated binding request/result. The external transport/agent owns actual browser navigation.

```text
Coordinator
  -> returns existing conversation reference or NEW_CONVERSATION_REQUIRED

External transport
  -> opens/attaches browser conversation
  -> returns non-secret conversation reference

Coordinator
  -> validates and persists binding
```

### Rules

- reuse only a reference already bound to the same workspace/task;
- if stored conversation is unavailable, do not silently replace it;
- if login/MFA/CAPTCHA/passkey is required, return `CTRL_USER_AUTH_REQUIRED`;
- browser credential state is never persisted by C2C.NET;
- reference must be validated as non-secret metadata before save.

### Tests

- attach existing binding;
- bind first conversation;
- cross-task reference reuse denied;
- missing old conversation requires handoff;
- auth-required state;
- unsafe reference rejected.

## UC-CTRL-03 — Send C2C Message

### Required flow

```text
V0.7 session determines valid outbound state
 -> V0.7 renderer produces bounded text
 -> compute canonical hash
 -> persist delivery Prepared
 -> external transport submits exact text
 -> record observed/ambiguous result
```

A transport send success does not itself advance the C2C state machine.

### Retry rule

```text
if send result == ambiguous
    do not resend
    require conversation inspection/reconciliation
```

Only the same delivery id/hash may be retried after absence is established.

### Tests

- INIT send prepared;
- EXECUTED send prepared only with valid evidence;
- duplicate same hash;
- conflicting duplicate;
- hard message size limit;
- timeout after possible submit;
- no source/diff/log canary in rendered/delivery state.

## UC-CTRL-04 — Receive and Validate Planner Response

### Required flow

```text
external transport reads completed assistant text
 -> enforce transport input cap
 -> pass exact text to V0.7 parser
 -> validate protocol version/task/iteration/state
 -> duplicate/conflict check
 -> call V0.7 session transition
 -> mark inbound delivery Accepted
```

The transport does not parse PLAN semantics itself.

### Completion boundary

For automated browser transports, response completion must be established by the integration layer using semantic UI state or bounded stability checks. Fixed sleep alone is not sufficient.

### Tests

- PLAN accepted;
- DONE accepted only if V0.7 evidence gate passes;
- BLOCKED/ERROR accepted when valid;
- wrong task id;
- stale iteration;
- unsupported protocol version;
- duplicate identical response;
- conflicting duplicate;
- partial/non-C2C assistant response;
- browser crash before acceptance leaves checkpoint unchanged.

## Suggested code placement

After inspecting the actual V0.7 implementation, prefer:

```text
src/C2C.Core/ControlPlane/
  Coordination/
  Conversation/
  Delivery/

src/C2C.Infrastructure/ControlPlane/
  Persistence/

src/C2C.Cli/ControlPlane/       # only for missing manual/binding adapters

tests/C2C.Core.Tests/ControlPlane/
tests/C2C.IntegrationTests/ControlPlane/
```

Do not create global `Drivers/`, `Managers/`, `Helpers/` or `Models/` dumping folders.

## Acceptance gate for this slice

```text
[ ] task/workspace binding proven
[ ] runtime readiness reused, not duplicated
[ ] conversation reference persists no secrets
[ ] outbound delivery journal is crash-safe
[ ] ambiguous send never auto-resends
[ ] inbound text always passes V0.7 parser/validator
[ ] browser failure does not advance session
[ ] fake/manual tests green
```

Do not begin `UC-CTRL-05..08` until this slice is reviewed.
