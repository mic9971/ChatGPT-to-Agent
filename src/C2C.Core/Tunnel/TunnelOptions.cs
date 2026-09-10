using System;

namespace C2C.Core.Tunnel;

/// <summary>
/// Configuration options for tunnel execution and lifecycle.
/// </summary>
public sealed class TunnelOptions
{
    public string? BinaryPath { get; set; }

    public int StartupTimeoutMs { get; set; } = 30_000;

    public string? StorageDirectory { get; set; }

    public int MaxLogBufferLines { get; set; } = 500;

    public Uri LocalEndpoint { get; set; } = new("http://127.0.0.1:5000");
}
