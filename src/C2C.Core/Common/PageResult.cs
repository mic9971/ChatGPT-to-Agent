namespace C2C.Core.Common;

/// <summary>
/// Common pagination result contract adhering to docs/02-common/07-COMMON-CONTRACTS.md.
/// </summary>
public sealed record PageResult<T>(IReadOnlyList<T> Items, string? NextCursor);
