using System;
using Microsoft.Extensions.DependencyInjection;

using C2C.Cli.Commands;
using C2C.Core.Workspace;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Diagnostics;
using C2C.Infrastructure.Execution;
using C2C.Infrastructure.Git;
using C2C.Infrastructure.Runtime;
using C2C.Infrastructure.Tunnel;
using C2C.Infrastructure.Workspace;

namespace C2C.Cli.Composition;

/// <summary>
/// Composition root for CLI service registration adhering to 12-DI-CONVENTIONS.md.
/// </summary>
public static class CliServiceCollectionExtensions
{
    public static IServiceCollection AddCliServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);

        // Core & Infrastructure capability slices
        services.AddWorkspaceServices();
        services.AddGitServices();
        services.AddExecutionServices();
        services.AddAuthorizationServices();
        services.AddTunnelServices();
        services.AddRuntimeServices();
        services.AddRuntimeDiagnostics();

        // Default workspace context for CLI execution
        services.AddSingleton<IWorkspaceContext>(sp =>
        {
            var store = sp.GetRequiredService<IWorkspaceConfigStore>();
            var config = store.LoadAsync().GetAwaiter().GetResult();
            if (config != null)
            {
                return new WorkspaceContext(config.WorkspaceId, config.CanonicalRoot, config.Label);
            }

            var factory = sp.GetRequiredService<IWorkspaceIdentityFactory>();
            string currentDir = Environment.CurrentDirectory;
            return new WorkspaceContext(factory.Create(currentDir), currentDir, "CLI Workspace");
        });

        // CLI commands
        services.AddTransient<SetupCommand>();
        services.AddTransient<StatusCommand>();
        services.AddTransient<DoctorCommand>();
        services.AddTransient<StartCommand>();
        services.AddTransient<StopCommand>();
        services.AddTransient<EnsureCommand>();
        services.AddTransient<PairCommand>();
        services.AddTransient<UnpairCommand>();
        services.AddTransient<RecordCommand>();
        services.AddTransient<LogsCommand>();

        return services;
    }
}
