# Test Matrix

| Area | Unit | Integration | Adversarial | External smoke |
|---|---:|---:|---:|---:|
| Workspace path/security | Required | Required | Required | No |
| MCP transport/tools | Required adapters | Required | Required security headers | No |
| Git readers | Required parser | Required temp repo | Required secret paths | No |
| Execution evidence | Required | Required store/MCP | Required sanitizer | No |
| Tunnel | Required state machine | Fake provider | Ownership/PID cases | Optional Cloudflare |
| Auth/pairing | Required | Required HTTP | Required replay/issuer/PKCE | Optional real client |
| C2C protocol | Required exhaustive | Persistence/restart | Injection/bounds | No |
| CLI | Required mapping | Process invocation | Secret output checks | No |
| Agent integration | Review checklist | E2E | Restart/blocked | Manual/CI where possible |
| Packaging | Helpers | Artifact smoke | Secret scan | Target OS |
