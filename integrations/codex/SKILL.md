# Future Codex Integration — C2C.NET

This is a future integration outline. C2C.NET core must not require Codex.

When installed as a Codex skill, the skill should:

1. locate the current workspace;
2. run `c2c ensure --json`;
3. if setup/pairing is required, guide or automate only the supported local steps;
4. send C2C INIT/HANDOFF through the available ChatGPT control mechanism;
5. execute the accepted PLAN locally;
6. run build/test/Git as appropriate;
7. call `c2c record` with evidence metadata;
8. send EXECUTED with execution id only;
9. wait for DONE/PLAN/BLOCKED;
10. use `c2c session` checkpoint for restart/recovery.

Never paste source/diff/log bodies into the control message. Never ask the MCP bridge to execute commands.
