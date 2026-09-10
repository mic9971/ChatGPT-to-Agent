# Antigravity Execution Order

Antigravity should be given one UC at a time in the roadmap order. Before coding a UC it must read:

1. `AGENTS.md`
2. `.agent/rules/00-GLOBAL.md`
3. `.agent/rules/02-USE-CASE-IMPLEMENTATION.md`
4. target UC
5. referenced BR files
6. architecture refs named by that UC
7. nearest source/tests

For each task, Antigravity returns PLAN before edits if the implementation touches a new security boundary, public contract, persistence schema or process lifecycle. For ordinary continuation inside an already approved UC, it may proceed after showing a concise file/test plan.
