using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Persisted configuration for a bound workspace. Adheres to BR-COM-009 and BR-COM-010.
/// </summary>
public sealed record WorkspaceConfig
{
    public int SchemaVersion { get; init; } = 1;
    public required WorkspaceId WorkspaceId { get; init; }
    public required string CanonicalRoot { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? Label { get; init; }
}
