# Common Authorization and Pairing Rules

| ID | Rule |
|---|---|
| BR-AUTH-001 | Authorization Code flow for public/browser clients uses PKCE S256. |
| BR-AUTH-002 | Authorization responses/tokens are validated against the expected issuer; issuer confusion is rejected. |
| BR-AUTH-003 | Client metadata/registration follows the selected current MCP-compatible profile; compatibility fallbacks are feature-gated and documented. |
| BR-AUTH-004 | Access tokens are short-lived and scope/workspace/client bound. |
| BR-AUTH-005 | Refresh tokens, when issued, rotate on use and replay is denied. |
| BR-AUTH-006 | Revocation/unpair invalidates future remote access as soon as practical and clears local pairing state. |
| BR-AUTH-007 | Pairing code is approval bootstrap only; access/refresh credentials never appear in human-visible C2C messages. |
| BR-AUTH-008 | Pairing attempts are rate/attempt limited and expire after configured TTL. |
