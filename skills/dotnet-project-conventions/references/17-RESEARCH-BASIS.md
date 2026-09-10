# Research Basis

This skill is a synthesis, not a copy of any single repository. The sources below are inspiration; C2C.NET architecture/BR/UC documents remain authoritative.

## kgrzybek/modular-monolith-with-ddd

https://github.com/kgrzybek/modular-monolith-with-ddd

Learned principle: business/bounded-context ownership is a stronger first grouping axis than global technical folders; module internals can still preserve application/domain/infrastructure boundaries.

## ardalis/CleanArchitecture

https://github.com/ardalis/CleanArchitecture

Learned principle: dependency direction and Clean Architecture remain useful, while smaller/vertical-slice variants show that assembly/layer ceremony should match system size rather than be copied mechanically.

## jasontaylordev/CleanArchitecture

https://github.com/jasontaylordev/CleanArchitecture

Learned principle: infrastructure/UI dependencies point inward and application/domain contracts should not depend on concrete adapters.

## microsoft/agent-framework

https://github.com/microsoft/agent-framework

Learned principle: agent instructions can be decomposed into focused skills/references (project structure, build/test, etc.) instead of one giant prompt.

## dotnet/sdk and dotnet/runtime

https://github.com/dotnet/sdk
https://github.com/dotnet/runtime

Learned principle: repository-level guidance should direct agents to the nearest architecture/convention instructions, and local repository conventions outrank generic style advice.

## microsoft/mcp

https://github.com/microsoft/mcp

Learned principle: deterministic naming/placement guidance is more useful to coding agents than vague “clean code” advice.

## managedcode/dotnet-skills

https://github.com/managedcode/dotnet-skills

Learned principle: keep the skill entry file action-oriented and move deep reference material into a references folder that agents read only when relevant.

## Adaptation rule

Never claim these repositories define one universal .NET structure. C2C.NET deliberately combines:

```text
Clean boundaries
+ capability-first folders
+ vertical use-case cohesion
+ strict model/shared-code taxonomy
+ agent decision rules
```

The target repository's approved architecture and local conventions always win.