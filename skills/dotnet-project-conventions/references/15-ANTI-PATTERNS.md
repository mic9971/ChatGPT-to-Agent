# Structural Anti-Patterns

These are review alarms, not syntax bans. If one appears, the agent must justify it against the target UC and local convention.

## Dumping-ground folders

```text
Models/
Services/
Interfaces/
Repositories/
Handlers/
Managers/
Helpers/
Utils/
Misc/
Common/Everything...
```

They are problematic when they mix unrelated capabilities and force readers to search the whole project to understand one use case.

## Vague class names

```text
CommonService
DataManager
FileHelper
GeneralProcessor
BaseHandler
Utility
```

Prefer names with semantic responsibility.

## Premature sharing

- moving two same-shaped DTOs into Common;
- introducing `BaseService` only to remove duplicate logging/validation lines;
- creating a generic repository before more than one capability proves the abstraction;
- creating `SharedKernel` without an approved architecture need.

## Layer leakage

- MCP SDK types in Core;
- provider DTOs used as domain models;
- persistence records returned directly through remote APIs;
- browser automation selectors in protocol/domain code.

## Project explosion

Do not create projects such as `Application.Contracts`, `Application.Common`, `Infrastructure.Common`, `Shared`, etc. simply because a template uses them. Every assembly boundary must pay for itself through dependency/security/package/deployment value.

## Mechanical refactoring

Do not rename/move hundreds of existing files to match this skill while implementing a feature. Convention adoption is incremental unless an explicit migration task is approved.