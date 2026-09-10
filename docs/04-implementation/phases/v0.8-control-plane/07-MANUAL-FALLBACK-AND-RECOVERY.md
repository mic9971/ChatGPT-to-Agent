# V0.8 Manual Fallback and Recovery

## Purpose

Manual fallback is not an emergency bypass. It is a first-class transport mode that preserves the same C2C validation, conversation binding and evidence rules when browser automation is unavailable or unsafe.

## Manual control flow

```text
1. c2c ensure --json
2. c2c session status/new --json
3. CLI returns exact bounded outbound C2C message
4. Operator pastes message into the bound ChatGPT conversation
5. Operator waits for completed assistant response
6. Operator saves/pipes only the bounded C2C response
7. c2c session accept validates it
8. Agent executes accepted PLAN locally
9. c2c record
10. c2c session executed returns exact EXECUTED message
11. repeat until terminal state
```

If V0.7 uses slightly different final command names, V0.8 must adapt to those names rather than create a parallel protocol path.

## Manual path invariants

- same `TASK_ID` and `ITERATION` checks;
- same parser/renderer;
- same 4 KB control-message cap;
- same execution evidence requirement;
- same conversation binding policy;
- same duplicate/conflict semantics;
- no source/diff/raw-log copy into control messages;
- no secrets on command line when stdin/bounded file is safer.

## When to degrade automatically to manual

Automation may recommend manual fallback after:

- Browser Agent unavailable/disabled;
- semantic composer cannot be located after bounded retries;
- response completion cannot be determined safely;
- send state remains ambiguous after inspection;
- browser navigation repeatedly fails;
- provider UI changes invalidate the known workflow.

The system must not switch to manual by loosening protocol validation.

## User authentication interruptions

When ChatGPT requires any of:

```text
password
passkey
MFA / 2FA
CAPTCHA
account consent
security verification
```

automation returns `CTRL_USER_AUTH_REQUIRED` and pauses. The user completes the challenge directly in the isolated/browser profile. C2C.NET receives no credential/cookie material.

After the user completes authentication:

1. re-open/inspect the same intended conversation;
2. revalidate the conversation binding;
3. inspect pending delivery state;
4. continue from the persisted V0.7 checkpoint.

Do not recreate the task or increment iteration due to auth interruption.

## Ambiguous send recovery

Ambiguous send is the highest-risk browser failure because retry can duplicate an instruction.

Required procedure:

```text
transport reports timeout/cancel/unknown after submit
 -> mark delivery Ambiguous
 -> reopen/inspect same conversation
 -> search latest user message boundary for exact bounded message/hash equivalent
 -> if observed: do not resend
 -> if clearly absent: retry same delivery id/hash once according to policy
 -> if uncertain: manual intervention
```

No automatic retry before observation.

## Partial planner response

If the browser reads a response while ChatGPT is still generating:

- do not call V0.7 `session accept` yet;
- continue waiting for semantic completion/stable response boundary;
- if completion cannot be established, return `CTRL_RESPONSE_TIMEOUT` or manual action required;
- never infer a PLAN from partial prose.

## Wrong conversation detection

Before sending, verify at least:

- ChatGPT origin/host is expected;
- stored conversation reference matches the intended binding when one exists;
- the transport is not on a login/settings/unrelated chat page;
- task binding is still valid locally.

Do not rely on visible chat title as sole identity.

## Browser crash recovery

Browser crash does not imply the last message failed.

```text
browser crash
 -> keep protocol checkpoint unchanged
 -> keep delivery journal status
 -> restart/reopen browser
 -> inspect same conversation
 -> reconcile pending delivery
 -> continue deterministically
```

## Conversation loss

If the conversation is deleted/unavailable:

- do not create a new chat and silently continue;
- invoke V0.7 HANDOFF builder;
- retire old binding;
- open/bind new conversation;
- send HANDOFF;
- require the new planner to re-read MCP evidence.

## Logging

Control recovery logs may contain:

```text
task id
iteration
provider category
failure code
retry count
correlation id
timestamps
message hash
```

They must not contain browser HTML dumps, cookies, passwords, access tokens, raw source/diff/log bodies or full transcripts.
