# Document Read Order

For any implementation task, read documents in this order:

1. Root `AGENTS.md`.
2. Applicable `.agent/rules/*` files.
3. `docs/00-governance/01-USE-CASE-CATALOG.md`.
4. The target use-case file under `docs/03-use-cases/<module>/`.
5. Every BR file referenced by that use case under `docs/02-common/`.
6. The minimal architecture documents referenced by the use case.
7. Existing source/tests nearest the change.

Agents SHALL NOT scan every design file by default. The use-case document is the implementation entry point and explicitly names its dependencies.
