# Feature / Folder Rules

## Capability first

A folder should normally answer **what business/system capability does this code serve?** before **what technical type is it?**

Good:

```text
Workspace/
Authorization/
Execution/
Tunnel/
ControlPlane/
```

Weak when used globally across unrelated capabilities:

```text
Models/
Services/
Repositories/
Handlers/
Helpers/
Utils/
```

## Flat first, split when cohesion improves

Do not create deep folders for a capability with only a few closely related types. Start flat when clear:

```text
Workspace/
├── IWorkspaceAccessPolicy.cs
├── WorkspaceConfig.cs
└── WorkspaceConfigurator.cs
```

When a capability contains multiple independent use cases and the folder becomes hard to navigate, split by use case:

```text
Workspace/
├── ConfigureWorkspace/
├── ReadFile/
└── SearchWorkspace/
```

The trigger is loss of cohesion/navigation, not an arbitrary file count.

## Existing structure wins

Do not move existing files just to make the tree prettier. Structural migration requires an explicit UC/ADR/refactor scope and regression tests.

## External provider folders

Provider-specific code may nest below the owning capability:

```text
Tunnel/
└── Cloudflare/
    └── CloudflareTunnelProvider.cs
```

Do not put provider code in a generic `Integrations/` root when a clear capability owns it.