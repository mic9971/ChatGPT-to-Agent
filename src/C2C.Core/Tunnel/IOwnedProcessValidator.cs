using System;

namespace C2C.Core.Tunnel;

/// <summary>
/// Status of an operating system process compared against expected tunnel session ownership metadata.
/// </summary>
public enum ProcessOwnershipStatus
{
    /// <summary>
    /// The process does not exist or has already exited.
    /// </summary>
    NotRunning,

    /// <summary>
    /// The process exists, is currently running, and matches expected start time and ownership identity.
    /// </summary>
    OwnedAndActive,

    /// <summary>
    /// A process with this ID exists, but start time or binary identity do not match (reused PID or foreign process).
    /// </summary>
    ForeignOrReused
}

/// <summary>
/// Contract for validating process ownership identity adhering to UC-TUN-02 and BR-CON-008.
/// Prevents signaling or killing foreign processes upon stale or reused PIDs.
/// </summary>
public interface IOwnedProcessValidator
{
    /// <summary>
    /// Validates whether a process ID matches the expected start time and ownership marker of an owned session.
    /// </summary>
    /// <param name="processId">The operating system process ID.</param>
    /// <param name="expectedStartTime">The recorded UTC start time of the owned process.</param>
    /// <param name="expectedOwnershipMarker">The unique ownership marker recorded at spawn.</param>
    /// <param name="expectedProcessName">Optional expected executable/process name (e.g. "cloudflared").</param>
    /// <returns>A <see cref="ProcessOwnershipStatus"/> indicating ownership validity.</returns>
    ProcessOwnershipStatus ValidateOwnership(
        int processId,
        DateTimeOffset expectedStartTime,
        string expectedOwnershipMarker,
        string? expectedProcessName = null);
}
