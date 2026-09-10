# 17 — Architecture Decision Register

## ADR-001 — .NET 10 / C# 14
**Status:** Accepted.  
Use current stable .NET 10 baseline for a new application/tool. Do not use new C# 14 syntax merely for novelty; prefer readability and analyzer support.

## ADR-002 — Official C# MCP SDK
**Status:** Accepted.  
Use the official `modelcontextprotocol/csharp-sdk`, with `ModelContextProtocol.AspNetCore` for remote HTTP MCP.

## ADR-003 — MCP 2026-07-28 stateless core
**Status:** Accepted.  
Do not model transport around old initialize/session identifiers. C2C task state is separate application state.

## ADR-004 — Read-only MCP V1
**Status:** Accepted.  
No shell/write/delete/commit tools. Any future change requires a new threat model and explicit product decision.

## ADR-005 — One bridge per workspace
**Status:** Accepted.  
Simplifies authorization, containment and threat reasoning.

## ADR-006 — Agent-agnostic execution
**Status:** Accepted.  
No proprietary coding-agent SDK in Core. CLI/JSON + instruction packs first.

## ADR-007 — CIMD-first auth registration
**Status:** Accepted with interoperability validation.  
Current MCP direction deprecates DCR. DCR remains an optional compatibility profile only.

## ADR-008 — Filesystem persistence V1
**Status:** Accepted.  
No DB/Redis. Use schema-versioned atomic local state. Security-sensitive storage may use OS protection/keychain.

## ADR-009 — Cloudflare is a provider, not core
**Status:** Accepted.  
`ITunnelProvider` abstraction; Quick Tunnel initial implementation.

## ADR-010 — Evidence-based review
**Status:** Accepted.  
Planner inspects actual diff/test/execution evidence rather than trusting executor narrative.

## ADR-011 — OpenIddict is an evaluated building block, not assumed complete solution
**Status:** Accepted.  
Use where it reduces cryptographic/auth risk, but verify current MCP-specific metadata/registration requirements. Avoid forcing architecture around unsupported features.
