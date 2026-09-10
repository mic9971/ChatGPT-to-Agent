# CLI & Local Lifecycle — Use Cases

| Use case | Title | Priority | Phase | Depends on |
|---|---|---:|---|---|
| `UC-CLI-01` | Setup CLI | P0 | V0.6 | UC-WS-01 |
| `UC-CLI-02` | Start and Stop Runtime | P0 | V0.6 | UC-MCP-01, UC-TUN-01, UC-TUN-02 |
| `UC-CLI-03` | Status and Doctor | P0 | V0.6 | - |
| `UC-CLI-04` | Ensure Runtime Ready | P0 | V0.6 | UC-TUN-03, UC-CLI-02 |
| `UC-CLI-05` | Pair and Unpair CLI | P1 | V0.6 | UC-AUTH-01, UC-AUTH-05 |
| `UC-CLI-06` | Manage C2C Session | P1 | V0.7 | UC-C2C-01, UC-C2C-08 |
| `UC-CLI-07` | Record Evidence and Read Local Logs | P0 | V0.6 | UC-EXE-01 |

Implementation should normally follow dependency order, not simply numeric order if a dependency says otherwise.
