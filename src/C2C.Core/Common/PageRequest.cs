namespace C2C.Core.Common;

/// <summary>
/// Common pagination request contract adhering to docs/02-common/07-COMMON-CONTRACTS.md.
/// </summary>
public sealed record PageRequest(string? Cursor = null, int? Limit = null);
