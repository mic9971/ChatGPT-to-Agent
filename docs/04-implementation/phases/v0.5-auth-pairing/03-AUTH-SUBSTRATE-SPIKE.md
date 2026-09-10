# V0.5A — Auth Substrate and Interoperability Spike

## Why this exists

V0.5 is the first phase where C2C.NET becomes a remotely protected OAuth resource and authorization server. A wrong library/profile decision here creates security debt that later pairing/control-plane work cannot compensate for.

The spike is implementation research with tests/prototype code only. It must not expose a production public auth endpoint until its decision report is accepted.

## Questions to prove

### A. OAuth server substrate

Evaluate the current stable OpenIddict server + validation stack first.

Prove:

- authorization-code flow works with PKCE S256;
- authorization response can satisfy current MCP issuer (`iss`) hardening;
- access token can be validated in the same Host;
- canonical resource/audience can be enforced;
- refresh flow can be enabled only when requested/allowed;
- authorization code is one-time;
- no raw token is written to logs.

### B. Filesystem-only persistence

Repository architecture says no database in V1. Determine the smallest safe OpenIddict integration that preserves this.

If degraded mode/custom event handlers are required, enumerate every validation no longer provided automatically, including client id, redirect URI, grant, token/revocation and scope validation.

Do not proceed if the custom surface becomes equivalent to hand-writing an OAuth server. Instead stop and propose an ADR with alternatives.

### C. Client registration profile

Test, with a deterministic fake MCP OAuth client and later a manual ChatGPT smoke, which profile is viable:

1. CIMD — preferred current MCP direction;
2. pre-registration — controlled fallback;
3. DCR — compatibility only, feature-gated and ADR-backed.

For CIMD, build the resolver as an isolated security component and test SSRF/redirect/size/timeout rules before advertising support.

### D. ChatGPT interoperability

When V0.4 tunnel is available, perform a manual safe smoke against a temporary auth profile and record only non-secret protocol facts:

- discovery URLs requested;
- whether CIMD/DCR/pre-registration is attempted;
- redirect URI shape (redact unique identifiers if needed);
- requested scopes;
- whether `resource` is sent;
- refresh/offline_access behavior;
- issuer behavior.

Never capture bearer/refresh/authorization-code values in the report.

## Spike output

Create a short decision report under the phase implementation evidence (or PR description) containing:

```text
AUTH_LIBRARY:
AUTH_LIBRARY_VERSION:
TARGET_FRAMEWORK:
PERSISTENCE_MODE:
LIBRARY_VALIDATIONS_RETAINED:
CUSTOM_VALIDATIONS_REQUIRED:
CLIENT_REGISTRATION_PROFILE:
CIMD_SUPPORTED: yes|no
DCR_REQUIRED: yes|no
RESOURCE_BINDING_STRATEGY:
ISSUER_STRATEGY:
REFRESH_STRATEGY:
CHATGPT_SMOKE_EVIDENCE:
SECURITY_GAPS:
RECOMMENDATION: PROCEED | ADR_REQUIRED | BLOCKED
```

## Stop conditions

Stop before production implementation when:

- library version/profile is unknown;
- redirect/client validation would be bypassed;
- token revocation requires silently violating the V1 no-database rule;
- CIMD implementation would permit arbitrary SSRF;
- DCR is being added without evidence;
- `iss` or resource binding cannot be enforced;
- refresh rotation semantics cannot be proven.
