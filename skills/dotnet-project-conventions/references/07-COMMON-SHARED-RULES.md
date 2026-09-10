# Common / Shared Rules

## Common must be earned

`Common`, `Shared`, and `SharedKernel` are not overflow folders. A type may move there only when:

1. at least two real capability owners need it;
2. they need the **same semantics**, not merely the same property shape;
3. the abstraction is stable enough to have one owner/contract;
4. sharing reduces coupling rather than creating a central dependency magnet.

If any condition is unclear, keep the type inside the capability that currently owns it.

## Good cross-capability candidates

Examples can include stable operation primitives, strongly defined identifiers used across protocol boundaries, or product-wide error/result primitives.

Even then, prefer precise naming such as:

```text
Common/OperationResult.cs
Common/OperationError.cs
```

not:

```text
Common/CommonModel.cs
Common/SharedHelper.cs
```

## Duplication vs wrong abstraction

Small duplication is cheaper than premature semantic coupling. Do not introduce a generic helper/base class only to remove a few repeated lines when the capabilities may evolve differently.

Promote after the common concept is understood, not before.

## No re-export dumping ground

Do not use Common to hide dependency-direction violations (for example moving Infrastructure DTOs to Common so Core can reference them). Fix the boundary instead.