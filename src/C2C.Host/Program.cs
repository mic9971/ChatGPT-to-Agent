using System;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Host.Auth;
using C2C.Host.Mcp;
using C2C.Infrastructure.Authorization;
using C2C.Infrastructure.Execution;
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

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddWorkspaceServices();
        builder.Services.AddGitServices();
        builder.Services.AddExecutionServices();
        builder.Services.AddAuthorizationServices(options =>
        {
            string? storageDir = builder.Configuration["Auth:StorageDirectory"];
            if (!string.IsNullOrWhiteSpace(storageDir))
            {
                options.StorageDirectory = storageDir;
            }
        });

        // OAuth 2.1 Server and Token Validation (UC-AUTH-02, UC-AUTH-03)
        builder.Services.AddC2CAuthorizationServer(options =>
        {
            if (builder.Configuration.GetValue<bool>("Auth:DisableTransportSecurityRequirement") ||
                builder.Environment.IsDevelopment())
            {
                options.DisableTransportSecurityRequirement = true;
            }

            // Remote auth is enabled by default unless explicit LocalOnly profile is configured (BR-SEC-005, 10-MCP-AUTHORIZATION-INTEGRATION.md)
            if (string.Equals(builder.Configuration["Auth:Profile"], "LocalOnly", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableRemoteAuthorization = false;
            }
        });

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

        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware enforcing MCP bearer authentication and workspace/grant validation
        app.UseMiddleware<McpAuthorizationMiddleware>();

        // Map OAuth endpoints (/connect/authorize, /connect/token, /.well-known/oauth-protected-resource)
        app.MapAuthorizationEndpoints();

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
