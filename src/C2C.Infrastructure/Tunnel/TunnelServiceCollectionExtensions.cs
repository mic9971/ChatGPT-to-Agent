using System;

using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Tunnel;
using C2C.Infrastructure.Tunnel.Cloudflare;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Service registration for Tunnel capability adhering to 12-DI-CONVENTIONS.md.
/// </summary>
public static class TunnelServiceCollectionExtensions
{
    public static IServiceCollection AddTunnelServices(
        this IServiceCollection services,
        Action<TunnelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TunnelOptions options = new();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddSingleton<ITunnelSessionStore, JsonTunnelSessionStore>();
        services.AddSingleton<IOwnedProcessValidator, SystemOwnedProcessValidator>();
        services.AddSingleton<IOwnedProcessRunner, SystemOwnedProcessRunner>();
        services.AddSingleton<ITunnelProvider, CloudflareQuickTunnelProvider>();
        services.AddSingleton<ITunnelService, TunnelService>();
        services.AddSingleton<IBridgeRuntime, LoopbackBridgeRuntime>();
        services.AddSingleton<IPairingProbe, DefaultPairingProbe>();
        services.AddSingleton<IRuntimeEnsurer, RuntimeEnsurer>();

        return services;
    }
}
