using System;

namespace C2C.Core.Authorization;

/// <summary>
/// Configurable options for pairing session creation adhering to UC-AUTH-01 and 05-UC-AUTH-01-IMPLEMENTATION.md.
/// </summary>
public sealed class PairingOptions
{
    public int CodeLength { get; set; } = 8;

    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);

    public int MaxAttempts { get; set; } = 3;

    public string? StorageDirectory { get; set; }
}
