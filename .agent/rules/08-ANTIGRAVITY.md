# Antigravity Operating Rules

Antigravity is the first execution agent, not part of C2C.NET Core.

- Begin with `c2c ensure --json` once CLI exists.
- If result is NEED_SETUP/NEED_PAIRING/BLOCKED, stop and surface exact required user action.
- Use the target UC as the implementation contract.
- Execute PLAN locally with Antigravity's own edit/terminal capabilities.
- Record evidence with C2C.NET; never ask MCP to mutate workspace.
- Relay only bounded C2C messages; source/diff/log evidence is read by planner through MCP.
- On restart, query session next-action; do not blindly repeat commands from an uncertain EXECUTING checkpoint.
