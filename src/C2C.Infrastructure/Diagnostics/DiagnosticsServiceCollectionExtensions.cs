using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using C2C.Core.Diagnostics;

namespace C2C.Infrastructure.Diagnostics;

/// <summary>
/// Service collection extensions for runtime diagnostics (UC-CLI-03).
/// </summary>
public static class DiagnosticsServiceCollectionExtensions
{
    public static IServiceCollection AddRuntimeDiagnostics(this IServiceCollection services)
    {
        services.TryAddSingleton<IRuntimeDiagnostics, RuntimeDiagnostics>();
        services.TryAddSingleton<JsonDiagnosticLogStore>();
        services.TryAddSingleton<IDiagnosticLogReader>(sp => sp.GetRequiredService<JsonDiagnosticLogStore>());
        services.TryAddSingleton<IDiagnosticLogSink>(sp => sp.GetRequiredService<JsonDiagnosticLogStore>());
        return services;
    }
}
