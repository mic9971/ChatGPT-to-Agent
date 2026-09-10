# Documentation / Traceability Rules

- Each code change names the UC it implements.
- If a reusable rule changes, update the canonical BR first and review all UCs referencing it.
- If a UC acceptance criterion changes, update traceability and tests before declaring done.
- Keep status fields honest: NOT-STARTED -> IMPLEMENTING -> DONE only with evidence.
- Architecture docs explain boundaries; UCs specify implementation behavior; BRs specify shared invariants.
