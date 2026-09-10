using System;
using System.IO;
using System.Runtime.InteropServices;

using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel.Cloudflare;

/// <summary>
/// Resolves cloudflared executable location from options, system PATH, or standard OS directories.
/// </summary>
public static class CloudflaredBinaryResolver
{
    public static string? Resolve(TunnelOptions? options = null)
    {
        // 1. Check explicit configuration
        if (!string.IsNullOrWhiteSpace(options?.BinaryPath))
        {
            string explicitPath = Path.GetFullPath(options.BinaryPath);
            return File.Exists(explicitPath) ? explicitPath : null;
        }

        string binaryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "cloudflared.exe"
            : "cloudflared";

        // 2. Search PATH environment variable
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathEnv))
        {
            string[] paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string dir in paths)
            {
                try
                {
                    string candidate = Path.Combine(dir, binaryName);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch
                {
                    // Ignore path probing exceptions
                }
            }
        }

        // 3. Search well-known system directories
        string[] wellKnownDirs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ?
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "cloudflared"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cloudflared")
            ]
            :
            [
                "/opt/homebrew/bin",
                "/usr/local/bin",
                "/usr/bin"
            ];

        foreach (string dir in wellKnownDirs)
        {
            string candidate = Path.Combine(dir, binaryName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
