# Traceability Matrix

This matrix maps functional requirements to detailed use cases. It is the primary completeness check before implementation/release.

| Requirement | Covered by use cases |
|---|---|
| FR-01 Workspace binding | `UC-WS-01`, `UC-WS-02` |
| FR-02 Loopback host | `UC-MCP-01`, `UC-CLI-02` |
| FR-03 MCP read-only tools | `UC-WS-02`, `UC-WS-03`, `UC-WS-04`, `UC-WS-05`, `UC-GIT-01`, `UC-GIT-02`, `UC-EXE-02`, `UC-EXE-03`, `UC-EXE-04` |
| FR-04 Path containment | `UC-WS-01`, `UC-WS-03`, `UC-WS-04`, `UC-WS-05`, `UC-GIT-01`, `UC-GIT-02` |
| FR-05 Sensitive content | `UC-WS-03`, `UC-WS-04`, `UC-WS-05`, `UC-GIT-01`, `UC-GIT-02`, `UC-EXE-04` |
| FR-06 Bounded outputs | `UC-WS-03`, `UC-WS-04`, `UC-WS-05`, `UC-GIT-01`, `UC-GIT-02`, `UC-EXE-04` |
| FR-07 Git evidence | `UC-GIT-01`, `UC-GIT-02` |
| FR-08 Execution record | `UC-EXE-01`, `UC-EXE-02` |
| FR-09 Execution log sanitizer | `UC-EXE-04` |
| FR-10 C2C task protocol | `UC-C2C-01`, `UC-C2C-02`, `UC-C2C-03`, `UC-C2C-04`, `UC-C2C-05`, `UC-C2C-06`, `UC-C2C-07` |
| FR-11 Checkpoint/resume | `UC-C2C-08`, `UC-CLI-06`, `UC-CTRL-06`, `UC-CTRL-07` |
| FR-12 Auth discovery | `UC-AUTH-02`, `UC-AUTH-03` |
| FR-13 Client registration strategy | `UC-AUTH-02` |
| FR-14 Pairing | `UC-AUTH-01`, `UC-AUTH-02`, `UC-CLI-05` |
| FR-15 Tunnel provider | `UC-TUN-01`, `UC-TUN-02`, `UC-TUN-03` |
| FR-16 CLI | `UC-CLI-01`, `UC-CLI-02`, `UC-CLI-03`, `UC-CLI-04`, `UC-CLI-05`, `UC-CLI-06`, `UC-CLI-07` |
| FR-17 Machine-readable mode | `UC-CLI-01`, `UC-CLI-03`, `UC-CLI-04`, `UC-CLI-06`, `UC-CLI-07` |
| FR-18 Automated control transport | `UC-CTRL-01`, `UC-CTRL-02`, `UC-CTRL-03`, `UC-CTRL-04`, `UC-CTRL-05`, `UC-CTRL-08`, `UC-AGT-02`, `UC-AGT-04` |
| FR-19 Conversation binding/recovery | `UC-CTRL-02`, `UC-CTRL-06`, `UC-CTRL-07`, `UC-CTRL-08` |
| FR-20 Browser authentication boundary | `UC-CTRL-02`, `UC-CTRL-08`, `UC-AUTH-01`, `UC-AUTH-02` |
