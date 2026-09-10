# 15 — Antigravity Implementation Plan

This plan is intentionally incremental. Antigravity must not scaffold every future module at once.

## Operating rule

For each slice: inspect docs -> write a short file-level plan -> implement only that slice -> run focused tests -> summarize evidence. No unrelated refactor, no commit unless explicitly requested.

## Slice A — Foundation

Create:

```text
C2CNet.sln
Directory.Build.props
Directory.Packages.props
global.json
src/C2C.Core
src/C2C.Infrastructure
src/C2C.Host
src/C2C.Cli
tests/C2C.Core.Tests
tests/C2C.IntegrationTests
```

Target `net10.0`; nullable enabled; analyzers enabled; deterministic build; Central Package Management.

Do not create Security/AgentIntegration projects until the first secure local MCP slice is proven unless a dependency boundary clearly requires them.

Acceptance: clean restore/build/test with one trivial health test.

## Slice B — Workspace boundary

Implement contracts:

```text
IWorkspaceContext
ICanonicalPathResolver
IWorkspaceAccessPolicy
ISensitivePathPolicy
```

Implement canonicalization + containment and adversarial unit tests. No MCP yet if security primitives are not green.

Acceptance: traversal/absolute/symlink/sensitive cases fail closed.

## Slice C — MCP local

Add official MCP C# SDK HTTP package to Host. Implement `workspace_info` and `read_file` as thin adapters over Core services.

Acceptance: integration test calls MCP tool through TestServer/real HTTP test host and verifies safe read + denied read.

## Slice D — Listing/search/Git

Implement one capability at a time. Git diff must use the filtered-path approach described in security design.

Acceptance: denied paths never appear in list/search/diff fixtures.

## Slice E — Execution records

Implement local schema/versioned persistence, atomic writes, idempotent record creation, sanitized artifacts, `c2c record/session` JSON contract.

Acceptance: repeated record key yields same execution id; private-key log is `restricted`.

## Slice F — Lifecycle/tunnel

Implement process runner, Quick Tunnel provider, ownership markers, `start/stop/status/doctor/ensure`.

Acceptance: fake-provider integration tests first; real cloudflared smoke test optional/manual.

## Slice G — Authorization

Before coding, verify actual ChatGPT MCP client registration behavior in the target environment. Implement current MCP auth discovery/issuer requirements. Prefer CIMD; only enable DCR compatibility if the target client demonstrably requires it.

Acceptance: auth integration suite for 401/403, PKCE, issuer mismatch, expiry, refresh replay, pairing.

## Slice H — C2C protocol

Implement parser/state/checkpoint/HANDOFF after data plane and evidence are stable.

Acceptance: deterministic transition tests and crash/resume fixtures.

## Slice I — Antigravity end-to-end

Use `integrations/antigravity/AGENT.md`. Prove a small sample change:

```text
INIT
-> planner reads MCP
-> PLAN
-> Antigravity edits/builds/tests
-> c2c record
-> EXECUTED
-> planner reads diff/test
-> DONE
```

## Agent stop conditions

Stop and report instead of guessing when:

- current MCP SDK behavior conflicts with docs;
- auth client interoperability is unknown;
- a proposed change weakens read-only boundary;
- a platform path canonicalization behavior cannot be tested;
- a dependency would add a large runtime/maintenance burden without a documented need.
