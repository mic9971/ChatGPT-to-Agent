using System;
using System.Diagnostics;

using C2C.Core.Tunnel;

namespace C2C.Infrastructure.Tunnel;

/// <summary>
/// Validates process ownership against OS process table adhering to UC-TUN-02, BR-CON-008, and BR-COM-011.
/// Fails closed on any security or access ambiguity.
/// </summary>
public sealed class SystemOwnedProcessValidator : IOwnedProcessValidator
{
    private static readonly TimeSpan MaxStartTimeTolerance = TimeSpan.FromSeconds(2);

    public ProcessOwnershipStatus ValidateOwnership(
        int processId,
        DateTimeOffset expectedStartTime,
        string expectedOwnershipMarker,
        string? expectedProcessName = null)
    {
        if (processId <= 0)
        {
            return ProcessOwnershipStatus.NotRunning;
        }

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            // Process ID does not exist in the OS process table
            return ProcessOwnershipStatus.NotRunning;
        }
        catch
        {
            // Access denied or foreign privilege level -> fail closed (BR-COM-011)
            return ProcessOwnershipStatus.ForeignOrReused;
        }

        using (process)
        {
            try
            {
                if (process.HasExited)
                {
                    return ProcessOwnershipStatus.NotRunning;
                }

                // 1. Verify start time with skew tolerance to prevent PID reuse attacks/accidents (BR-CON-008)
                DateTime osStartTimeUtc = process.StartTime.ToUniversalTime();
                TimeSpan skew = (osStartTimeUtc - expectedStartTime.UtcDateTime).Duration();

                if (skew > MaxStartTimeTolerance)
                {
                    // Process with this PID was started at a different time; PID was reused by OS
                    return ProcessOwnershipStatus.ForeignOrReused;
                }

                // 2. Verify process name if specified (e.g. "cloudflared")
                if (!string.IsNullOrWhiteSpace(expectedProcessName))
                {
                    if (!process.ProcessName.Contains(expectedProcessName, StringComparison.OrdinalIgnoreCase))
                    {
                        return ProcessOwnershipStatus.ForeignOrReused;
                    }
                }

                return ProcessOwnershipStatus.OwnedAndActive;
            }
            catch (InvalidOperationException)
            {
                // Process exited between retrieval and inspection
                return ProcessOwnershipStatus.NotRunning;
            }
            catch
            {
                // Inaccessible attributes -> fail closed
                return ProcessOwnershipStatus.ForeignOrReused;
            }
        }
    }
}
