using System;
using System.Collections.Generic;

namespace C2C.Core.Authorization;

/// <summary>
/// Authoritative mapping of MCP read-only tools to their required OAuth scopes
/// adhering to UC-MCP-02, 04-DETAILED-AUTH-DESIGN.md, and 10-MCP-AUTHORIZATION-INTEGRATION.md.
/// </summary>
public static class McpToolScopes
{
    private static readonly Dictionary<string, string> ToolToScope = new(StringComparer.Ordinal)
    {
        ["workspace_info"] = AuthorizationScopes.WorkspaceRead,
        ["read_file"] = AuthorizationScopes.WorkspaceRead,
        ["list_directory"] = AuthorizationScopes.WorkspaceRead,
        ["search_workspace"] = AuthorizationScopes.WorkspaceSearch,
        ["git_status"] = AuthorizationScopes.GitRead,
        ["git_diff"] = AuthorizationScopes.GitRead,
        ["execution_summary"] = AuthorizationScopes.ExecutionRead,
        ["test_status"] = AuthorizationScopes.ExecutionRead,
        ["execution_output"] = AuthorizationScopes.ExecutionRead
    };

    public static string? GetRequiredScope(string toolName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        return ToolToScope.TryGetValue(toolName, out string? scope) ? scope : null;
    }

    public static IReadOnlyDictionary<string, string> AllMappings => ToolToScope;
}
