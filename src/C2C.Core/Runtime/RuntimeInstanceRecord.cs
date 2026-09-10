using System;

namespace C2C.Core.Runtime;

/// <summary>
/// Persisted runtime ownership record adhering to 05-UC-CLI-02-RUNTIME-LIFECYCLE.md and BR-CON-008.
/// Stores process identity to guard against PID reuse attacks or foreign process termination.
/// </summary>
public sealed record RuntimeInstanceRecord
{
    public required string WorkspaceId { get; init; }

    public required string RuntimeInstanceId { get; init; }

    public required int ProcessId { get; init; }

    public required DateTimeOffset ProcessStartTime { get; init; }

    public required string LocalEndpoint { get; init; }

    public string? ExecutablePath { get; init; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public int SchemaVersion { get; init; } = 1;
}
