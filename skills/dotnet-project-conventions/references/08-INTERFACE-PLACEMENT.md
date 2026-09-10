# Interface Placement

## Put the abstraction with its owner

Do not create a global `Interfaces/` folder. Place an interface near the capability/layer that defines the need.

Example:

```text
C2C.Core/Workspace/IWorkspaceAccessPolicy.cs
C2C.Infrastructure/Workspace/WorkspaceAccessPolicy.cs
```

The caller/abstraction owner lives in Core; the concrete filesystem implementation lives in Infrastructure.

## When an interface is justified

Use an interface when it represents one of these boundaries:

- external or nondeterministic resource (filesystem/process/network/time abstraction when required);
- replaceable provider/strategy;
- persistence port;
- transport-independent capability consumed by adapters;
- security/policy boundary where implementation is intentionally separated.

Do not create an interface solely because a class exists or because a mocking library prefers one.

## Naming

Use `I<SemanticCapability>` rather than vague `IManager`, `IHelper`, or `IService` when a more specific role exists.

Examples:

```text
IWorkspaceFileReader
ITunnelProvider
IExecutionStore
IControlPlaneDriver
```

## Provider-specific interfaces

If only one provider needs a low-level helper, keep that abstraction inside the provider implementation until a second real provider proves the common contract.