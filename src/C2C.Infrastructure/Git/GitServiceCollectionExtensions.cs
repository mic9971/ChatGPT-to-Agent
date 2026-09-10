using System;

using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Git;

namespace C2C.Infrastructure.Git;

/// <summary>
/// Service registration for Git Evidence capability per 12-DI-CONVENTIONS.md.
/// </summary>
public static class GitServiceCollectionExtensions
{
    public static IServiceCollection AddGitServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<GitOptions>();
        services.AddSingleton<IGitProcess, GitProcess>();
        services.AddSingleton<IGitReader, GitReader>();

        return services;
    }
}
