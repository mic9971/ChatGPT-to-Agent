using System.Text.RegularExpressions;

using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Enforces additive .c2cignore exclusions per BR-SEC-004 and architecture/05-WORKSPACE-SECURITY.md.
/// </summary>
public sealed class IgnorePolicy : IIgnorePolicy
{
    private const int MaxIgnoreFileSizeBytes = 65536; // 64 KB cap
    private const int MaxRuleCount = 500;

    private readonly List<Regex> _compiledRules = [];

    public IgnorePolicy(string? canonicalRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(canonicalRoot))
        {
            LoadFromWorkspace(canonicalRoot);
        }
    }

    public IgnorePolicy(IEnumerable<string> rules)
    {
        ParseRules(rules);
    }

    private void LoadFromWorkspace(string canonicalRoot)
    {
        string ignorePath = Path.Combine(canonicalRoot, ".c2cignore");
        if (!File.Exists(ignorePath))
        {
            return;
        }

        FileInfo fileInfo = new(ignorePath);
        if (fileInfo.Length > MaxIgnoreFileSizeBytes)
        {
            throw new InvalidOperationException(
                $".c2cignore exceeds maximum allowable size of {MaxIgnoreFileSizeBytes} bytes.");
        }

        string[] lines = File.ReadAllLines(ignorePath);
        ParseRules(lines);
    }

    private void ParseRules(IEnumerable<string> lines)
    {
        int count = 0;
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            count++;
            if (count > MaxRuleCount)
            {
                throw new InvalidOperationException(
                    $".c2cignore exceeds maximum allowable rule count of {MaxRuleCount}.");
            }

            // In V1, .c2cignore is additive deny only. Negations '!' are ignored or disallowed.
            if (line.StartsWith('!'))
            {
                // In V1, additive only; negation cannot un-deny
                continue;
            }

            string pattern = ConvertGlobToRegex(line);
            _compiledRules.Add(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }
    }

    public bool IsIgnored(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string normalized = relativePath.Replace('\\', '/').Trim('/');

        foreach (Regex rule in _compiledRules)
        {
            if (rule.IsMatch(normalized))
            {
                return true;
            }
        }

        return false;
    }

    private static string ConvertGlobToRegex(string glob)
    {
        string g = glob.Replace('\\', '/').Trim();
        bool matchDirectoryOnly = g.EndsWith('/');
        if (matchDirectoryOnly)
        {
            g = g.TrimEnd('/');
        }

        bool matchFromRoot = g.StartsWith('/');
        if (matchFromRoot)
        {
            g = g.TrimStart('/');
        }

        // Convert wildcard glob to regex pattern
        string escaped = Regex.Escape(g)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".");

        if (matchFromRoot)
        {
            return matchDirectoryOnly
                ? $"^{escaped}(/.*)?$"
                : $"^{escaped}(/.*)?$";
        }

        return matchDirectoryOnly
            ? $"(^|/){escaped}(/.*)?$"
            : $"(^|/){escaped}(/.*)?$";
    }
}
