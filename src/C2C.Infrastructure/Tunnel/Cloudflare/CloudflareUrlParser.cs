using System;
using System.Text.RegularExpressions;

namespace C2C.Infrastructure.Tunnel.Cloudflare;

/// <summary>
/// Parser for discovering and validating Cloudflare Quick Tunnel public URLs from process output per UC-TUN-01.
/// </summary>
public static class CloudflareUrlParser
{
    private static readonly Regex AnsiEscapeRegex = new(
        @"\x1B\[[0-9;]*[a-zA-Z]",
        RegexOptions.Compiled);

    private static readonly Regex TryCloudflareRegex = new(
        @"https://([a-zA-Z0-9-]+)\.trycloudflare\.com",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Uri? TryParseUrl(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        // Strip ANSI escape codes
        string clean = AnsiEscapeRegex.Replace(line, string.Empty).Trim();

        Match match = TryCloudflareRegex.Match(clean);
        if (!match.Success)
        {
            return null;
        }

        string rawUrl = match.Value;
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        // Validate strictly: must be HTTPS and host must end with .trycloudflare.com
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!uri.Host.EndsWith(".trycloudflare.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri;
    }
}
