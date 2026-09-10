using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Runtime;

namespace C2C.Infrastructure.Runtime;

/// <summary>
/// Production runtime host launcher that executes C2C.Host as a detached background process adhering to BR-CON-008 and BR-SEC-005.
/// </summary>
public sealed class DefaultRuntimeHostLauncher : IRuntimeHostLauncher
{
    private readonly string? _explicitHostPath;

    public DefaultRuntimeHostLauncher(string? explicitHostPath = null)
    {
        _explicitHostPath = explicitHostPath;
    }

    public Task<LaunchedProcessInfo> LaunchAsync(
        string workspaceId,
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceId);
        ArgumentNullException.ThrowIfNull(endpoint);

        var (fileName, arguments, exePath) = ResolveHostExecution(endpoint);

        ProcessStartInfo startInfo = new(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        foreach (string arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        Process process = new() { StartInfo = startInfo };

        try
        {
            process.Start();

            DateTimeOffset startTime;
            try
            {
                startTime = process.StartTime.ToUniversalTime();
            }
            catch
            {
                startTime = DateTimeOffset.UtcNow;
            }

            return Task.FromResult(new LaunchedProcessInfo(process.Id, startTime, exePath));
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private (string FileName, string[] Arguments, string ExecutablePath) ResolveHostExecution(Uri endpoint)
    {
        // 1. Explicit host path or environment variable
        string? candidate = _explicitHostPath ?? Environment.GetEnvironmentVariable("C2C_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
        {
            return BuildInvocation(candidate, endpoint);
        }

        // 2. Proximity in AppContext.BaseDirectory
        string baseDir = AppContext.BaseDirectory;
        string[] localProbes =
        {
            Path.Combine(baseDir, "C2C.Host"),
            Path.Combine(baseDir, "C2C.Host.exe"),
            Path.Combine(baseDir, "C2C.Host.dll")
        };

        foreach (string probe in localProbes)
        {
            if (File.Exists(probe))
            {
                return BuildInvocation(probe, endpoint);
            }
        }

        // 3. Solution repository source tree probing
        string? current = baseDir;
        for (int i = 0; i < 6 && current != null; i++)
        {
            string debugDll = Path.Combine(current, "src", "C2C.Host", "bin", "Debug", "net8.0", "C2C.Host.dll");
            if (File.Exists(debugDll))
            {
                return BuildInvocation(debugDll, endpoint);
            }

            string releaseDll = Path.Combine(current, "src", "C2C.Host", "bin", "Release", "net8.0", "C2C.Host.dll");
            if (File.Exists(releaseDll))
            {
                return BuildInvocation(releaseDll, endpoint);
            }

            current = Directory.GetParent(current)?.FullName;
        }

        // Fallback: assume dotnet executable with C2C.Host.dll in base directory
        string defaultDll = Path.Combine(baseDir, "C2C.Host.dll");
        return ("dotnet", new[] { defaultDll, "--urls", endpoint.ToString() }, defaultDll);
    }

    private static (string FileName, string[] Arguments, string ExecutablePath) BuildInvocation(string path, Uri endpoint)
    {
        string fullPath = Path.GetFullPath(path);
        if (fullPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return ("dotnet", new[] { fullPath, "--urls", endpoint.ToString() }, fullPath);
        }

        return (fullPath, new[] { "--urls", endpoint.ToString() }, fullPath);
    }
}
