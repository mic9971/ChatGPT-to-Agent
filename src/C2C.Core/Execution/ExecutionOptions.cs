using System;

namespace C2C.Core.Execution;

/// <summary>
/// Operational limits and persistence configuration for execution evidence.
/// </summary>
public sealed class ExecutionOptions
{
    public string? StorageDirectory { get; set; }

    public int MaxChangedFiles { get; set; } = 200;

    public int MaxArtifacts { get; set; } = 10;

    public int MaxArtifactBytes { get; set; } = 2_000_000;

    public int DefaultChunkLines { get; set; } = 100;

    public int MaxChunkLines { get; set; } = 500;

    public int DefaultChunkBytes { get; set; } = 64_000;

    public int MaxChunkBytes { get; set; } = 256_000;

    public int MaxFailingTestNames { get; set; } = 20;

    public int MaxCommandTextLength { get; set; } = 500;

    public int RetentionDaysRecord { get; set; } = 30;

    public int RetentionDaysArtifact { get; set; } = 7;
}
