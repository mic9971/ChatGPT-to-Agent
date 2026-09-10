# Glossary

- **Planner/Reviewer:** ChatGPT-side reasoning/review role.
- **Execution Agent:** local coding agent that edits/builds/tests (Antigravity, Codex, etc.).
- **Bridge:** C2C.NET local ASP.NET Core process exposing MCP and local lifecycle APIs.
- **Workspace:** one configured canonical repository/project root served by a bridge instance.
- **Data Plane:** read-only MCP path used by the planner to inspect evidence.
- **Control Plane:** bounded C2C messages coordinating task state.
- **Execution Record:** immutable local evidence metadata for one executor iteration.
- **Artifact:** sanitized/restricted output associated with an execution record.
- **BR:** reusable Business Rule.
- **UC:** Use Case implementation unit.
