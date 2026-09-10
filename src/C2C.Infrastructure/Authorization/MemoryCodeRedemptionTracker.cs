using System;
using System.Collections.Concurrent;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// In-memory implementation of ICodeRedemptionTracker preventing replay of authorization codes
/// adhering to BR-AUTH-005 and BR-CON-005.
/// </summary>
public sealed class MemoryCodeRedemptionTracker : ICodeRedemptionTracker
{
    private readonly ConcurrentDictionary<string, byte> _redeemedCodes = new(StringComparer.Ordinal);

    public bool TryRedeemCode(string codeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeId);
        return _redeemedCodes.TryAdd(codeId, 0);
    }
}
