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

## Architectural invariants

- MCP remains read-only and is never used to drive the coding agent.
- Browser/UI automation is a control-plane transport, not an authorization shortcut.
- ChatGPT account authentication is user-owned. The agent may navigate to login but must pause for password, passkey, CAPTCHA, MFA/2FA or equivalent challenges.
- C2C.NET never persists ChatGPT browser cookies, passwords, passkeys or equivalent credential material.
- A planning conversation binding belongs to exactly one `workspace_id + task_id` pair.
- Browser failure never advances the persisted C2C state by itself.
- Every `EXECUTED` relay references already-persisted execution evidence.
- Source bodies, raw diffs and raw logs remain on the MCP data plane.
- Control transport is replaceable: Antigravity first, manual fallback always available, Codex/other drivers later.

## Component model

V0.8 may introduce a transport abstraction outside the domain/security core:

```csharp
public interface IControlPlaneDriver
{
    Task<ControlConversation> OpenOrAttachAsync(
        ControlConversationRequest request,
        CancellationToken cancellationToken);

    Task SendAsync(
        ControlConversation conversation,
        C2CEnvelope envelope,
        CancellationToken cancellationToken);

    Task<C2CEnvelope> WaitForResponseAsync(
        ControlConversation conversation,
        ControlWaitOptions options,
        CancellationToken cancellationToken);
}
```

`IControlPlaneDriver` is transport only. C2C state validation remains in the protocol service, session/checkpoint persistence remains in C2C.NET, and browser-specific selectors/recovery remain inside the agent integration layer.

Initial transports:

```text
IControlPlaneDriver
├── AntigravityBrowserControlDriver
└── ManualControlDriver

Future:
└── CodexComputerUseControlDriver
```

Do not create this interface before V0.8 merely because this design exists. Earlier phases follow `BR-COM-012` and avoid speculative runtime abstractions.

## Conversation binding

Persist only bounded non-secret metadata:

```text
ControlConversationBinding
- schema_version
- task_id
- workspace_id
- provider = chatgpt-web
- conversation_reference
- last_accepted_state
- iteration
- last_sent_message_hash
- last_received_message_hash
- updated_at
```

The binding is not proof of authentication and must never contain browser cookies, access tokens, passwords or MFA material.

If a stored conversation cannot be reopened, use explicit `HANDOFF` rather than silently creating a new planning context and pretending it is the same conversation.

## Automated control loop

```text
Execution Agent
    |
    | c2c ensure --json
    v
Runtime READY
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
Agent validates PLAN
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

1. locate the ChatGPT composer by semantic role/label;
2. verify expected host and conversation identity;
3. type one complete bounded message;
4. submit once;
5. wait for assistant response completion;
6. parse only the latest response intended for the active task;
7. validate `TASK_ID`, `ITERATION`, protocol version and allowed state before accepting it.

Coordinate-only clicking is not a stable contract. If the available browser automation is too brittle, degrade to operator-assisted/manual control instead of weakening protocol validation.

## Authentication boundary

Remote MCP OAuth and browser account login are separate:

```text
ChatGPT connector -> OAuth/PKCE -> C2C.NET MCP
Execution agent -> browser session -> ChatGPT Web
Execution agent -> local authorization -> C2C.NET admin/CLI
```

The execution agent does not receive the MCP refresh token and C2C.NET does not receive the user's ChatGPT password.

## Idempotency and duplicate prevention

- Persist the hash of the last accepted outbound/inbound C2C envelope.
- Retrying the same outbound message is allowed only when the persisted transition is still pending and the content hash matches.
- A different message for the same `(task_id, iteration, state)` is a protocol conflict.
- After a browser timeout, inspect the conversation before resending.
- Never increment iteration merely because the browser driver retried.

## Failure and recovery

| Failure | Required behavior |
|---|---|
| ChatGPT logged out | `CTRL_USER_AUTH_REQUIRED`; pause for user action |
| CAPTCHA / MFA / passkey | `CTRL_USER_AUTH_REQUIRED`; no bypass attempt |
| Conversation missing | explicit HANDOFF flow |
| Composer unavailable | bounded retry, then manual fallback |
| Send timeout | inspect conversation before deciding to resend |
| Duplicate planner response | same hash is idempotent |
| Conflicting response | `C2C_PROTOCOL_CONFLICT` |
| Browser crash | keep prior checkpoint; do not advance task |
| Wrong task/iteration | reject response |

## Testing strategy

V0.8 requires deterministic fake-driver tests plus a real/manual Antigravity smoke. CI must not depend on live ChatGPT credentials.

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
- manual fallback.

## Implementation boundary

This document is design-only until V0.8. Current Workspace/MCP implementation must not be refactored to anticipate this module. Implementation order remains:

```text
secure workspace/data plane
-> execution evidence
-> tunnel/auth
-> C2C protocol/checkpoint
-> control-plane automation
-> Antigravity E2E
```
