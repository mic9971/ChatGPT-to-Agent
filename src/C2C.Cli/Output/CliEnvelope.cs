using System.Collections.Generic;

namespace C2C.Cli.Output;

/// <summary>
/// Stable machine-readable envelope for CLI commands adhering to 03-CLI-CONTRACT-AND-EXIT-CODES.md.
/// </summary>
public sealed class CliEnvelope<T>
{
    public int SchemaVersion { get; init; } = 1;

    public required string Command { get; init; }

    public required string Status { get; init; }

    public string? ErrorCode { get; init; }

    public string? ActionRequired { get; init; }

    public T? Data { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
