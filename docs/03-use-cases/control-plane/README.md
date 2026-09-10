# Control Plane Automation Use Cases

This module owns transport automation between the execution agent and ChatGPT Web. It does not own MCP data access, OAuth to the MCP server, local code execution, or C2C state semantics.

Implementation order:

```text
UC-CTRL-01 Start Control Session
 -> UC-CTRL-02 Open/Attach Conversation
 -> UC-CTRL-03 Send C2C Message
 -> UC-CTRL-04 Receive/Validate Response
 -> UC-CTRL-05 Run C2C Iteration Loop
 -> UC-CTRL-06 Resume Existing Conversation
 -> UC-CTRL-07 Handoff New Conversation
 -> UC-CTRL-08 Recover Driver Failure
```

All use cases reference `docs/02-common/09-BR-CONTROL-PLANE.md`. V0.8 starts only after the C2C protocol/checkpoint phase is accepted.
