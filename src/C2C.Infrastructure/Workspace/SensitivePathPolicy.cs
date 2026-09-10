using System.Text.RegularExpressions;

using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements sensitive path deny policy per BR-SEC-003 and architecture/05-WORKSPACE-SECURITY.md.
/// </summary>
public sealed class SensitivePathPolicy : ISensitivePathPolicy
{
    private static readonly HashSet<string> SafeAllowedFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env.example",
        ".env.sample",
        ".env.template"
    };

    private static readonly string[] SensitiveDirectories =
    [
        ".ssh",
        ".aws",
        ".azure",
        ".gcp",
        ".git"
    ];

    private static readonly string[] SensitiveExtensions =
    [
        ".pem",
        ".p12",
        ".pfx",
        ".key"
    ];

    private static readonly Regex[] SensitiveNamePatterns =
    [
        new(@"^\.env(\..+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^id_(rsa|ed25519|ecdsa|dsa).*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^credentials.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^service-account.*\.json$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@".*keychain.*", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public bool IsSensitive(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string normalized = relativePath.Replace('\\', '/').Trim('/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return false;
        }

        string fileName = segments[^1];

        // 1. Check explicit safe exceptions first
        if (SafeAllowedFilenames.Contains(fileName))
        {
            return false;
        }

        // 2. Check directory segments
        foreach (string segment in segments[..^1])
        {
            foreach (string sensitiveDir in SensitiveDirectories)
            {
                if (string.Equals(segment, sensitiveDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // If a directory itself is the target
        foreach (string sensitiveDir in SensitiveDirectories)
        {
            if (string.Equals(fileName, sensitiveDir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 3. Check sensitive file extensions
        foreach (string ext in SensitiveExtensions)
        {
            if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // 4. Check regex filename patterns
        foreach (Regex pattern in SensitiveNamePatterns)
        {
            if (pattern.IsMatch(fileName))
            {
                return true;
            }
        }

        return false;
    }
}
