# Detailed Implementation Roadmap

## Phase V0.1 — Foundation / secure local read

1. `UC-WS-01` Configure and Bind Workspace
2. `UC-WS-04` Read File (Core service + adversarial path tests)
3. `UC-MCP-01` Stateless MCP endpoint
4. Expose `workspace_info` (`UC-WS-02`) and `read_file`

**Gate:** local MCP read works; traversal/absolute/symlink/sensitive cases fail closed; no write/exec tool exists.

## Phase V0.2 — Workspace discovery + Git evidence

5. `UC-WS-03` List Directory
6. `UC-WS-05` Search Workspace
7. `UC-GIT-01` Git Status
8. `UC-GIT-02` Git Diff

**Gate:** denied names/content never leak through list/search/Git.

## Phase V0.3 — Execution evidence

9. `UC-EXE-01` Record Execution
10. `UC-EXE-02` Summary
11. `UC-EXE-03` Test Status
12. `UC-EXE-04` Sanitized Output

**Gate:** immutable evidence records, idempotency, sanitizer/restricted artifacts proven.

## Phase V0.4 — Runtime/tunnel

13. `UC-TUN-01..03`

## Phase V0.5 — Authorization/pairing

14. `UC-AUTH-01..05`
15. Finish protected-call path in `UC-MCP-02`

## Phase V0.6 — CLI

16. `UC-CLI-01..07`

## Phase V0.7 — C2C protocol/checkpoint

17. `UC-C2C-01..08`

## Phase V0.8 — Agent integration

18. `UC-AGT-01..04` (Antigravity first)

## Phase V0.9 — Packaging

19. `UC-PKG-01`
20. `UC-PKG-02` only when the first real state migration is needed.

Do not implement a later phase merely because an agent has spare context. Each gate must be reviewed before the next security boundary is exposed.
