# 10 — Agent Integration

## Goal

C2C.NET is execution-agent agnostic. Integration should be a thin instruction/CLI layer, not a hard dependency on a proprietary SDK.

## Minimum executor capabilities

An executor can integrate if it can:

- operate inside a local workspace;
- run `c2c ensure --json` and parse result;
- exchange C2C text messages with the planning client (automated or operator-assisted);
- edit files and run local build/test/Git operations itself;
- call `c2c record` after an iteration;
- read/write bounded local task checkpoint through `c2c session`.

## Adapter model

V1 uses instruction packs and CLI contracts. If programmatic adapters are later needed, define them outside Core:

```csharp
public interface IExecutionAgentAdapter
{
    string Name { get; }
    Task<AgentCapabilities> DetectAsync(CancellationToken cancellationToken);
}
```

Do not let the bridge invoke arbitrary agent shell commands.

## Control transport

C2C protocol is transport-independent. Supported modes can be:

- browser/computer-use automation;
- manual user copy/paste;
- future first-party connector/action with explicit authorization.

The core task state does not depend on how the control message moved.

## Integration pack responsibilities

Each agent pack documents:

1. environment/setup detection;
2. when to call `ensure`;
3. exact INIT/EXECUTED/HANDOFF format;
4. implementation discipline;
5. how to record evidence;
6. how to resume after process restart;
7. what actions are prohibited (never bypass read-only MCP boundary; never paste logs/source into control message).
