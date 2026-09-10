# 00 — Vision

## Problem

Current coding agents are strong executors but may spend expensive reasoning capacity on architecture planning and review. `codex-with-chatgpt` demonstrates a useful split: use ChatGPT Web as a planning/review brain while the coding harness retains all execution power. C2C.NET generalizes this idea and rebuilds the bridge/runtime in .NET.

## Vision statement

Build a small, security-first, agent-agnostic local runtime that lets a planning/review client inspect a local workspace through read-only MCP while an independent execution agent performs edits, builds, tests, and Git operations.

## Product boundaries

C2C.NET **is**:

- a local ASP.NET Core bridge;
- a stateless MCP read-only server;
- a workspace security boundary;
- an OAuth/pairing integration surface for remote MCP clients;
- an execution-evidence store/reader;
- a CLI and lifecycle manager;
- a transport-independent C2C control protocol specification;
- an integration surface for coding agents.

C2C.NET **is not**:

- an LLM or coding model;
- a remote code execution service;
- a general shell exposed to ChatGPT;
- a cloud IDE;
- a multi-tenant source hosting service;
- a replacement for GitHub/GitLab;
- a reason to upload a repository to an external server.

## Target user experience

```text
$ c2c setup
✓ Workspace detected
✓ Security policy validated
✓ Bridge started
✓ Secure public connection established
✓ ChatGPT/MCP connection paired
✓ File-read test passed
Ready.
```

Then an execution agent can run a task loop:

```text
INIT -> PLAN -> EXECUTE locally -> RECORD -> EXECUTED -> REVIEW -> DONE/PLAN/BLOCKED
```

## Design goal: agent-agnostic

The core must not reference a Codex SDK or Antigravity SDK. Agent integration should be achieved through stable CLI/JSON contracts and small instruction packs. This allows the same runtime to work with any agent capable of:

- invoking commands;
- editing a workspace;
- reading a plan from the control channel;
- emitting a C2C state message;
- recording execution evidence through the CLI.

## Success criteria for V1

- End-to-end ChatGPT remote MCP connection to a local workspace.
- Read-only MCP tools with strong path/sensitive-content containment.
- Secure OAuth/pairing and revocation.
- Tunnel lifecycle with a provider abstraction.
- Execution evidence independently readable by the reviewer.
- Resumable task checkpoints without dumping logs or source into control messages.
- At least one non-Codex execution-agent integration (Antigravity first).
- Cross-platform self-contained releases for macOS arm64/x64, Windows x64, Linux x64/arm64.
