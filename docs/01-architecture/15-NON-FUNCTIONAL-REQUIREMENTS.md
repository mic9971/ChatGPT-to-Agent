# 18 — Non-Functional Requirements

## Security

- Fail closed on ambiguous path/auth state.
- No remote write/exec capability in V1.
- Secrets not logged or returned.
- Workspace tokens cannot authorize another workspace.
- Security-sensitive policy code has exhaustive adversarial tests.

## Performance

- Idle bridge should remain lightweight; target <150 MB RSS on typical self-contained runtime after stabilization, with measurement before optimization.
- Common `workspace_info`/small `read_file` response target <100 ms local p95 excluding client/network.
- Search/diff are bounded and cancellable.
- Avoid sync-over-async and request-thread blocking.

## Reliability

- `start/ensure/stop/record` safe under retries.
- Atomic state writes.
- Process/tunnel ownership tracking.
- Cancellation and timeout for every external process/network operation.

## Portability

- macOS arm64 first-class.
- Windows x64 and Linux x64 supported for V1; osx-x64/linux-arm64 packaging targeted before 1.0 if CI allows.
- Filesystem behavior abstracted/tested per platform.

## Maintainability

- Small DI graph; no service locator.
- Stable public/machine schemas versioned.
- Central Package Management.
- nullable reference types and analyzers enabled.
- dependency additions require a reason and license/security review.

## Observability

- structured logs with event ids and redaction;
- correlation/task/execution ids where safe;
- optional OpenTelemetry metrics/traces later, disabled or local-only by default unless user opts in;
- no source bodies/diffs in telemetry.

## Usability

- human CLI gives one actionable next step;
- `doctor` explains root cause using safe diagnostics;
- agent JSON contract avoids prose parsing.
