# 05 — Workspace Security

## Threat model

Workspace content is untrusted. An attacker may place malicious path names, symlinks, Git changes, huge files, prompt injections, credential files or crafted logs inside the repository. The bridge must not gain capabilities because a README/comment tells it to.

## Single security gate

Every path-returning or path-reading operation must go through a shared policy pipeline. Never implement separate ad-hoc checks in each MCP tool.

```text
Raw relative input
  -> syntax validation
  -> reject NUL / URI / device-path tricks
  -> combine with workspace root
  -> canonicalize deepest existing ancestor
  -> resolve symlinks/junctions
  -> reconstruct non-existing suffix safely when applicable
  -> canonical containment check
  -> sensitive-pattern policy
  -> .c2cignore policy
  -> operation-specific bounds
  -> allow
```

## Canonicalization

Use OS-appropriate final-path primitives rather than `Path.GetFullPath()` alone:

- Unix/macOS: resolve real paths/symlinks for existing ancestors.
- Windows: resolve final paths/junctions/reparse points through handle/final-path semantics where feasible.

Containment must compare canonical root and canonical candidate on directory boundaries, not simple prefix strings (`/repo2` must not match `/repo`).

Avoid hard-coding “macOS is case-insensitive”: volumes can differ. Prefer canonical actual paths and explicit path-segment containment. Add platform test fixtures for case behavior.

## Sensitive deny defaults

Initial deny examples:

```text
.env
.env.*
*.pem
*.p12
*.pfx
*.key
id_rsa*
id_ed25519*
.ssh/**
.aws/**
.azure/**
.gcp/**
credentials*
service-account*.json
*keychain*
```

Explicit safe exceptions may include `.env.example`, `.env.sample`, and documentation fixtures that contain no real secret. Exceptions are exact and reviewed; never use a broad allow rule that defeats the deny set.

## `.c2cignore`

- additive deny only in V1;
- cannot re-allow built-in sensitive patterns;
- parsed with bounded file size and bounded rule count;
- invalid rules fail closed with a diagnostic surfaced to `doctor`.

## Listing and search leakage

Denied names should not appear in remote directory listings or search results. The filter applies before response serialization.

## Git safety

For diff:

1. obtain changed path names without diff bodies;
2. canonicalize/filter every path;
3. request diff only for the allowed path set;
4. cap bytes/lines;
5. run output sanitizer as defense-in-depth.

Never trust Git path names as safe text. Escape/control-character handling is required.

## Execution artifact safety

Execution output sanitizer should:

- reject private key blocks entirely;
- redact bearer tokens/API-key-like patterns conservatively;
- redact home directory / local user path prefixes;
- cap bytes and lines;
- normalize invalid UTF-8 safely;
- never write sanitized output over the original file;
- return `restricted` when safe sanitization is uncertain.

## Local admin security

- loopback only;
- random admin token stored in user-only app state;
- reject requests carrying reverse-proxy origin headers;
- return 404 rather than detailed auth information to unauthenticated probes where practical;
- redact sensitive values in logs.

## Security test categories

Required adversarial tests include traversal, absolute path, UNC/device paths, junction/symlink files and directories, mixed separators, case variants, null bytes, renamed `.env`, ignored paths, Git diff with secret path, huge file/diff, malicious filenames, binary content, log token/private-key blocks and authorization cross-workspace attempts.
