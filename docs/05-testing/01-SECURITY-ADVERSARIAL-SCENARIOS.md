# Security Adversarial Scenarios

Minimum fixtures before remote exposure:

- `../` traversal and encoded/odd separator variants applicable to platform.
- Absolute path outside root.
- Symlink/junction/reparse point from inside root to outside.
- Sensitive `.env`, private-key and credentials-like files.
- `.c2cignore` file visible through list/search/Git regression attempts.
- Git diff with normal file + secret file changed in same worktree.
- Search query containing shell metacharacters and option-looking prefixes.
- Artifact containing PEM private key, bearer-like token and home paths.
- Pairing brute force / expiry / double use.
- OAuth issuer mismatch, redirect mismatch, PKCE downgrade, wrong workspace/client, refresh replay.
- Tunnel stale PID reused by unrelated process.
- C2C goal/PLAN/diff containing prompt-injection text attempting to enable write/shell or suppress tests.
- Corrupt/truncated persisted checkpoint during restart.
