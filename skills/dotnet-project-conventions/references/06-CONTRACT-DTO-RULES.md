# Contract and DTO Rules

## Boundary ownership

A contract belongs to the boundary whose semantics it represents.

```text
MCP schema          -> Host/Mcp/<Capability>
CLI JSON schema     -> Cli/<Capability>
Provider DTO        -> Infrastructure/<Capability>/<Provider>
Use-case request    -> Core/Application capability/use case
C2C protocol        -> Core/C2C (product-owned protocol)
```

## Do not leak transport types inward

Core/application contracts must not depend on ASP.NET request types, MCP SDK objects, command-line parser models, or provider SDK DTOs.

Adapters map at the edge.

## Avoid universal DTOs

Do not create a single `WorkspaceDto` and reuse it for persistence, MCP, CLI, and internal application flow. Those boundaries evolve for different reasons.

Use separate types when semantics/versioning differ, even if today they have identical fields.

## Public contracts

Version public machine-readable schemas or document compatibility rules. Changes that rename/remove fields require the target UC/ADR and compatibility review.

## Mapping

Prefer small explicit mapping over reflection-heavy generic mappers for security/protocol boundaries. The mapping code makes disclosure and versioning decisions visible in review.