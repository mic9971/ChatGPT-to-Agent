# V0.8B — Antigravity Integration Design

## Goal

Make Antigravity the first execution-agent integration that can operate the full C2C loop while keeping C2C.NET provider-neutral.

## Current Antigravity capabilities relevant to this design

Google's current Antigravity documentation states that the IDE agent can operate across the editor, terminal and browser, and that its Browser Agent can read and actuate Chrome. Antigravity also supports MCP configuration and agent skills/workflows. A separate Antigravity Python SDK exists for programmatic autonomous agents, but it is not required for the V0.8 IDE integration path.

References used when this design was authored:

- https://www.antigravity.google/docs/ide/overview/
- https://www.antigravity.google/docs/browser
- https://www.antigravity.google/docs/mcp
- https://www.antigravity.google/docs/sdk/overview/

## Primary V0.8B integration mode: IDE workflow

Do not implement a .NET class that attempts to remote-control Antigravity IDE.

The integration pack teaches the Antigravity agent to perform the transport itself:

```text
Antigravity Agent
  |
  +-- terminal --> c2c ensure/session/record commands
  |
  +-- browser --> ChatGPT conversation
  |
  +-- editor ----> local source modification
```

## Integration pack target

```text
integrations/antigravity/
  AGENT.md
  C2C-WORKFLOW.md
  setup.md
  mcp_config.template.json        # include only if validated against current Antigravity format
```

Optionally expose the same content as an Antigravity skill if the repository's skill packaging convention supports it cleanly.

## Agent workflow

### Bootstrap

```text
1. read AGENTS.md + integration instructions
2. run c2c ensure --json
3. if setup required -> stop and surface action
4. if pairing required -> run approved pair flow and involve user where required
5. load/new C2C session
6. acquire/confirm executor lease
```

### Open planner

```text
1. read conversation binding/control status
2. use Browser Agent to open ChatGPT
3. if login challenge -> pause for user
4. if existing reference -> open same conversation
5. if no reference -> create new conversation and bind non-secret reference
6. verify composer semantically available
```

### INIT / PLAN

```text
1. obtain exact rendered INIT from C2C session/coordinator
2. send exactly once through browser
3. wait for assistant response completion
4. extract only the latest bounded C2C response
5. pass response to c2c session accept
6. continue only if V0.7 returns accepted PLAN
```

### Execute PLAN

Antigravity uses editor/terminal capabilities directly. The C2C bridge does not execute the plan.

Before editing, Antigravity must still obey repository coding conventions, agent rules, target UC scope and security rules.

### Evidence / EXECUTED

```text
1. run build/test commands required by PLAN
2. record evidence using c2c record
3. obtain execution id
4. attach/render EXECUTED using C2C session command
5. send bounded EXECUTED to ChatGPT
6. ChatGPT reads diff/test/evidence independently through MCP
7. receive PLAN | DONE | BLOCKED | ERROR
```

### Repeat / terminal

- PLAN -> execute next bounded iteration;
- DONE -> verify locally accepted terminal checkpoint, then stop;
- BLOCKED -> surface required external action;
- ERROR -> run bounded recovery/doctor and stop if unresolved;
- lost conversation -> use HANDOFF, never silently continue in a fresh chat.

## Browser interaction rules

Prefer semantic/accessibility interaction over coordinate scripts:

- expected ChatGPT origin;
- semantic composer textbox/role;
- semantic send control/submit behavior;
- newest assistant-message boundary;
- response generation completion indicator when available;
- navigation state and conversation URL/reference.

Fixed screen coordinates, pixel matching or fixed sleeps may be used only as temporary smoke-test assistance, never as the durable contract.

## Browser profile and authentication

Antigravity documents an isolated browser profile. User-entered authentication can persist in that profile, but C2C.NET must never extract or copy browser cookies/session storage.

On password/passkey/MFA/CAPTCHA/consent challenge:

```text
STATE = user action required
agent pauses
user completes challenge directly
agent resumes from same C2C checkpoint
```

## Allowed agent permissions

V0.8B needs only capabilities already conceptually required by the execution agent:

```text
read/edit workspace locally
run bounded terminal commands
invoke c2c CLI
use Browser Agent for ChatGPT
```

It does not gain:

```text
MCP write tools
bridge shell endpoint
ChatGPT credential access
OAuth token export
arbitrary browser-account scraping
```

## Antigravity MCP configuration

Antigravity's own MCP support is not the ChatGPT data plane. Do not confuse:

```text
ChatGPT -> C2C.NET MCP      # required planner evidence path
Antigravity -> MCP servers  # optional agent tooling path
```

The Antigravity integration does not need C2C.NET MCP write access because it already edits locally through the IDE/terminal.

## Optional future Antigravity SDK adapter

The current Antigravity SDK is Python-based and can run autonomous agents programmatically. A future standalone runner could call C2C CLI or implement a provider-neutral transport adapter around the SDK, but:

- it stays outside `C2C.Core`;
- it is not required for V0.8B IDE acceptance;
- it must not make Python a mandatory C2C.NET runtime dependency;
- it requires a separate ADR/package/security review before production adoption.

## Acceptance criteria

- Antigravity can start from `c2c ensure --json`;
- it can open/attach the correct ChatGPT conversation;
- it sends only bounded C2C control messages;
- user-owned auth challenges pause safely;
- accepted PLAN is executed locally;
- EXECUTED references recorded evidence;
- ChatGPT reviews through MCP;
- two-iteration replan flow works;
- restart/resume works without duplicate execution/send;
- missing conversation routes through HANDOFF;
- no ChatGPT credential is persisted by C2C.NET.
