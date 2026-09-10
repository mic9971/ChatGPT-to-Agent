# Authorization & Pairing — Use Cases

| Use case | Title | Priority | Phase | Depends on |
|---|---|---:|---|---|
| `UC-AUTH-01` | Create Pairing Session | P1 | V0.5 | UC-WS-01 |
| `UC-AUTH-02` | Approve Pairing and Authorize Client | P1 | V0.5 | UC-AUTH-01 |
| `UC-AUTH-03` | Issue and Validate Access Token | P1 | V0.5 | UC-AUTH-02 |
| `UC-AUTH-04` | Rotate Refresh Token | P1 | V0.5 | UC-AUTH-03 |
| `UC-AUTH-05` | Revoke / Unpair Client | P1 | V0.5 | UC-AUTH-03 |

Implementation should normally follow dependency order, not simply numeric order if a dependency says otherwise.
