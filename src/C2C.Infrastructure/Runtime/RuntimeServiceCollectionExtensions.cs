using System;
using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Runtime;
using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Runtime;

/// <summary>
/// Service registration for Runtime lifecycle and host management adhering to 12-DI-CONVENTIONS.md.
/// </summary>
public static class RuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddRuntimeServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRuntimeOwnershipStore, JsonRuntimeOwnershipStore>();
        services.AddSingleton<IRuntimeHostLauncher, DefaultRuntimeHostLauncher>();
        services.AddSingleton<IRuntimeController, RuntimeController>();
        services.AddSingleton<IPairingProbe, AuthorizationPairingProbe>();

        return services;
    }
}
