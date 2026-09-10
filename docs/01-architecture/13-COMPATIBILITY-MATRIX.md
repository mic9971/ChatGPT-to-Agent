# 16 — Compatibility Matrix

This is the design-time matrix. During implementation, Antigravity must replace assumptions with exact source/test references from the original repository and current MCP SDK.

| Original behavior | C2C.NET target | Decision |
|---|---|---|
| ChatGPT plans/reviews; Codex executes | Planner plans/reviews; arbitrary execution agent executes | Keep, generalize |
| Control plane carries tiny C2C messages | Transport-independent C2C text contract | Keep |
| MCP carries source/diff/search/evidence | Stateless MCP 2026-07-28 data plane | Keep, modernize transport |
| 9 read-only tools | Same initial capability set | Keep behavior; re-specify contracts |
| One bridge = one workspace | Same | Keep |
| Canonical realpath + sensitive-file deny | OS-specific canonical resolver + shared policy gate | Keep, strengthen platform correctness |
| `.c2cignore` | Additive deny-only V1 | Keep with stricter semantics |
| Git diff excludes sensitive paths | Filter changed path set before diff bodies | Keep intent, strengthen implementation |
| Execution summary/test/output evidence | Same | Keep |
| Output sanitizer | Same plus explicit artifact classification | Keep |
| OAuth 2.1/PKCE | Current MCP OAuth requirements | Keep |
| DCR-first legacy behavior | CIMD-first; DCR optional compatibility | Change due to MCP 2026-07-28 |
| MCP stateful/session assumptions from older spec | Stateless core, no transport session dependence | Change due to MCP 2026-07-28 |
| Cloudflare Quick/Named tunnel abstraction | `ITunnelProvider`, Quick first | Keep/generalize |
| Node CLI/daemon | self-contained .NET CLI/runtime | Change implementation |
| Codex skill is primary UX | generic integration packs; Antigravity first | Change/generalize |
| local checkpoint + HANDOFF | same concept | Keep |
| raw source never put into control message | same | Keep |

## Required source-of-truth process

Before marking any feature compatible, inspect original source + tests, not README only. Record:

- exact original file(s);
- request/response schema;
- limits/pagination;
- error behavior;
- security scope;
- persistence semantics;
- matching .NET test case.
