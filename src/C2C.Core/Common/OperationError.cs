namespace C2C.Core.Common;

/// <summary>
/// Machine-readable error representation adhering to BR-COM-007.
/// </summary>
public sealed record OperationError(string Code, string Message, string? Target = null);
