# Domain / Core Model Rules

## What belongs in Core

A Core model represents stable system meaning independent of ASP.NET Core, filesystem implementation, Git executable, Cloudflare, Antigravity, or any transport library.

Good candidates:

- value objects with validation/identity (`WorkspaceId`);
- protocol state/envelopes owned by C2C.NET;
- execution evidence contracts owned by the product;
- policy results/error semantics that multiple adapters consume.

## What does not belong in Core

- HTTP request/response DTOs;
- MCP SDK types;
- CLI parser option objects;
- EF/database entities when they only exist for persistence;
- Cloudflare/provider response types;
- browser automation selectors/models.

## Entity/value-object discipline

Create a rich entity/value object only when it owns a real invariant. Do not wrap every string/int into a type without a domain/security reason.

Prefer immutable/value-like contracts for identifiers, protocol messages, snapshots, and evidence records when mutation is not part of the lifecycle.

## No anemic `Models/` bucket

Keep domain/core types under the capability they describe:

```text
Core/Workspace/WorkspaceId.cs
Core/Execution/ExecutionRecord.cs
Core/C2C/C2CEnvelope.cs
```

not:

```text
Core/Models/WorkspaceId.cs
Core/Models/ExecutionRecord.cs
```