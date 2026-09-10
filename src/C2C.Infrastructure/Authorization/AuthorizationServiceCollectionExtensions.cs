using System;

using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Service registration for Authorization and Pairing capability adhering to 12-DI-CONVENTIONS.md.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationServices(
        this IServiceCollection services,
        Action<PairingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        PairingOptions options = new();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddSingleton<IPairingStore, JsonPairingStore>();
        services.AddSingleton<IPairingService, PairingService>();
        services.AddSingleton<IAuthorizationStateStore, JsonAuthorizationStateStore>();
        services.AddSingleton<IRefreshFamilyStore, JsonRefreshFamilyStore>();
        services.AddSingleton<IAuthorizationRevoker, AuthorizationRevoker>();
        services.AddSingleton<IClientMetadataResolver, CimdClientMetadataResolver>();
        services.AddSingleton<ICodeRedemptionTracker, MemoryCodeRedemptionTracker>();

        return services;
    }
}
