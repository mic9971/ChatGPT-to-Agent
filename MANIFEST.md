# Manifest

Total files: **120**

```text
.agent/rules/00-GLOBAL.md
.agent/rules/01-DOCUMENTATION.md
.agent/rules/02-USE-CASE-IMPLEMENTATION.md
.agent/rules/03-DOTNET.md
.agent/rules/04-SECURITY.md
.agent/rules/05-TESTING.md
.agent/rules/06-GIT-SCOPE.md
.agent/rules/07-REVIEW-DONE.md
.agent/rules/08-ANTIGRAVITY.md
AGENTS.md
MANIFEST.md
README.md
docs/00-governance/00-DOCUMENT-READ-ORDER.md
docs/00-governance/01-USE-CASE-CATALOG.md
docs/00-governance/02-USE-CASE-TEMPLATE.md
docs/00-governance/03-DOCUMENTATION-RULES.md
docs/00-governance/04-GLOSSARY.md
docs/00-governance/05-TRACEABILITY-MATRIX.md
docs/01-architecture/00-VISION.md
docs/01-architecture/01-SRS.md
docs/01-architecture/02-HIGH-LEVEL-ARCHITECTURE.md
docs/01-architecture/03-C2C-PROTOCOL.md
docs/01-architecture/04-MCP-DESIGN.md
docs/01-architecture/05-WORKSPACE-SECURITY.md
docs/01-architecture/06-OAUTH-PAIRING.md
docs/01-architecture/07-TUNNEL-DESIGN.md
docs/01-architecture/08-EXECUTION-REVIEW.md
docs/01-architecture/09-CLI-DESIGN.md
docs/01-architecture/10-AGENT-INTEGRATION.md
docs/01-architecture/11-DATA-MODEL.md
docs/01-architecture/12-ERROR-RECOVERY.md
docs/01-architecture/13-COMPATIBILITY-MATRIX.md
docs/01-architecture/14-ADR-DECISIONS.md
docs/01-architecture/15-NON-FUNCTIONAL-REQUIREMENTS.md
docs/01-architecture/16-RESEARCH-NOTES.md
docs/02-common/00-BR-COMMON.md
docs/02-common/01-BR-SECURITY.md
docs/02-common/02-BR-CONCURRENCY-IDEMPOTENCY.md
docs/02-common/03-BR-C2C-PROTOCOL.md
docs/02-common/04-BR-AUTH.md
docs/02-common/05-BR-EXECUTION-EVIDENCE.md
docs/02-common/06-BR-OBSERVABILITY-ERRORS.md
docs/02-common/07-COMMON-CONTRACTS.md
docs/02-common/08-COMMON-TEST-RULES.md
docs/03-use-cases/agent-integration/README.md
docs/03-use-cases/agent-integration/UC-AGT-01-INSTALL-AGENT-INTEGRATION-PACK.md
docs/03-use-cases/agent-integration/UC-AGT-02-AGENT-STARTS-C2C-TASK.md
docs/03-use-cases/agent-integration/UC-AGT-03-AGENT-EXECUTES-ACCEPTED-PLAN.md
docs/03-use-cases/agent-integration/UC-AGT-04-AGENT-SUBMIT-RESUME-HANDOFF.md
docs/03-use-cases/auth/README.md
docs/03-use-cases/auth/UC-AUTH-01-CREATE-PAIRING-SESSION.md
docs/03-use-cases/auth/UC-AUTH-02-APPROVE-PAIRING-AND-AUTHORIZE-CLIENT.md
docs/03-use-cases/auth/UC-AUTH-03-ISSUE-AND-VALIDATE-ACCESS-TOKEN.md
docs/03-use-cases/auth/UC-AUTH-04-ROTATE-REFRESH-TOKEN.md
docs/03-use-cases/auth/UC-AUTH-05-REVOKE-UNPAIR-CLIENT.md
docs/03-use-cases/c2c/README.md
docs/03-use-cases/c2c/UC-C2C-01-INITIALIZE-C2C-TASK.md
docs/03-use-cases/c2c/UC-C2C-02-ACCEPT-PLANNER-PLAN.md
docs/03-use-cases/c2c/UC-C2C-03-MARK-LOCAL-EXECUTION-STARTED.md
docs/03-use-cases/c2c/UC-C2C-04-SUBMIT-EXECUTED-EVIDENCE-REFERENCE.md
docs/03-use-cases/c2c/UC-C2C-05-REVIEW-EVIDENCE-AND-REPLAN.md
docs/03-use-cases/c2c/UC-C2C-06-COMPLETE-TASK-WITH-DONE.md
docs/03-use-cases/c2c/UC-C2C-07-HANDLE-BLOCKED-OR-ERROR.md
docs/03-use-cases/c2c/UC-C2C-08-RESUME-OR-HANDOFF-TASK.md
docs/03-use-cases/cli/README.md
docs/03-use-cases/cli/UC-CLI-01-SETUP-CLI.md
docs/03-use-cases/cli/UC-CLI-02-START-AND-STOP-RUNTIME.md
docs/03-use-cases/cli/UC-CLI-03-STATUS-AND-DOCTOR.md
docs/03-use-cases/cli/UC-CLI-04-ENSURE-RUNTIME-READY.md
docs/03-use-cases/cli/UC-CLI-05-PAIR-AND-UNPAIR-CLI.md
docs/03-use-cases/cli/UC-CLI-06-MANAGE-C2C-SESSION.md
docs/03-use-cases/cli/UC-CLI-07-RECORD-EVIDENCE-AND-READ-LOCAL-LOGS.md
docs/03-use-cases/execution/README.md
docs/03-use-cases/execution/UC-EXE-01-RECORD-EXECUTION-EVIDENCE.md
docs/03-use-cases/execution/UC-EXE-02-READ-EXECUTION-SUMMARY.md
docs/03-use-cases/execution/UC-EXE-03-READ-NORMALIZED-TEST-STATUS.md
docs/03-use-cases/execution/UC-EXE-04-READ-SANITIZED-EXECUTION-OUTPUT.md
docs/03-use-cases/git/README.md
docs/03-use-cases/git/UC-GIT-01-READ-GIT-STATUS.md
docs/03-use-cases/git/UC-GIT-02-READ-GIT-DIFF.md
docs/03-use-cases/mcp/README.md
docs/03-use-cases/mcp/UC-MCP-01-START-STATELESS-MCP-ENDPOINT.md
docs/03-use-cases/mcp/UC-MCP-02-INVOKE-MCP-TOOL-WITH-AUTHORIZATION.md
docs/03-use-cases/packaging/README.md
docs/03-use-cases/packaging/UC-PKG-01-PUBLISH-SELF-CONTAINED-CLI.md
docs/03-use-cases/packaging/UC-PKG-02-UPGRADE-COMPATIBILITY-CHECK.md
docs/03-use-cases/tunnel/README.md
docs/03-use-cases/tunnel/UC-TUN-01-START-PUBLIC-TUNNEL.md
docs/03-use-cases/tunnel/UC-TUN-02-STOP-OWNED-TUNNEL.md
docs/03-use-cases/tunnel/UC-TUN-03-ENSURE-RECOVER-TUNNEL.md
docs/03-use-cases/workspace/README.md
docs/03-use-cases/workspace/UC-WS-01-CONFIGURE-AND-BIND-WORKSPACE.md
docs/03-use-cases/workspace/UC-WS-02-GET-WORKSPACE-INFO.md
docs/03-use-cases/workspace/UC-WS-03-LIST-DIRECTORY.md
docs/03-use-cases/workspace/UC-WS-04-READ-FILE.md
docs/03-use-cases/workspace/UC-WS-05-SEARCH-WORKSPACE.md
docs/04-implementation/00-ROADMAP.md
docs/04-implementation/01-WORK-BREAKDOWN.md
docs/04-implementation/02-DEFINITION-OF-DONE.md
docs/04-implementation/03-ANTIGRAVITY-EXECUTION-ORDER.md
docs/04-implementation/04-RELEASE-GATES.md
docs/04-implementation/05-ANTIGRAVITY-IMPLEMENTATION-PLAN-v0.1-REFERENCE.md
docs/04-implementation/06-ROADMAP-v0.1-REFERENCE.md
docs/05-testing/00-TEST-MATRIX.md
docs/05-testing/01-SECURITY-ADVERSARIAL-SCENARIOS.md
docs/05-testing/02-E2E-SCENARIOS.md
integrations/antigravity/AGENT.md
integrations/codex/SKILL.md
skills/c2c-dotnet/SKILL.md
skills/dotnet-engineering/SKILL.md
skills/dotnet-engineering/references/01-architecture.md
skills/dotnet-engineering/references/02-csharp-style.md
skills/dotnet-engineering/references/03-async-concurrency.md
skills/dotnet-engineering/references/04-aspnetcore.md
skills/dotnet-engineering/references/05-security.md
skills/dotnet-engineering/references/06-testing.md
skills/dotnet-engineering/references/07-performance.md
skills/dotnet-engineering/references/08-observability.md
skills/dotnet-engineering/references/09-dependencies.md
skills/dotnet-engineering/references/10-review-checklist.md
```

## Use case count by module

- Workspace & Filesystem: 5
- Git Evidence: 2
- MCP Data Plane: 2
- Execution Evidence: 4
- Tunnel & Runtime Connectivity: 3
- Authorization & Pairing: 5
- C2C Control Protocol: 8
- CLI & Local Lifecycle: 7
- Execution Agent Integration: 4
- Packaging & Upgrade: 2
