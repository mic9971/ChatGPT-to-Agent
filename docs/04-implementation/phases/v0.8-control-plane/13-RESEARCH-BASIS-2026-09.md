# V0.8 Research Basis — September 2026

## Purpose

Record the external product capabilities that informed the V0.8 design so implementation agents do not assume an unsupported browser-control API or stale ChatGPT setup flow.

This file is evidence/reference only. Repository BRs and approved UCs remain normative.

## Google Antigravity findings

Current Google documentation describes Antigravity IDE as an agentic development environment whose agent can operate across editor, terminal and browser. The Browser Agent can read and actuate a Chrome browser.

Relevant references:

- https://www.antigravity.google/docs/ide/overview/
- https://www.antigravity.google/docs/browser
- https://www.antigravity.google/docs/agent

### Design implication

The V0.8 IDE path should let the **Antigravity agent own browser actions**. C2C.NET should expose deterministic CLI/session/control contracts and must not invent a .NET remote-control API for the IDE.

## Antigravity MCP findings

Current Antigravity docs support MCP configuration in IDE/CLI/SDK, including custom MCP servers.

Reference:

- https://www.antigravity.google/docs/mcp

### Design implication

Antigravity MCP support is optional for the executor and must not be confused with the required planner data plane:

```text
ChatGPT -> C2C.NET MCP   # planner evidence path
Antigravity -> MCP       # optional agent tooling
```

Antigravity does not need C2C.NET MCP write capability because the agent already has local editor/terminal execution.

## Antigravity SDK findings

Google currently documents a Python `google-antigravity` SDK for building autonomous agents programmatically.

Reference:

- https://www.antigravity.google/docs/sdk/overview/

### Design implication

A future programmatic adapter is possible, but it is not evidence of a .NET IDE-browser automation API. For V0.8:

- no mandatory Python sidecar;
- no SDK dependency in `C2C.Core`;
- optional SDK adapter requires a separate ADR if later adopted.

## ChatGPT custom app / MCP findings

Current OpenAI Help Center documentation describes custom apps using MCP and plan/workspace-dependent developer/custom-app controls. ChatGPT connects to remote MCP servers; setup may require enabling the relevant app/developer capability and completing OAuth before tool scan/use.

Relevant references:

- https://help.openai.com/en/articles/11487775-apps-in-chatgpt
- https://help.openai.com/en/articles/12584461-developer-mode-and-mcp-apps-in-chatgpt

### Design implication

The repository must not hard-code current ChatGPT UI labels as protocol behavior. V0.8 setup should distinguish:

```text
C2C.NET server ready
ChatGPT product/plan/workspace feature available
custom app/MCP configured
OAuth/pairing complete
browser login complete
```

A blocker in one layer does not imply another layer is broken.

## Security findings applied

The design intentionally keeps three credential domains separate:

```text
1. ChatGPT -> C2C.NET MCP OAuth
2. Antigravity -> local C2C CLI/runtime
3. Antigravity browser profile -> ChatGPT account session
```

No domain copies another domain's credential material.

## Reverification rule

Before implementing provider UI automation, re-check the current official documentation for:

- Antigravity browser capability and safety controls;
- Antigravity skill/workflow/config format;
- ChatGPT custom app/MCP setup flow;
- current plan/workspace availability;
- OAuth connector requirements.

If the product surface has changed, update this research note and the integration docs, but do not change C2C protocol/security semantics merely to match UI churn.
