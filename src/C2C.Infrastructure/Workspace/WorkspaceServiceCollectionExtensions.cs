using System;

using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Service registration extensions for Workspace capability adhering to
/// skills/dotnet-project-conventions/references/12-DI-CONVENTIONS.md.
/// </summary>
public static class WorkspaceServiceCollectionExtensions
{
    public static IServiceCollection AddWorkspaceServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Path resolution & security policies
        services.AddSingleton<ICanonicalPathResolver, CanonicalPathResolver>();
        services.AddSingleton<ISensitivePathPolicy, SensitivePathPolicy>();
        services.AddSingleton<IWorkspaceAccessPolicy, WorkspaceAccessPolicy>();

        // Workspace file reading
        services.AddSingleton<WorkspaceFileReaderOptions>();
        services.AddSingleton<IWorkspaceFileReader, WorkspaceFileReader>();

        // Workspace directory listing
        services.AddSingleton<WorkspaceDirectoryReaderOptions>();
        services.AddSingleton<IWorkspaceDirectoryReader, WorkspaceDirectoryReader>();

        // Workspace text search
        services.AddSingleton<WorkspaceSearchOptions>();
        services.AddSingleton<ISearchBackend, ManagedSearchBackend>();
        services.AddSingleton<IWorkspaceSearchService, WorkspaceSearchService>();

        // Workspace configuration & identity
        services.AddSingleton<IWorkspaceInfoService, WorkspaceInfoService>();
        services.AddSingleton<IWorkspaceConfigStore, JsonWorkspaceConfigStore>();
        services.AddSingleton<IWorkspaceIdentityFactory, Sha256WorkspaceIdentityFactory>();
        services.AddSingleton<IWorkspaceConfigurator, WorkspaceConfigurator>();

        return services;
    }
}
