# Use-Case Conventions

## Naming

Use a verb + semantic object for operations:

```text
ConfigureWorkspace
ReadFile
SearchWorkspace
RecordExecution
CreatePairing
RefreshToken
StartTunnel
ResumeConversation
```

Avoid vague verbs such as `Process`, `Handle`, `Manage`, `Do`, or `Execute` unless the domain term itself is that broad.

## Co-location

When a use case needs several dedicated types, keep them together:

```text
Workspace/ReadFile/
├── ReadFileRequest.cs
├── ReadFileResult.cs
├── ReadFileService.cs     # only if service is the chosen local pattern
└── ReadFileValidator.cs   # only if actual validation behavior exists
```

Do not force a `Handler` suffix or mediator abstraction if the repository does not use one. Follow the nearest established implementation pattern.

## One use case, one responsibility

A use-case service orchestrates one bounded operation. It may call policies/ports but should not become a module-wide god service.

Avoid `WorkspaceService` accumulating configure/read/list/search/git behavior simply because all operations mention workspace.

## Contracts

Input/output contracts belong to the use case unless they are deliberately public/shared protocol contracts. Internal use-case results should not become global DTOs.

## Validation

Validate at the layer that owns the rule:

- syntax/transport shape at adapter boundary;
- use-case preconditions in application logic;
- domain/security invariant in the owning Core policy/value object.

Do not duplicate the same rule in MCP, CLI, and Core handlers.