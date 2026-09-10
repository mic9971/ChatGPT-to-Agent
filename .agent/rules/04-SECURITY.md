# Security Rules

- Read `docs/02-common/01-BR-SECURITY.md` for every path/auth/tunnel/evidence UC.
- Never add MCP shell/write/delete/commit tools in V1.
- All path-bearing capabilities use the one shared canonical workspace policy.
- Sensitive/ignore policy applies before names/bodies are disclosed.
- Never broad-read secret Git diff/log then redact as the primary security control.
- Never log tokens, pairing codes, Authorization header, source bodies or raw execution output.
- Never kill a process unless current runtime ownership is verified.
- Security test failures are not “fixed” by weakening assertions.
