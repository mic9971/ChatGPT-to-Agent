# 10 — Agent Integration

## Goal

C2C.NET is execution-agent agnostic. Integration is a thin instruction/CLI/control-transport layer, not a hard dependency on a proprietary SDK.

## Three independent channels

```text
DATA PLANE
ChatGPT -> MCP/OAuth -> C2C.NET -> workspace/evidence (read-only)

CONTROL PLANE
Execution Agent -> browser/computer-use/manual driver -> ChatGPT Web -> C2C messages

LOCAL RUNTIME CONTROL
Execution Agent -> c2c CLI / loopback admin surface -> C2C.NET
```

Never reuse credentials or trust assumptions across these channels.

## Minimum executor capabilities

An executor can integrate if it can:

- operate inside a local workspace;
- run `c2c ensure --json` and parse result;
- exchange C2C text messages with the planning client (automated or operator-assisted);
- edit files and run local build/test/Git operations itself;
- call `c2c record` after an iteration;
- read/write bounded local task checkpoint through `c2c session`.

## Adapter model

V1 keeps execution-agent integration outside Core. If detection adapters are useful:

```csharp
public interface IExecutionAgentAdapter
{
    string Name { get; }
    Task<AgentCapabilities> DetectAsync(CancellationToken cancellationToken);
}
```

Control-plane transport is a different responsibility. V0.8 may introduce `IControlPlaneDriver` at the integration/application edge as defined by `20-CONTROL-PLANE-AUTOMATION.md`.

Do not let the bridge invoke arbitrary agent shell commands.

## Control transport

C2C protocol is transport-independent. Supported modes:

- Antigravity browser/computer-use automation (first automated profile);
- manual user copy/paste fallback;
- future Codex/computer-use driver;
- future first-party connector/action with explicit authorization.

The core task state does not depend on how the control message moved.

## Authentication boundary

- ChatGPT connector -> OAuth/PKCE -> MCP server.
- Antigravity/Codex browser session -> ChatGPT account login owned by the user.
- Agent -> local C2C.NET runtime uses CLI/loopback local authorization.

The agent must pause for password/passkey/CAPTCHA/MFA/2FA and must never persist those browser credentials in C2C.NET.

## Integration pack responsibilities

Each agent pack documents:

1. environment/setup detection;
2. when to call `ensure`;
3. exact INIT/EXECUTED/HANDOFF format;
4. implementation discipline;
5. how to record evidence;
6. how to open/attach/resume the planning conversation;
7. how to recover browser/control-driver failure;
8. how to fall back to manual transport;
9. prohibited actions (never bypass read-only MCP boundary; never paste logs/source into control messages; never bypass account authentication challenges).

## Implementation timing

Do not add browser/control-driver runtime code before V0.8. Earlier phases implement secure data/evidence/auth/protocol foundations only.
