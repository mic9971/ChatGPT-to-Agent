# V0.7 Protocol and Session Architecture

## Core design

V0.7 is intentionally transport-neutral.

```text
Execution Agent / User
        |
        | local call
        v
     C2C.Cli
        |
        v
  C2C.Core/C2C
  - protocol models
  - parser/renderer
  - transition validator
  - session service
  - recovery/handoff contracts
        |
        v
C2C.Infrastructure/C2C
  - atomic JSON session store
  - executor lease store
  - safe local clock/id generation adapters
        |
        +----> existing Execution query/store
        +----> existing Workspace identity/context
```

The future browser/control-plane adapter is deliberately absent.

## Proposed capability layout

```text
src/
  C2C.Core/
    C2C/
      Protocol/
      Sessions/
      Recovery/

  C2C.Infrastructure/
    C2C/
      Persistence/
      Leasing/

  C2C.Cli/
    Session/

tests/
  C2C.Core.Tests/
    C2C/
  C2C.IntegrationTests/
    C2C/
```

Do not create global `Models/`, `Services/`, `Helpers/`, `Managers/` or `Interfaces/` dumping folders.

## Suggested Core contracts

Names may be adjusted to match existing conventions after baseline inspection, but responsibilities must remain separated.

```csharp
public interface IC2CMessageParser
{
    OperationResult<C2CMessage> Parse(string text);
}

public interface IC2CMessageRenderer
{
    OperationResult<string> Render(C2CMessage message);
}

public interface IC2CProtocolValidator
{
    OperationResult ValidateTransition(C2CSession current, C2CMessage incoming);
}

public interface IC2CSessionStore
{
    Task<C2CSession?> GetAsync(string workspaceId, string taskId, CancellationToken ct);
    Task SaveAsync(C2CSession session, long expectedVersion, CancellationToken ct);
}

public interface IC2CSessionService
{
    // Create task / accept plan / mark executing / attach execution / complete / block / fail.
}

public interface IC2CSessionRecovery
{
    Task<OperationResult<C2CRecoveryResult>> LoadAsync(string taskId, CancellationToken ct);
}

public interface IC2CHandoffBuilder
{
    OperationResult<string> Build(C2CSession session);
}
```

Avoid a giant manager interface. Keep state mutation behind one cohesive session application service and keep parser/renderer/state-machine logic independently testable.

## Model taxonomy

### Protocol model

`C2CMessage` describes the bounded provider-neutral envelope and state-specific sections.

### Session model

`C2CSession` is persisted local application state. It is not the same thing as a control message.

### Evidence reference

The session stores only immutable execution ids and bounded digests/summaries. It does not duplicate execution artifact bodies.

### Conversation reference

`conversationReference` may be stored as an opaque optional local value, but V0.7 does not know how to open a browser or send to ChatGPT. V0.8 consumes this field.

## Dependency rules

`C2C.Core` may depend on existing Core abstractions such as workspace identity and execution query contracts, but must not depend on:

- ASP.NET Core;
- Cloudflare;
- browser automation;
- Antigravity;
- Codex;
- concrete JSON filesystem implementation;
- ChatGPT web session/cookies;
- OAuth implementation classes.

Infrastructure implements persistence/lease contracts. CLI adapts Core services to human/agent commands.

## App-state ownership

Use the existing `IAppStatePathProvider`/equivalent repository mechanism. Conceptually:

```text
C2C.NET/
  workspaces/
    <workspace-id>/
      session.json                # only if one active task model is chosen
      sessions/
        <task-id>.json            # preferred if concurrent historical tasks are retained
      leases/
        <task-id>.lease
```

The exact shape must be chosen after inspecting current app-state conventions. Do not introduce a database in V0.7.

## Security boundary

Control text is untrusted input even when it came from ChatGPT or a local user. Parser and validator enforce protocol structure. The protocol must never be allowed to mutate workspace security policy, grant scopes, or create write/exec MCP capabilities.

## V0.8 compatibility

V0.7 must expose enough provider-neutral operations that V0.8 can later implement:

```text
open/attach conversation
send rendered C2C message
receive planner text
parse + validate
advance session
```

No browser-specific type should appear in the V0.7 Core contracts.
