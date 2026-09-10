# V0.8 Conversation Binding and Delivery Journal

## Goal

Persist only the transport metadata required to resume safely after process/browser failure without duplicating the V0.7 protocol session as a second source of truth.

## Source-of-truth rule

V0.7 `C2CSession` remains authoritative for:

```text
task id
workspace id
iteration
protocol/checkpoint state
accepted plan/evidence references
terminal state
next valid protocol action
```

V0.8 transport persistence must not mirror those fields as independently mutable state.

## Conversation binding

Recommended model:

```text
ControlConversationBinding
- schemaVersion
- workspaceId
- taskId
- provider                 # e.g. chatgpt-web
- conversationReference    # opaque, non-secret
- bindingVersion
- createdAt
- updatedAt
```

Optional diagnostics fields may include safe provider/mode labels, but never browser credential material.

### Invariant

```text
(workspaceId, taskId) -> at most one active conversation binding
```

A conversation reference may not be rebound to another task/workspace without explicit clear/handoff semantics.

## Conversation reference policy

The reference may be a URL or provider-specific opaque id only if it is non-secret and safe to persist. Before persistence:

- require supported provider;
- reject userinfo credentials in URLs;
- reject fragment/query fields known to contain tokens;
- normalize only enough for equality/security checks;
- do not fetch the URL from C2C.NET;
- do not treat possession of the reference as proof of browser authentication.

## Delivery journal

Use a separate bounded journal for ambiguous browser delivery:

```text
ControlDeliveryRecord
- deliveryId
- workspaceId
- taskId
- direction                # outbound/inbound
- messageState             # INIT/PLAN/EXECUTED/etc metadata only
- iteration
- canonicalHash
- status
- attemptCount
- preparedAt
- lastAttemptAt
- observedAt?
- correlationId
```

Do not persist the full control message if V0.7 already stores/reconstructs the bounded envelope. If content is needed for crash-safe replay, store only the already-approved bounded rendered control message and enforce the same 4 KB hard cap; never store source/diff/raw logs.

## Atomic persistence

Reuse the repository's app-state ownership and atomic write pattern:

```text
write temp
 -> flush/fsync where practical
 -> atomic replace
```

Conceptual location:

```text
<app-state>/workspaces/<workspace-id>/control/
  <task-id>/binding.json
  <task-id>/deliveries.json
```

The exact path must reuse the implemented `IAppStatePathProvider`/equivalent and existing security conventions.

## Optimistic concurrency

Transport metadata updates use a monotonic `bindingVersion` or expected-version compare. Older browser workers must not overwrite a newer binding/journal state.

## Send ambiguity algorithm

When transport returns timeout/cancellation after possible submit:

```text
Prepared
  -> Submitting
  -> Ambiguous
       |
       v
inspect conversation
  ├─ exact canonical hash/message observed -> ObservedSent
  ├─ clearly absent and safe to retry      -> Prepared/retry same deliveryId
  └─ cannot determine                      -> manual fallback / BLOCKED
```

Never increment C2C iteration because of a delivery retry.

## Duplicate behavior

- same task/iteration/state/hash -> idempotent;
- same task/iteration/state but different hash -> `C2C_PROTOCOL_CONFLICT`;
- same delivery id with different hash -> transport conflict;
- stale binding version -> concurrency conflict;
- accepted inbound duplicate with same hash -> idempotent success.

## Clear and handoff

Clearing an unavailable conversation binding does not reset the C2C task. The safe sequence is:

```text
confirm unavailable/replaced
 -> build V0.7 HANDOFF
 -> clear/retire old binding
 -> create new conversation
 -> bind new reference to same workspace/task
 -> send HANDOFF
```

Keep retired binding metadata only as bounded audit metadata if useful; do not retain browser transcript/cookies.

## Security canaries

Tests must prove binding/journal persistence never contains:

```text
Bearer tokens
OAuth access/refresh tokens
pairing codes
browser cookies
password/passkey/MFA material
source body canaries
diff body canaries
raw log canaries
```
