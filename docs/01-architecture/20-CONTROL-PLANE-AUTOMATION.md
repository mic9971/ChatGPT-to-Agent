# 20 — Control Plane Automation

## Purpose

C2C.NET separates three independent channels and must not mix their authentication or responsibilities:

```text
DATA PLANE
ChatGPT Web -> MCP over HTTPS/OAuth -> C2C.NET -> read-only workspace/evidence

CONTROL PLANE
Execution Agent -> browser/computer-use/manual transport -> ChatGPT Web -> C2C text protocol

LOCAL RUNTIME CONTROL
Execution Agent -> c2c CLI / loopback admin surface -> C2C.NET runtime
```

The V0.8 responsibility is **Control Plane Automation**: allowing an execution agent such as Antigravity to open or attach to the correct ChatGPT conversation, relay bounded C2C messages, receive planner responses, and recover safely without coupling `C2C.Core` to a proprietary IDE.

Detailed V0.8 implementation pack: `../04-implementation/phases/v0.8-control-plane/00-README.md`.

## Architectural invariants

- MCP remains read-only and is never used to drive the coding agent.
- Browser/UI automation is a control-plane transport, not an authorization shortcut.
- ChatGPT account authentication is user-owned. The agent may navigate to login but must pause for password, passkey, CAPTCHA, MFA/2FA or equivalent challenges.
- C2C.NET never persists ChatGPT browser cookies, passwords, passkeys or equivalent credential material.
- A planning conversation binding belongs to exactly one `workspace_id + task_id` pair.
- Browser failure never advances persisted C2C protocol state by itself.
- Every `EXECUTED` relay references already-persisted execution evidence.
- Source bodies, raw diffs and raw logs remain on the MCP data plane.
- Control transport is replaceable: Antigravity first, manual fallback always available, other agents later.
- `C2C.Core` contains no Antigravity/ChatGPT DOM/proprietary browser dependency.

## Updated component model

V0.8 may introduce a provider-neutral transport seam for coordination tests/manual or future programmatic adapters:

```csharp
public interface IControlConversationTransport
{
    Task<ControlOpenResult> OpenOrAttachAsync(
        ControlOpenRequest request,
        CancellationToken cancellationToken);

    Task<ControlSendResult> SendAsync(
        ControlConversationHandle conversation,
        string boundedMessage,
        CancellationToken cancellationToken);

    Task<ControlReceiveResult> ReceiveAsync(
        ControlConversationHandle conversation,
        ControlReceiveOptions options,
        CancellationToken cancellationToken);
}
```

The transport owns only conversation I/O. C2C state validation remains in the V0.7 protocol/session service, persistence remains local C2C app-state, and agent/browser-specific selectors remain in the integration layer.

### Important implementation correction

The Antigravity IDE path is **not** implemented as a concrete `.NET AntigravityBrowserControlDriver`.

Current Antigravity provides its own agent/browser capability. Therefore the production V0.8B path is:

```text
Antigravity Agent
  ├─ c2c CLI for runtime/session/evidence
  ├─ editor/terminal for local execution
  └─ Browser Agent for ChatGPT Web transport
```

A fake transport may be implemented for deterministic tests, and manual transport remains first-class. A future Antigravity SDK adapter may exist outside `C2C.Core` only after separate ADR/security review.

## Conversation binding

Persist only bounded non-secret transport metadata:

```text
ControlConversationBinding
- schema_version
- workspace_id
- task_id
- provider = chatgpt-web
- conversation_reference
- binding_version
- created_at
- updated_at
```

Do **not** duplicate V0.7 session state such as iteration/accepted protocol checkpoint as independently mutable transport state. V0.7 `C2CSession` remains authoritative.

The binding is not proof of authentication and must never contain browser cookies, access tokens, passwords or MFA material.

If a stored conversation cannot be reopened, use explicit `HANDOFF` rather than silently creating a new planning context and pretending it is the same conversation.

## Delivery journal

Browser delivery can be ambiguous. Persist bounded delivery metadata independently from protocol state:

```text
ControlDeliveryRecord
- delivery_id
- workspace_id
- task_id
- direction
- protocol_state
- iteration
- canonical_hash
- status
- attempt_count
- timestamps
- correlation_id
```

Recommended statuses:

```text
Prepared -> Submitting -> ObservedSent | Ambiguous | Failed
ObservedSent -> ResponseObserved -> Accepted
```

`Accepted` means the inbound C2C message passed the V0.7 parser/validator/session transition. UI submission success alone never means protocol acceptance.

## Automated control loop

```text
Execution Agent
    |
    | c2c ensure --json
    v
Runtime READY
    |
    v
Load/new V0.7 C2C session
    |
    v
Open/attach ChatGPT conversation
    |
    | INIT
    v
Planner inspects workspace through MCP
    |
    | PLAN
    v
Agent passes response through C2C session validation
    |
    v
Edit / build / test / git locally
    |
    | c2c record
    v
Persisted evidence
    |
    | EXECUTED (evidence id only)
    v
Planner reviews MCP evidence
    |
    +--> PLAN -> next iteration
    +--> DONE -> close
    +--> BLOCKED/ERROR -> stop safely
```

## Browser automation rules

Prefer semantic/accessibility-based interaction:

1. verify expected ChatGPT origin and intended conversation reference;
2. locate the composer by semantic role/label;
3. type one complete bounded message;
4. submit once;
5. wait for assistant response completion using semantic/stability evidence;
6. read only the latest response intended for the active task;
7. pass exact bounded text to the V0.7 C2C parser/validator;
8. accept only a valid `TASK_ID`, `ITERATION`, version and state transition.

Coordinate-only clicking or fixed sleeps are not stable product contracts. If browser automation becomes brittle, degrade to operator-assisted/manual control instead of weakening protocol validation.

## Authentication boundary

Remote MCP OAuth and browser account login are separate:

```text
ChatGPT connector -> OAuth/PKCE -> C2C.NET MCP
Execution agent -> browser session -> ChatGPT Web
Execution agent -> local authorization -> C2C.NET admin/CLI
```

The execution agent does not receive ChatGPT's MCP refresh token merely because it controls the browser, and C2C.NET does not receive the user's ChatGPT password/browser cookie.

## Idempotency and duplicate prevention

- Persist the hash of each prepared/observed control delivery.
- Retrying the same outbound message is allowed only when the content hash/delivery identity matches and absence has been established when the prior send was ambiguous.
- A different message for the same `(task_id, iteration, state)` is a protocol conflict.
- After browser timeout/cancellation following submit, inspect the conversation before resending.
- Never increment iteration merely because browser transport retried.

## Failure and recovery

| Failure | Required behavior |
|---|---|
| ChatGPT logged out | `CTRL_USER_AUTH_REQUIRED`; pause for user action |
| CAPTCHA / MFA / passkey | `CTRL_USER_AUTH_REQUIRED`; no bypass attempt |
| Conversation missing | explicit HANDOFF flow |
| Composer unavailable | bounded semantic retry, then manual fallback |
| Send timeout/unknown after submit | mark ambiguous; inspect before resend |
| Partial response | wait for completion or timeout; do not parse as PLAN |
| Duplicate planner response | same hash is idempotent |
| Conflicting response | `C2C_PROTOCOL_CONFLICT` |
| Browser crash | keep prior protocol checkpoint; reconcile delivery journal |
| Wrong task/iteration | reject response |
| Automated transport unavailable | manual fallback using same C2C validation |

## Testing strategy

V0.8 requires deterministic fake/manual transport tests plus a real/operator-assisted Antigravity smoke. CI must not depend on live ChatGPT credentials.

Required scenarios:

- open existing conversation;
- INIT -> PLAN;
- EXECUTED -> DONE;
- two-iteration PLAN loop;
- crash after send but before local acknowledgement;
- duplicate response;
- wrong task id / stale iteration;
- login required;
- HANDOFF after missing conversation;
- manual fallback;
- ambiguous send reconciliation;
- no source/diff/log/browser credential leakage.

## Implementation boundary

Implementation order remains:

```text
secure workspace/data plane
-> execution evidence
-> tunnel/auth
-> CLI
-> C2C protocol/checkpoint
-> V0.8A control-plane coordination substrate
-> V0.8B Antigravity integration workflow
-> packaging
```

Do not move agent/browser automation into `C2C.Host` or `C2C.Core` merely to make the E2E demo easier.
