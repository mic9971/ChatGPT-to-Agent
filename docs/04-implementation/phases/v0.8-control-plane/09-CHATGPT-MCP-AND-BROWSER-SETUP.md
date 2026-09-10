# V0.8B — ChatGPT MCP and Browser Setup Flow

## Purpose

Define the one-time and recurring setup needed for ChatGPT to act as planner/reviewer while Antigravity acts as executor.

## Two independent connections

Do not merge these concepts:

```text
A. ChatGPT -> C2C.NET MCP
   remote HTTPS + OAuth/pairing

B. Antigravity -> ChatGPT Web
   browser profile + user-owned ChatGPT login
```

A successful ChatGPT browser login does not prove MCP authorization. A valid MCP OAuth token does not authenticate the browser account.

## ChatGPT MCP app setup

Use the current ChatGPT custom app/MCP flow supported by the user's eligible plan/workspace. At implementation time, verify current OpenAI documentation before automating UI steps because product labels and availability may change.

Conceptual flow:

```text
c2c ensure --json
  -> public MCP endpoint available
  -> authorization metadata reachable
  -> pairing/auth readiness known

ChatGPT settings / custom app setup
  -> provide remote MCP endpoint
  -> scan/discover tools
  -> complete OAuth/pairing when prompted
  -> confirm expected read-only tool set
```

Expected V1 MCP tools remain read-only:

```text
workspace_info
read_file
list_directory
search_workspace
git_status
git_diff
execution_summary
test_status
execution_output
```

If tool discovery differs unexpectedly, stop and diagnose instead of accepting an expanded permission surface.

## ChatGPT plan/workspace availability note

Current OpenAI product capabilities can differ by ChatGPT plan and workspace policy. V0.8 setup documentation must therefore distinguish:

- C2C.NET server correctness;
- user's ChatGPT plan/workspace eligibility;
- workspace admin/developer-mode/custom-app policy;
- OAuth/pairing status.

Do not report a server implementation failure when the blocker is an unavailable ChatGPT product feature.

## Refresh/offline connectivity

If the ChatGPT MCP app requires long-lived connectivity, reuse the V0.5 OAuth refresh-token behavior and current discovery metadata. V0.8 does not invent a second token mechanism.

## Browser setup

Antigravity's Browser Agent uses its own isolated Chrome profile. Setup may require:

1. browser tools enabled;
2. ChatGPT domain permitted by Antigravity browser policy/allowlist;
3. user logs into ChatGPT inside that isolated profile;
4. user completes any password/passkey/MFA/CAPTCHA/consent step;
5. agent verifies ChatGPT UI is available;
6. agent opens or creates the task-bound conversation.

C2C.NET never reads cookies from that profile.

## One-time bootstrap state

After successful setup, the reusable state is intentionally split:

```text
C2C.NET app-state
  -> workspace binding
  -> runtime/tunnel state
  -> OAuth authorization state
  -> C2C task/session state
  -> non-secret conversation reference

Antigravity browser profile
  -> browser-owned ChatGPT login/session cookies
```

Neither component should copy the other's credentials.

## Per-task start flow

```text
1. c2c ensure --json
2. if not configured -> c2c setup
3. if pairing/auth action required -> approved auth flow
4. c2c session new/status
5. read control conversation binding
6. Browser Agent opens bound ChatGPT conversation or creates a new one
7. send INIT/HANDOFF as dictated by session state
```

## Connector/app missing in a conversation

If ChatGPT cannot access the C2C MCP app when review is needed:

- do not paste source/diff/log as a workaround;
- surface a setup/auth action required state;
- let the user reconnect/enable the app according to current ChatGPT UI;
- then retry from the same C2C checkpoint.

## Security checks

Before accepting setup complete:

```text
[ ] MCP endpoint is HTTPS remotely
[ ] bridge remains loopback-only locally
[ ] expected OAuth issuer/resource/workspace binding is intact
[ ] only expected read-only MCP tools are visible
[ ] no browser cookie/token is in C2C app-state
[ ] browser login challenge is user-owned
[ ] conversation reference is non-secret
[ ] c2c ensure reports deterministic machine-readable status
```

## Product-documentation references used at design time

OpenAI:

- https://help.openai.com/en/articles/11487775-apps-in-chatgpt
- https://help.openai.com/en/articles/12584461-developer-mode-and-mcp-apps-in-chatgpt

Antigravity:

- https://www.antigravity.google/docs/browser
- https://www.antigravity.google/docs/mcp

These URLs are research references, not immutable API contracts. Reverify them before implementing UI automation.
