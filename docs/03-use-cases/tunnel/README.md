# Tunnel & Runtime Connectivity — Use Cases

| Use case | Title | Priority | Phase | Depends on |
|---|---|---:|---|---|
| `UC-TUN-01` | Start Public Tunnel | P1 | V0.4 | UC-MCP-01 |
| `UC-TUN-02` | Stop Owned Tunnel | P1 | V0.4 | UC-TUN-01 |
| `UC-TUN-03` | Ensure/Recover Tunnel | P1 | V0.4 | UC-TUN-01, UC-TUN-02, UC-MCP-01 |

Implementation should normally follow dependency order, not simply numeric order if a dependency says otherwise.
