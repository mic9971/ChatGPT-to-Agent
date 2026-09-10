# Dependency Rules

## Direction over convenience

Placement must preserve approved dependency direction. For C2C.NET:

```text
Core <- Infrastructure
Core <- Security (where applicable)
Core/Infrastructure/Security <- Host composition/adapters
Core/application services <- CLI adapter
```

Exact project references in the repository are authoritative; this diagram is conceptual.

## Core must not know adapters

Core must not depend on:

- ASP.NET Core endpoint/request types;
- MCP SDK transport objects;
- Git executable/process implementation;
- Cloudflare SDK/process format;
- Antigravity/Codex/browser automation;
- concrete JSON/filesystem persistence details unless the persisted shape itself is a product contract.

## Capability coupling

Prefer one capability consuming another through an explicit public contract/policy rather than reaching into implementation internals.

Do not create circular capability dependencies. If two capabilities mutually depend on implementation details, re-check ownership or extract a small stable contract owned by the correct upstream capability.

## Package dependencies

A NuGet package does not justify a new architectural layer. Add packages only in the project that needs them and do not leak their types into Core contracts unless the package is itself part of the approved public protocol abstraction.