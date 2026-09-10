# C2C Control Protocol — Use Cases

| Use case | Title | Priority | Phase | Depends on |
|---|---|---:|---|---|
| `UC-C2C-01` | Initialize C2C Task | P1 | V0.7 | UC-WS-01 |
| `UC-C2C-02` | Accept Planner PLAN | P1 | V0.7 | UC-C2C-01 |
| `UC-C2C-03` | Mark Local Execution Started | P1 | V0.7 | UC-C2C-02 |
| `UC-C2C-04` | Submit EXECUTED Evidence Reference | P1 | V0.7 | UC-EXE-01, UC-C2C-03 |
| `UC-C2C-05` | Review Evidence and Replan | P1 | V0.7 | UC-C2C-04, UC-GIT-02, UC-EXE-02, UC-EXE-03 |
| `UC-C2C-06` | Complete Task with DONE | P1 | V0.7 | UC-C2C-05 |
| `UC-C2C-07` | Handle BLOCKED or ERROR | P1 | V0.7 | UC-C2C-01 |
| `UC-C2C-08` | Resume or HANDOFF Task | P1 | V0.7 | UC-C2C-01 |

Implementation should normally follow dependency order, not simply numeric order if a dependency says otherwise.
