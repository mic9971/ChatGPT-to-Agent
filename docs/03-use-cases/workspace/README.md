# Workspace & Filesystem — Use Cases

| Use case | Title | Priority | Phase | Depends on |
|---|---|---:|---|---|
| `UC-WS-01` | Configure and Bind Workspace | P0 | V0.1 | - |
| `UC-WS-02` | Get Workspace Info | P0 | V0.2 | UC-WS-01 |
| `UC-WS-03` | List Directory | P0 | V0.2 | UC-WS-01 |
| `UC-WS-04` | Read File | P0 | V0.1 | UC-WS-01 |
| `UC-WS-05` | Search Workspace | P0 | V0.2 | UC-WS-03, UC-WS-04 |

Implementation should normally follow dependency order, not simply numeric order if a dependency says otherwise.
