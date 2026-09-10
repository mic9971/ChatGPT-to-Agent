# Senior .NET Review Checklist

## Correctness
- Does behavior match acceptance criteria?
- Are null/empty/not-found/cancellation/error cases defined?
- Is state transition deterministic/idempotent?

## Security
- Is input untrusted and validated at canonical boundary?
- Could list/search/diff/log leak something read_file blocks?
- Any new shell/process string interpolation?
- Any secret/log/exception leakage?

## Architecture
- Any Core -> infrastructure dependency leak?
- Is abstraction useful or speculative?
- Is Program.cs/CLI handler accumulating business logic?

## Async/concurrency
- Cancellation propagated?
- Any `.Result`/`.Wait()`?
- Singleton mutable state thread-safe?
- Retry safe/idempotent?

## Performance
- Any unbounded list/read/diff?
- Any whole-file/whole-log materialization?
- Any blocking I/O on ASP.NET request path?

## Testing
- Happy + failure + boundary tests?
- Security regression included?
- Integration test for HTTP/adapter behavior where relevant?

## Operations
- Stable safe error code?
- Structured logs useful and redacted?
- Config validated?
- Dependency/package change justified?
