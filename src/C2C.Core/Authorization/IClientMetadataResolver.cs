using System.Threading;
using System.Threading.Tasks;

namespace C2C.Core.Authorization;

/// <summary>
/// Result of client validation adhering to BR-AUTH-003 and 04-DETAILED-AUTH-DESIGN.md.
/// </summary>
public sealed record ClientMetadataResult
{
    public bool IsValid { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ClientId { get; init; }

    public string? ClientName { get; init; }

    public static ClientMetadataResult Valid(string clientId, string? clientName = null) =>
        new() { IsValid = true, ClientId = clientId, ClientName = clientName };

    public static ClientMetadataResult Invalid(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Domain contract for resolving and validating client registration/metadata adhering to BR-AUTH-003.
/// </summary>
public interface IClientMetadataResolver
{
    Task<ClientMetadataResult> ValidateClientAsync(
        string clientId,
        string? redirectUri = null,
        CancellationToken cancellationToken = default);
}
