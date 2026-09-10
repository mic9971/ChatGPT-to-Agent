using System.Text;

using C2C.Core.Git;

namespace C2C.Infrastructure.Git;

/// <summary>
/// Parses git status --porcelain=v1 -z output into typed <see cref="GitStatusEntry"/> records.
/// Uses NUL-delimited (-z) format to safely handle filenames with spaces or special characters.
/// </summary>
public static class GitStatusParser
{
    /// <summary>
    /// Parses the NUL-delimited porcelain v1 output from "git status --porcelain=v1 -z".
    /// Each entry is one or two NUL-terminated tokens (two for renames).
    /// </summary>
    public static IReadOnlyList<GitStatusEntry> Parse(string rawOutput)
    {
        ArgumentNullException.ThrowIfNull(rawOutput);

        if (string.IsNullOrEmpty(rawOutput))
        {
            return [];
        }

        var entries = new List<GitStatusEntry>();
        // NUL-delimited: split on '\0', filter empty trailing token
        string[] tokens = rawOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);

        int i = 0;
        while (i < tokens.Length)
        {
            string token = tokens[i];

            // Each entry starts with a 2-char XY status code + space + path
            if (token.Length < 4)
            {
                // Malformed entry — skip safely, never leak raw token
                i++;
                continue;
            }

            char indexStatus = token[0];
            char worktreeStatus = token[1];
            // token[2] is a space separator
            string path = token[3..];

            string? originalPath = null;

            // Rename/copy entries: next token is the original path
            bool isRename = indexStatus == 'R' || indexStatus == 'C' ||
                            worktreeStatus == 'R' || worktreeStatus == 'C';
            if (isRename && i + 1 < tokens.Length)
            {
                i++;
                originalPath = tokens[i];
            }

            string statusLabel = BuildStatusLabel(indexStatus, worktreeStatus);

            entries.Add(new GitStatusEntry
            {
                IndexStatus = indexStatus,
                WorktreeStatus = worktreeStatus,
                RelativePath = path,
                OriginalPath = originalPath,
                StatusLabel = statusLabel
            });

            i++;
        }

        return entries;
    }

    private static string BuildStatusLabel(char index, char worktree)
    {
        // Prioritize index status for label
        return (index, worktree) switch
        {
            ('A', _) => "added",
            ('M', _) => "modified",
            ('D', _) => "deleted",
            ('R', _) => "renamed",
            ('C', _) => "copied",
            ('?', '?') => "untracked",
            ('!', '!') => "ignored",
            (_, 'M') => "modified",
            (_, 'D') => "deleted",
            _ => $"changed ({index}{worktree})"
        };
    }
}
