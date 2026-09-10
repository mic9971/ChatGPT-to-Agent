# Naming Conventions

This file complements the language-level naming rules in `csharp-clean-code`. It governs architectural/semantic names.

## Name by responsibility

Prefer names that communicate business/system responsibility:

```text
WorkspaceConfigurator
WorkspaceAccessPolicy
ExecutionRecord
TunnelProvider
ControlConversationBinding
```

Avoid names that only say “does something”:

```text
WorkspaceManager
CommonService
FileHelper
GeneralProcessor
Utility
```

## Use-case names

Use `Verb + Object`:

```text
ReadFile
SearchWorkspace
RecordExecution
RefreshToken
```

Suffixes may clarify role:

- `Request` / `Result` for application operations;
- `Options` for runtime configuration;
- `Record` / `Snapshot` for persisted or immutable evidence/state when semantically true;
- `Envelope` for protocol wrapper;
- `Document` for persistence-only serialized shape;
- `Dto` only for an explicit external/transport DTO where a better semantic name is unavailable.

## Avoid redundant suffixes

Do not use `Model`, `Data`, `Info`, `Object`, `Item` by default. They are acceptable only when the domain/protocol specifically defines that term (for example `WorkspaceInfo` as an intentionally public operation result).

## Provider names

Provider-specific implementations include the provider when it matters:

```text
CloudflareTunnelProvider
JsonWorkspaceConfigStore
Sha256WorkspaceIdentityFactory
```

The interface remains provider-neutral.