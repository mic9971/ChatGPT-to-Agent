# 02 — High-Level Architecture

## Context

```text
┌────────────────────────────────────────────┐
│ Planning / Review Client                   │
│ ChatGPT Web today; other reviewers later  │
└──────────────┬────────────────▲────────────┘
               │                │
      MCP      │                │ C2C text/control
   Data Plane  │                │ plane
               v                │
┌────────────────────────────────────────────┐
│ C2C.NET                                    │
│                                            │
│ C2C.Host                                   │
│ - ASP.NET Core loopback host               │
│ - stateless MCP HTTP endpoint              │
│ - auth/discovery endpoints                 │
│ - local-only admin endpoint                │
│                                            │
│ C2C.Core                                   │
│ - protocol/state contracts                 │
│ - workspace policies                       │
│ - execution contracts                      │
│                                            │
│ C2C.Infrastructure                         │
│ - filesystem canonicalization              │
│ - search/Git adapters                      │
│ - persistence/process/tunnel adapters      │
│                                            │
│ C2C.Security                               │
│ - OAuth/pairing/token/scopes               │
│                                            │
│ C2C.Cli                                    │
│ - setup/ensure/record/session/...           │
└─────────────────────┬──────────────────────┘
                      │ read-only
                      v
┌────────────────────────────────────────────┐
│ Local Workspace                            │
└─────────────────────▲──────────────────────┘
                      │ edit/build/test/git
┌─────────────────────┴──────────────────────┐
│ Execution Agent                            │
│ Antigravity / Codex / Claude / Gemini ...  │
└────────────────────────────────────────────┘
```

## Proposed solution structure

```text
C2CNet.sln

src/
  C2C.Host/
  C2C.Core/
  C2C.Infrastructure/
  C2C.Security/
  C2C.Cli/
  C2C.AgentIntegration/

tests/
  C2C.Core.Tests/
  C2C.Security.Tests/
  C2C.IntegrationTests/

skills/
integrations/
docs/
```

Avoid over-segmenting into dozens of assemblies. Split only where a boundary materially improves dependency direction, security review, testability or packaging.

## Dependency rules

`C2C.Core` owns interfaces/contracts and depends on no ASP.NET, Cloudflare, Git executable or concrete storage.

`C2C.Infrastructure` implements filesystem, Git, search, tunnel, process and persistence abstractions from Core.

`C2C.Security` implements auth/pairing/token services and may depend on ASP.NET security primitives where necessary, but workspace business/security rules remain in Core.

`C2C.Host` is the composition root and HTTP/MCP adapter.

`C2C.Cli` orchestrates local lifecycle through application services; it must not duplicate bridge business logic.

## Runtime processes

Target V1 can be a single self-contained `c2c` binary with subcommands. `start`/`ensure` launches a bridge child process or daemonized process and optionally `cloudflared`. Avoid requiring a permanently installed Windows service/macOS daemon for V1; platform auto-start is optional later.

## MCP 2026-07-28 implication

The MCP core is stateless: do not design around `Mcp-Session-Id` or initialize-session storage. Each request carries enough protocol/client context to be processed independently. C2C task checkpointing is application state and remains separate from MCP transport state.

## Local admin surface

A local-only admin API can expose health/runtime diagnostics to the CLI. It SHALL:

- bind loopback only;
- require a random local admin token for non-trivial operations;
- reject proxy-forwarded request headers;
- avoid returning workspace absolute paths/secrets in unauthenticated probes;
- use stable machine-readable error codes.
