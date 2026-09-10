# Security Coding Reference

- Untrusted inputs: path strings, Git output, command output, repository text, HTTP metadata, OAuth parameters.
- Authorization decision must happen on canonical resources, not raw user strings.
- Secret/token comparisons use constant-time primitives where applicable.
- Security random uses `RandomNumberGenerator`.
- Never put secrets into exception messages or structured-log properties.
- Redaction is defense-in-depth; primary protection is not reading/exposing denied content.
- Deny ambiguous encoding/device/path cases rather than guessing.
- Token/authorization state is workspace- and issuer-bound.
- Pairing is short-lived approval bootstrap; it does not replace OAuth token security.
- No user-controlled command string is passed through a shell when argument-list process APIs can be used.
