# Error / Result Conventions

## Expected outcomes vs exceptional failures

Use stable result/error contracts for expected business/protocol outcomes. Reserve exceptions for programmer errors, broken invariants, or infrastructure failures that cannot be represented as a normal operation result.

## Capability-owned error codes

Prefer capability-specific codes when the error is owned by one module:

```text
WORKSPACE_PATH_OUTSIDE_ROOT
WORKSPACE_PATH_DENIED
EXECUTION_NOT_FOUND
C2C_PROTOCOL_CONFLICT
```

Keep truly universal primitives in Common, but do not put every capability's codes into one ever-growing `CommonErrorCodes` file when ownership is clear elsewhere.

## Boundary mapping

Adapters translate internal results to MCP/HTTP/CLI representations. Do not let transport status codes become Core business semantics.

## Security

Remote errors must not disclose absolute local paths, tokens, stack traces, secret filenames/content, command output, or provider credentials.

## Naming

Use semantic result types (`ReadFileResult`, `WorkspaceBindingResult`) when operation-specific data matters. Use generic `OperationResult<T>` only where its semantics are stable and already approved.

Do not create exception classes merely to encode expected control flow.