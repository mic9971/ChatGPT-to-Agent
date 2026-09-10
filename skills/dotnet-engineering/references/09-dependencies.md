# Dependency Management Reference

- Use `Directory.Packages.props` Central Package Management.
- Pin SDK via `global.json` with a documented roll-forward policy.
- Prefer framework/BCL capabilities before new dependencies.
- New package requires: purpose, license check, maintenance activity, security posture, transitive size, and why BCL/framework is insufficient.
- Keep auth/crypto dependencies mature and standards-focused.
- Use official MCP C# SDK rather than a bespoke MCP implementation.
- Do not add Redis, DB, message bus, MediatR or Polly-style layers unless a real requirement exists. For HTTP resilience use current `Microsoft.Extensions.Http.Resilience` when needed.
- Avoid package version attributes scattered across csproj files once CPM is enabled.
