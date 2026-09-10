# Project Structure

## Principle

Project/assembly boundaries express architecture; folders inside a project express business cohesion.

For C2C.NET the approved project-level direction stays authoritative. Do not create one assembly per use case or one project per technical noun.

## Default shape

```text
C2C.Core            framework-independent contracts/policies/domain/application logic
C2C.Infrastructure  filesystem, Git, process, persistence, tunnel/provider implementations
C2C.Security        authorization/pairing/token implementation
C2C.Host            ASP.NET Core/MCP/HTTP adapters and composition root
C2C.Cli             local command adapter/lifecycle orchestration
```

Within a project prefer capability folders:

```text
C2C.Core/
├── Workspace/
├── Execution/
├── C2C/
└── Common/
```

not global technical buckets such as:

```text
Models/
Services/
Interfaces/
Handlers/
```

## When a new project is justified

Create a new project only when at least one material boundary exists: dependency direction, security isolation, packaging/deployment, platform-specific implementation, or testability that cannot be achieved cleanly inside the existing assembly.

Do not create a project merely because a folder contains many files.

## Adapter exception

A transport/adaptor project may group by transport first, then capability:

```text
C2C.Host/
└── Mcp/
    ├── Workspace/
    └── Execution/
```

The adapter stays thin and delegates behavior to Core/application services.