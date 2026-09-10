# 07 — Tunnel Design

## Goal

Expose the loopback-only MCP bridge to an authorized remote MCP client without directly binding the bridge to a public interface.

## Abstraction

```csharp
public interface ITunnelProvider
{
    Task<TunnelSession> StartAsync(Uri localEndpoint, CancellationToken cancellationToken);
    Task StopAsync(TunnelSession session, CancellationToken cancellationToken);
    Task<TunnelHealth> GetHealthAsync(TunnelSession session, CancellationToken cancellationToken);
}
```

`C2C.Core` knows only the abstraction/data contract. Provider implementation lives in Infrastructure.

## Initial providers

### Cloudflare Quick Tunnel
Best first implementation because it needs little user setup. Public URL may change after restart; `ensure` must detect stale connector/config and return a repair-required status rather than silently assuming the old URL remains valid.

### Cloudflare Named/Stable Tunnel
Later profile for stable hostname/domain and lower reconnect friction.

Possible future providers: ngrok, Tailscale Funnel, custom reverse proxy. Do not implement until needed.

## Process management

Use `System.Diagnostics.Process` through an `IProcessRunner` abstraction. Requirements:

- explicit executable path resolution;
- bounded startup timeout;
- capture stdout/stderr through a redacting logger/parser;
- no shell-string interpolation when argument-list APIs are available;
- graceful stop then forced kill after timeout;
- child-process ownership marker so `stop` does not kill unrelated `cloudflared` processes.

## Security invariants

- bridge stays on `127.0.0.1`/`::1` only;
- tunnel is HTTPS public surface;
- public MCP endpoint still requires authorization;
- URL secrecy is never an authentication mechanism;
- local admin routes are not intentionally exposed through the tunnel; map/route middleware must prevent it.

## Health model

`TunnelHealth`: `Starting`, `Healthy`, `Degraded`, `Stopped`, `Failed`. Include safe reason codes, not raw credentials/command lines.
