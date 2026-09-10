# V0.6D — UC-CLI-07 Record Evidence and Read Local Logs

## Goal

Expose local commands for the existing Execution Evidence subsystem and bounded C2C.NET diagnostics without creating a generic file reader or leaking sensitive content.

## `c2c record`

### Reuse

Delegate to the existing V0.3 `IExecutionRecorder` and related contracts. The CLI does not create a second execution-id/idempotency algorithm or artifact sanitizer.

### Input strategy

Prefer typed options and/or one explicitly documented structured input mode. Avoid giant command lines containing raw build/test output.

Recommended pattern:

```text
c2c record --input <C2C-owned/explicit evidence descriptor>
```

or bounded typed fields plus explicit artifact references/content inputs as accepted by the existing recorder contract.

If file input is supported, it must be an explicit record input path for a local command, not a new remote read capability. Apply size limits before reading and do not follow this into a general arbitrary-file command.

### Result

Return the existing persisted execution id and replay/idempotency status without echoing raw artifact bodies.

Example:

```json
{
  "schemaVersion": 1,
  "command": "record",
  "status": "success",
  "errorCode": null,
  "actionRequired": null,
  "data": {
    "executionId": "exe_...",
    "recorded": true,
    "idempotentReplay": false
  },
  "warnings": []
}
```

### Security

- existing artifact sanitizer/classification is mandatory;
- changed files still follow workspace visibility rules;
- secrets are not re-emitted by CLI formatting;
- invalid payloads fail with bounded stable errors;
- no raw command output is copied to normal CLI diagnostic logs.

## `c2c logs`

### Boundary

This command reads only the C2C.NET diagnostic source configured by the application. It never accepts an arbitrary path.

Allowed filters can include:

```text
--tail <bounded count>
--since <bounded time>
--level <known level>
--component <known/sanitized component>
--json
```

Do not add `--file /some/path`.

### Storage

If no persistent diagnostic sink exists when this slice starts, add the smallest local-only structured sink needed for C2C-owned runtime diagnostics.

Preferred properties:

- app-state `logs/` location only;
- JSONL or similarly parseable records;
- bounded record length;
- rotation/retention cap;
- redaction before persistence;
- safe concurrent append/read;
- corrupt line skipped/reported safely rather than crashing the reader.

Do not introduce a database for logs.

### Reader contract

A local-only abstraction such as:

```csharp
public interface IDiagnosticLogReader
{
    Task<DiagnosticLogPage> ReadAsync(
        DiagnosticLogRequest request,
        CancellationToken cancellationToken);
}
```

belongs with the diagnostics capability, not under CLI-only business logic.

### Double-defense

Secrets should be redacted before log persistence. The reader/output layer may perform a second bounded sanitization pass, but it must not rely solely on post-read filtering as the primary secret-control mechanism.

## Tests

- record success returns execution id;
- duplicate idempotency key maps to existing V0.3 semantics;
- oversized/invalid record rejected before unsafe allocation/read;
- restricted artifact remains restricted through CLI;
- `logs` cannot accept arbitrary file path;
- tail/byte/time bounds enforced;
- token/private-key/pairing-code canaries never appear in persisted/output logs;
- malformed log line does not expose raw unsafe bytes;
- concurrent logging/read remains bounded;
- JSON stdout is valid one-object output.

## Completion gate

```text
[ ] execution recorder reused, not reimplemented
[ ] c2c record returns stable executionId
[ ] logs source is C2C-owned only
[ ] no arbitrary file read option
[ ] redaction occurs before persistence
[ ] log reads are bounded
[ ] V0.3 execution security tests remain green
```
