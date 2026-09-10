using System;
using System.IO;

using Xunit;

using C2C.Core.Tunnel;
using C2C.Infrastructure.Tunnel.Cloudflare;

namespace C2C.Core.Tests.Tunnel;

public sealed class CloudflaredBinaryResolverTests : IDisposable
{
    private readonly string _tempDir;

    public CloudflaredBinaryResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "c2c_resolver_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public void Resolve_ExplicitExistingBinary_ReturnsPath()
    {
        string fakeBinary = Path.Combine(_tempDir, "fake-cloudflared");
        File.WriteAllText(fakeBinary, "echo binary");

        TunnelOptions options = new() { BinaryPath = fakeBinary };
        string? resolved = CloudflaredBinaryResolver.Resolve(options);

        Assert.NotNull(resolved);
        Assert.Equal(Path.GetFullPath(fakeBinary), resolved);
    }

    [Fact]
    public void Resolve_ExplicitNonExistentBinary_ReturnsNull()
    {
        TunnelOptions options = new() { BinaryPath = Path.Combine(_tempDir, "does-not-exist") };
        string? resolved = CloudflaredBinaryResolver.Resolve(options);

        Assert.Null(resolved);
    }
}
