# V0.8 Phase Architecture and Boundaries

## Three-channel model

V0.8 must preserve the existing separation:

```text
DATA PLANE
ChatGPT
  -> remote MCP over HTTPS/OAuth
  -> C2C.Host
  -> read-only Workspace/Git/Execution evidence

CONTROL PLANE
Execution Agent
  -> browser/computer-use/manual relay
  -> ChatGPT Web
  -> bounded C2C text

LOCAL RUNTIME CONTROL
Execution Agent
  -> c2c CLI
  -> C2C runtime/session/evidence services
```

These channels have different trust and authentication models and must never share credentials implicitly.

## Ownership model

### C2C.Core

Owns provider-neutral control coordination concepts only when they are true application contracts:

```text
C2C.Core/
  ControlPlane/
    Coordination/
    Conversation/
    Delivery/
```

Allowed responsibilities:

- task/workspace-bound conversation metadata model;
- delivery state model;
- provider-neutral coordination contract;
- transport result/failure categories;
- validation that delegates C2C protocol semantics to the existing V0.7 session/protocol service.

Not allowed:

- Antigravity types;
- ChatGPT DOM selectors;
- Chrome automation;
- OAuth implementation;
- browser cookie/session access.

### C2C.Infrastructure

Owns local persistence for transport metadata if V0.7 session persistence cannot safely host it:

```text
C2C.Infrastructure/
  ControlPlane/
    Persistence/
```

Infrastructure may implement atomic JSON binding/journal storage. It must not implement Antigravity IDE automation.

### C2C.Cli

Owns the manual/agent-facing adapter needed to bind conversation references and inspect delivery state if the implemented V0.7 CLI does not already expose enough primitives.

Possible capability layout:

```text
C2C.Cli/
  ControlPlane/
    BindCommand.cs
    StatusCommand.cs
    ClearBindingCommand.cs
```

Do not duplicate `session new`, `session accept`, `session executed`, `session handoff` if V0.7 already exposes them.

### integrations/antigravity

Owns the production Antigravity IDE workflow:

```text
integrations/antigravity/
  AGENT.md
  C2C-WORKFLOW.md
  setup.md
  mcp_config.template.json   # only if needed/valid for the chosen integration path
```

The Antigravity agent itself uses its Browser Agent/browser capability. C2C.NET does not call the IDE browser through .NET.

## V0.8A boundary

V0.8A implements the reusable coordination substrate for `UC-CTRL-01..08` and proves it with deterministic fake/manual transports.

Recommended shape:

```text
                  V0.7 C2C Session Service
                           ^
                           |
                 ControlPlaneCoordinator
                  /        |        \
                 /         |         \
       Binding Store   Delivery     Transport seam
                         Journal       (provider-neutral)
                                         |
                                  fake/manual in tests
```

The coordinator does not execute code and does not read workspace files directly. It asks V0.7 session services what message/next action is valid.

## V0.8B boundary

V0.8B teaches the first execution agent, Antigravity, how to realize the transport externally:

```text
Antigravity
  1. c2c ensure --json
  2. c2c session status/new
  3. Browser Agent opens/attaches ChatGPT
  4. send exact rendered INIT/EXECUTED/HANDOFF text
  5. wait for completed assistant response
  6. feed bounded response to c2c session accept
  7. execute accepted PLAN locally
  8. c2c record
  9. c2c session executed
 10. repeat until DONE/BLOCKED/ERROR
```

The browser workflow is an integration procedure, not a domain service.

## Why no `AntigravityBrowserControlDriver` in .NET V0.8

Antigravity IDE already provides agent/browser actuation as part of its agent environment. The current product design does not require a public .NET API that controls the IDE. Creating a concrete .NET `AntigravityBrowserControlDriver` would couple the bridge to an implementation detail and duplicate capabilities the execution agent already owns.

A future external Antigravity SDK adapter is possible, but it belongs outside `C2C.Core` and is not required for the IDE V0.8 path.

## Manual fallback is first-class

The system must still work when browser automation is unavailable:

```text
c2c session ... -> rendered bounded message
user/operator    -> paste into ChatGPT
ChatGPT          -> bounded C2C response
user/operator    -> feed response to c2c session accept
```

Manual fallback uses the same protocol parser, validation and session state as automation.

## Project-structure rule

Do not create a new assembly only because the feature is called ControlPlane. Add a new project only if the implemented dependencies prove an assembly boundary is required. Prefer capability folders inside the existing Core/Infrastructure/CLI projects for V0.8A.
