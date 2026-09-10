using System;

using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Execution;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Service registration for Execution Evidence capability adhering to 12-DI-CONVENTIONS.md.
/// </summary>
public static class ExecutionServiceCollectionExtensions
{
    public static IServiceCollection AddExecutionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ExecutionOptions>();
        services.AddSingleton<IExecutionStore, JsonExecutionStore>();
        services.AddSingleton<IArtifactSanitizer, ArtifactSanitizer>();
        services.AddSingleton<IExecutionArtifactReader, ArtifactChunkReader>();
        services.AddSingleton<IExecutionRecorder, ExecutionRecorder>();
        services.AddSingleton<IExecutionQuery, ExecutionQuery>();

        return services;
    }
}
