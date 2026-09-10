# Observability Reference

Use `ILogger<T>` structured logging with stable event ids/categories.

Safe correlation fields:

- workspace public/salted id;
- task id;
- execution id;
- operation name;
- duration/status/error code.

Never log:

- raw access/refresh token;
- Authorization headers;
- pairing code;
- private key;
- source file bodies;
- Git diff bodies;
- full home-directory path when avoidable.

Metrics/traces may use OpenTelemetry later. Telemetry must be opt-in for remote export and must not contain source content. Local diagnostics remain useful without external telemetry.
