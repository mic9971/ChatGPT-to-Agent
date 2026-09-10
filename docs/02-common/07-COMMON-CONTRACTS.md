# Common Contracts and Versioning

## Core identifiers

```csharp
public readonly record struct WorkspaceId(string Value);
public readonly record struct TaskId(string Value);
public readonly record struct ExecutionId(string Value);
public readonly record struct ArtifactId(string Value);
```

Identifiers are validated at boundaries and are not accepted as arbitrary filesystem paths.

## Result shape

Application services prefer typed results for expected failures:

```csharp
public sealed record OperationError(string Code, string Message);
public sealed record OperationResult<T>(T? Value, OperationError? Error);
```

Do not force this exact type if the implementation chooses an equivalent consistent pattern; expected denied/not-found states must not require stack-trace exceptions.

## Paging

```csharp
public sealed record PageRequest(string? Cursor, int? Limit);
public sealed record PageResult<T>(IReadOnlyList<T> Items, string? NextCursor);
```

Server applies maximums even if client requests a larger limit. Stable sort order is established before page slicing.

## CLI JSON envelope

```json
{
  "schemaVersion": 1,
  "ok": true,
  "operation": "ensure",
  "data": {},
  "error": null
}
```

## Persisted record envelope

Every local record includes at minimum:

```json
{
  "schemaVersion": 1,
  "workspaceId": "...",
  "createdAt": "..."
}
```

Readers reject unsupported major versions and may migrate explicitly supported old versions. Silent destructive migration is forbidden.
