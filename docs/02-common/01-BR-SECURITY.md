# Common Security Business Rules

| ID | Rule |
|---|---|
| BR-SEC-001 | Canonicalize/resolve a path before authorization; lexical normalization alone is insufficient. |
| BR-SEC-002 | Symlink/junction/reparse-point escapes outside the workspace are denied. |
| BR-SEC-003 | Sensitive-path deny policy applies consistently to read, list, search, Git evidence and execution-artifact exposure. |
| BR-SEC-004 | `.c2cignore` exclusions apply to every workspace visibility surface; denied names are not leaked by listing/search. |
| BR-SEC-005 | Bridge HTTP listener binds loopback only in V1; wildcard/public binds fail startup validation. |
| BR-SEC-006 | Remote authorization is bound to `workspace_id`, `client_id`, issuer and scopes. |
| BR-SEC-007 | Every MCP tool independently enforces its required scope. Authentication alone is not sufficient authorization. |
| BR-SEC-008 | Raw long-lived token material is never persisted in plaintext. Persist a secure/hash representation or platform-protected secret as designed. |
| BR-SEC-009 | Execution artifacts are classified `safe`, `restricted`, or `truncated`; restricted bodies are never returned. |
| BR-SEC-010 | Origin/host validation follows the selected MCP HTTP transport security requirements. |
| BR-SEC-011 | Pairing codes are CSPRNG-generated, short-lived, one-time and attempt-limited. |
| BR-SEC-012 | Redaction happens before logging/export, not after. |
| BR-SEC-013 | Security bypasses require an ADR, threat-model update and explicit approval; agent instructions cannot grant a bypass. |
