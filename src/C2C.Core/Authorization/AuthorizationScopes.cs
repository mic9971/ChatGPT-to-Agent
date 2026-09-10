using System;
using System.Collections.Generic;

namespace C2C.Core.Authorization;

/// <summary>
/// V1 OAuth scope definitions adhering to 04-DETAILED-AUTH-DESIGN.md, BR-SEC-006, and BR-SEC-007.
/// Only read-only scopes are permissible in V1 (BR-COM-002).
/// </summary>
public static class AuthorizationScopes
{
    public const string WorkspaceRead = "workspace.read";
    public const string WorkspaceSearch = "workspace.search";
    public const string GitRead = "git.read";
    public const string ExecutionRead = "execution.read";
    public const string OfflineAccess = "offline_access";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        WorkspaceRead,
        WorkspaceSearch,
        GitRead,
        ExecutionRead,
        OfflineAccess
    };

    public static bool IsValid(string scope) => All.Contains(scope);

    public static bool ValidateAll(IEnumerable<string> scopes, out string? invalidScope)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        foreach (string scope in scopes)
        {
            if (!IsValid(scope))
            {
                invalidScope = scope;
                return false;
            }
        }

        invalidScope = null;
        return true;
    }
}
