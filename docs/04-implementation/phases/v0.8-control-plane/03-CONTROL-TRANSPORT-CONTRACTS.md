# V0.8 Control Transport Contracts

## Goal

Define a provider-neutral seam around conversation transport without allowing transport code to own C2C protocol state.

## Contract principle

The transport may:

- open or attach a conversation;
- submit already-rendered bounded text;
- wait for/read completed planner text;
- report transport/auth/navigation failures.

The transport may **not**:

- decide whether a PLAN/DONE transition is valid;
- increment C2C iteration;
- generate execution evidence;
- mutate workspace state;
- bypass browser authentication;
- rewrite planner text before validation.

## Suggested contracts

Exact names must match the implemented V0.7 style, but responsibilities should remain narrow.

```csharp
public interface IControlPlaneCoordinator
{
    Task<OperationResult<ControlSessionSnapshot>> StartAsync(
        string taskId,
        CancellationToken cancellationToken);

    Task<OperationResult<ControlOutboundMessage>> PrepareOutboundAsync(
        string taskId,
        CancellationToken cancellationToken);

    Task<OperationResult<ControlAcceptResult>> AcceptInboundAsync(
        string taskId,
        string boundedPlannerText,
        CancellationToken cancellationToken);
}
```

`PrepareOutboundAsync` must obtain/render the currently valid message from the V0.7 C2C session service. It must not invent protocol text independently.

Optional transport seam for deterministic tests/future adapters:

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

## Transport result model

Do not throw transport-specific exceptions through Core as the normal control flow. Normalize into stable categories:

```text
Ready
UserAuthRequired
ConversationUnavailable
ComposerUnavailable
SendAmbiguous
ResponseTimeout
NavigationFailed
DriverUnavailable
Cancelled
Failed
```

Each result may include:

```text
safeMessage
retryable
correlationId
conversationReference?   # non-secret only
```

It must never include:

```text
cookie
browser storage token
password
passkey
OAuth access/refresh token
raw page dump
unbounded HTML
```

## Open/attach semantics

`OpenOrAttachAsync` receives only a validated conversation request:

```text
workspaceId
taskId
provider
existingConversationReference?
```

If a stored reference exists and cannot be reopened, return `ConversationUnavailable`; do not silently create a replacement. The coordinator must route that condition through `UC-CTRL-07` HANDOFF.

## Send semantics

Before transport send:

1. V0.7 message is rendered and validated.
2. Size is bounded.
3. Canonical message digest is computed.
4. Delivery attempt is persisted as `Prepared`.
5. Transport submits exactly that text.

After send, a successful UI action is not equivalent to an accepted C2C transition. Store transport delivery state separately from protocol checkpoint state.

Recommended delivery statuses:

```text
Prepared
Submitting
ObservedSent
Ambiguous
ResponseObserved
Accepted
Failed
```

Do not use `Accepted` until the inbound C2C response passes V0.7 parser/validator/session logic.

## Receive semantics

A transport may return text only when it believes the assistant response is complete, but C2C.NET still treats the returned text as untrusted input.

Required acceptance order:

```text
bounded text
 -> C2C parser
 -> version/task/iteration/state validation
 -> duplicate/conflict check
 -> session transition
 -> delivery journal accepted
```

## Timeout and cancellation

- every transport operation has explicit timeout/cancellation;
- timeout does not advance protocol state;
- cancellation does not mark a send unsent if submission might already have happened;
- ambiguous send always enters reconciliation flow before retry.

## Fake transport

V0.8A tests should provide a deterministic fake implementation capable of scripting:

```text
open success
user auth required
send success
send ambiguous
PLAN response
DONE response
wrong task response
stale iteration response
timeout
conversation missing
```

The fake exists only for tests and must not become a hidden alternate protocol implementation.

## Manual transport

Manual fallback need not implement browser automation. The CLI may expose the prepared outbound text and accept bounded planner text through stdin/local bounded file using the existing V0.7 session commands.

Do not add duplicate parser/validator logic to the manual path.
