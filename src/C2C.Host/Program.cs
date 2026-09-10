using System;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Host.Mcp;
using C2C.Infrastructure.Git;
using C2C.Infrastructure.Workspace;

namespace C2C.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // BR-SEC-005: Bridge listener binds loopback only in V1. Fail on wildcard/public.
        ValidateBindingAddresses(builder.Configuration["ASPNETCORE_URLS"]);

        // Time abstraction (BR-CON-002, 03-DOTNET.md)
        builder.Services.AddSingleton(TimeProvider.System);

        // Register Core & Infrastructure services by capability (12-DI-CONVENTIONS.md)
        builder.Services.AddWorkspaceServices();
        builder.Services.AddGitServices();

        // Default workspace context if none provided (e.g. from environment or config store)
        builder.Services.AddSingleton<IWorkspaceContext>(sp =>
        {
            string currentDir = Directory.GetCurrentDirectory();
            var factory = sp.GetRequiredService<IWorkspaceIdentityFactory>();
            return new WorkspaceContext(factory.Create(currentDir), currentDir, "Default Workspace");
        });

        // Configure MCP server using official C# SDK (UC-MCP-01)
        var mcpBuilder = builder.Services.AddMcpServer();
        McpServerConfigurator.Configure(mcpBuilder);

        var app = builder.Build();

        // Map stateless MCP endpoint at /mcp
        app.MapMcp("/mcp");

        await app.RunAsync();
    }

    public static void ValidateBindingAddresses(string? urls)
    {
        if (string.IsNullOrWhiteSpace(urls))
        {
            return;
        }

        string[] addresses = urls.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (string addr in addresses)
        {
            if (addr.Contains("0.0.0.0", StringComparison.Ordinal) ||
                addr.Contains("://*:", StringComparison.Ordinal) ||
                addr.Contains("://+:", StringComparison.Ordinal) ||
                addr.Contains("://*;", StringComparison.Ordinal) ||
                addr.EndsWith("://*", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"BR-SEC-005 violation: Bridge listener binds loopback only in V1. Wildcard/public address '{addr}' is strictly forbidden.");
            }
        }
    }
}
