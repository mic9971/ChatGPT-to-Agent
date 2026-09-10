# 19 — Research Notes (2026-09-10 baseline)

## Key findings that changed the design

### MCP changed materially in 2026
The 2026-07-28 MCP release moved the core to a stateless request/response model, removed dependence on initialize/session identifiers, introduced routing headers/cacheable list results, and hardened authorization. New C2C.NET code should target that model rather than copying an older stateful implementation.

### DCR is no longer the preferred new registration approach
The current MCP direction formally deprecates Dynamic Client Registration in favor of Client ID Metadata Documents (CIMD), while retaining DCR for backwards compatibility. Therefore C2C.NET should be CIMD-first when interoperable and implement DCR only when an actual client requires it.

### Official C# MCP SDK is suitable
The official C# SDK includes `ModelContextProtocol.AspNetCore` for HTTP MCP servers and is a Tier-1/current-spec SDK. This removes the need to implement MCP JSON-RPC/transport manually.

### .NET baseline
.NET 10 is the latest stable target framework in the current Microsoft documentation and pairs with C# 14. New solution should target `net10.0` unless a concrete deployment constraint requires .NET 8 LTS compatibility.

### OpenIddict fit
OpenIddict supports ASP.NET Core 10, authorization code/refresh flows and PKCE. First-class DCR is still tracked separately, which is less problematic now that MCP itself is moving away from DCR. Use OpenIddict selectively rather than expecting it to supply every MCP-specific endpoint/metadata behavior automatically.

### .NET engineering practices researched
Microsoft guidance reinforces:

- DI: small services, no service-locator pattern, avoid `BuildServiceProvider` during registration, make singleton shared state explicitly thread-safe.
- ASP.NET Core: avoid blocking calls/sync-over-async; bound large responses; keep hot paths fast.
- Options: strongly-typed options and validation.
- HTTP: `IHttpClientFactory`/resilience handlers for outbound network dependencies; retries must respect idempotency.
- Testing: xUnit is supported; `WebApplicationFactory` is the standard ASP.NET Core integration-test harness.
- Code analysis: built-in analyzers and warnings-as-errors policies are appropriate for a new codebase.
- NuGet: Central Package Management reduces multi-project dependency drift.

## Primary references used

- Original project: `XiaoDuoYa/codex-with-chatgpt` README, architecture, protocol and security documents, inspected 2026-09-10.
- Model Context Protocol: 2026-07-28 specification release and authorization guidance.
- Official MCP C# SDK: `modelcontextprotocol/csharp-sdk`.
- Microsoft Learn: .NET/C# coding conventions, DI guidelines, Options pattern, ASP.NET Core best practices, integration testing, code analysis, HttpClient/resilience and Central Package Management.
- OpenIddict documentation: PKCE and ASP.NET Core/server capabilities; DCR support status checked separately.

## Implementation-time verification rule

The above is a design baseline, not permission to assume packages never change. Before the Auth/MCP implementation slices, the agent must verify the exact installed SDK/package version and run protocol/interoperability tests against the target ChatGPT MCP client.
