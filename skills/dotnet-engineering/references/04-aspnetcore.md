# ASP.NET Core Reference

- Keep request paths asynchronous and non-blocking.
- Bound request/response body sizes and collections.
- Never capture `HttpContext` for background work after the request completes.
- Minimal API handlers should delegate immediately to application services; avoid business logic in route lambdas.
- Validate options at startup for security-critical configuration.
- Return safe problem/error contracts; do not send stack traces or local paths.
- Use middleware ordering intentionally: forwarded headers (if any), auth, authorization, MCP routing, local admin restrictions.
- Local admin surface and public tunneled surface should be route-separated so the tunnel cannot accidentally expose admin commands.
- Outbound HTTP uses named/typed clients; resilience/retry is explicit and idempotency-aware.
