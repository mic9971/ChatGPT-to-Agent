# Use Case Catalog

Total detailed use cases: **50**.

| Module | Use case | Title | Priority | Phase | Status |
|---|---|---|---:|---|---|
| Workspace & Filesystem | `UC-WS-01` | Configure and Bind Workspace | P0 | V0.1 | DESIGN-READY / NOT-STARTED |
| Workspace & Filesystem | `UC-WS-02` | Get Workspace Info | P0 | V0.2 | DESIGN-READY / NOT-STARTED |
| Workspace & Filesystem | `UC-WS-03` | List Directory | P0 | V0.2 | DESIGN-READY / NOT-STARTED |
| Workspace & Filesystem | `UC-WS-04` | Read File | P0 | V0.1 | DESIGN-READY / NOT-STARTED |
| Workspace & Filesystem | `UC-WS-05` | Search Workspace | P0 | V0.2 | DESIGN-READY / NOT-STARTED |
| Git Evidence | `UC-GIT-01` | Read Git Status | P0 | V0.2 | DESIGN-READY / NOT-STARTED |
| Git Evidence | `UC-GIT-02` | Read Git Diff | P0 | V0.2 | DESIGN-READY / NOT-STARTED |
| MCP Data Plane | `UC-MCP-01` | Start Stateless MCP Endpoint | P0 | V0.1 | DESIGN-READY / NOT-STARTED |
| MCP Data Plane | `UC-MCP-02` | Invoke MCP Tool with Authorization | P0 | V0.5 | DESIGN-READY / NOT-STARTED |
| Execution Evidence | `UC-EXE-01` | Record Execution Evidence | P0 | V0.3 | DESIGN-READY / NOT-STARTED |
| Execution Evidence | `UC-EXE-02` | Read Execution Summary | P0 | V0.3 | DESIGN-READY / NOT-STARTED |
| Execution Evidence | `UC-EXE-03` | Read Normalized Test Status | P0 | V0.3 | DESIGN-READY / NOT-STARTED |
| Execution Evidence | `UC-EXE-04` | Read Sanitized Execution Output | P0 | V0.3 | DESIGN-READY / NOT-STARTED |
| Tunnel & Runtime Connectivity | `UC-TUN-01` | Start Public Tunnel | P1 | V0.4 | DESIGN-READY / NOT-STARTED |
| Tunnel & Runtime Connectivity | `UC-TUN-02` | Stop Owned Tunnel | P1 | V0.4 | DESIGN-READY / NOT-STARTED |
| Tunnel & Runtime Connectivity | `UC-TUN-03` | Ensure/Recover Tunnel | P1 | V0.4 | DESIGN-READY / NOT-STARTED |
| Authorization & Pairing | `UC-AUTH-01` | Create Pairing Session | P1 | V0.5 | DESIGN-READY / NOT-STARTED |
| Authorization & Pairing | `UC-AUTH-02` | Approve Pairing and Authorize Client | P1 | V0.5 | DESIGN-READY / NOT-STARTED |
| Authorization & Pairing | `UC-AUTH-03` | Issue and Validate Access Token | P1 | V0.5 | DESIGN-READY / NOT-STARTED |
| Authorization & Pairing | `UC-AUTH-04` | Rotate Refresh Token | P1 | V0.5 | DESIGN-READY / NOT-STARTED |
| Authorization & Pairing | `UC-AUTH-05` | Revoke / Unpair Client | P1 | V0.5 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-01` | Initialize C2C Task | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-02` | Accept Planner PLAN | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-03` | Mark Local Execution Started | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-04` | Submit EXECUTED Evidence Reference | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-05` | Review Evidence and Replan | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-06` | Complete Task with DONE | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-07` | Handle BLOCKED or ERROR | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| C2C Control Protocol | `UC-C2C-08` | Resume or HANDOFF Task | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-01` | Start Control Session | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-02` | Open or Attach Planner Conversation | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-03` | Send C2C Control Message | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-04` | Receive and Validate Planner Response | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-05` | Run C2C Iteration Control Loop | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-06` | Resume Existing Conversation | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-07` | Handoff to Replacement Conversation | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Control Plane Automation | `UC-CTRL-08` | Recover Control Driver Failure | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-01` | Setup CLI | P0 | V0.6 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-02` | Start and Stop Runtime | P0 | V0.6 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-03` | Status and Doctor | P0 | V0.6 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-04` | Ensure Runtime Ready | P0 | V0.6 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-05` | Pair and Unpair CLI | P1 | V0.6 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-06` | Manage C2C Session | P1 | V0.7 | DESIGN-READY / NOT-STARTED |
| CLI & Local Lifecycle | `UC-CLI-07` | Record Evidence and Read Local Logs | P0 | V0.6 | DESIGN-READY / NOT-STARTED |
| Execution Agent Integration | `UC-AGT-01` | Install Agent Integration Pack | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Execution Agent Integration | `UC-AGT-02` | Agent Starts C2C Task | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Execution Agent Integration | `UC-AGT-03` | Agent Executes Accepted PLAN | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Execution Agent Integration | `UC-AGT-04` | Agent Submit / Resume / Handoff | P1 | V0.8 | DESIGN-READY / NOT-STARTED |
| Packaging & Upgrade | `UC-PKG-01` | Publish Self-Contained CLI | P1 | V0.9 | DESIGN-READY / NOT-STARTED |
| Packaging & Upgrade | `UC-PKG-02` | Upgrade / Compatibility Check | P2 | V1.x | DESIGN-READY / NOT-STARTED |
