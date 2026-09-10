using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Authorization;

namespace C2C.Infrastructure.Authorization;

/// <summary>
/// Client identifier metadata document (CIMD) resolver and pre-registered client validator
/// adhering to BR-AUTH-003, BR-SEC-006, and 02-CURRENT-SPEC-AND-INTEROP.md.
/// Defends against SSRF, internal network scanning, and loopback escapes.
/// </summary>
public sealed class CimdClientMetadataResolver : IClientMetadataResolver
{
    private readonly HashSet<string> _preRegisteredClients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _preRegisteredRedirectUris = new(StringComparer.Ordinal);

    public CimdClientMetadataResolver(
        IEnumerable<string>? preRegisteredClients = null,
        IDictionary<string, string>? preRegisteredRedirectUris = null)
    {
        if (preRegisteredClients != null)
        {
            foreach (string client in preRegisteredClients)
            {
                _preRegisteredClients.Add(client);
            }
        }

        if (preRegisteredRedirectUris != null)
        {
            foreach (var kvp in preRegisteredRedirectUris)
            {
                _preRegisteredRedirectUris[kvp.Key] = kvp.Value;
            }
        }
    }

    public void RegisterClient(string clientId, string redirectUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        _preRegisteredClients.Add(clientId);
        _preRegisteredRedirectUris[clientId] = redirectUri;
    }

    public Task<ClientMetadataResult> ValidateClientAsync(
        string clientId,
        string? redirectUri = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult(ClientMetadataResult.Invalid("Client ID cannot be empty."));
        }

        // 1. Check pre-registered clients first
        if (_preRegisteredClients.Contains(clientId))
        {
            if (!string.IsNullOrEmpty(redirectUri) &&
                _preRegisteredRedirectUris.TryGetValue(clientId, out string? expectedRedirect))
            {
                if (!string.Equals(redirectUri, expectedRedirect, StringComparison.Ordinal))
                {
                    return Task.FromResult(ClientMetadataResult.Invalid(
                        $"Redirect URI '{redirectUri}' does not match registered URI for client '{clientId}'."));
                }
            }

            return Task.FromResult(ClientMetadataResult.Valid(clientId, "Pre-registered Client"));
        }

        // 2. Validate CIMD client_id as a URI
        if (!Uri.TryCreate(clientId, UriKind.Absolute, out Uri? clientUri))
        {
            return Task.FromResult(ClientMetadataResult.Invalid(
                $"Client ID '{clientId}' is neither a recognized pre-registered client nor a valid absolute CIMD URI."));
        }

        // Must be HTTPS for CIMD
        if (!string.Equals(clientUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ClientMetadataResult.Invalid("CIMD client identifier URI must use HTTPS scheme."));
        }

        // Defend against SSRF
        string host = clientUri.Host;
        if (IsForbiddenHost(host))
        {
            return Task.FromResult(ClientMetadataResult.Invalid($"SSRF defense: CIMD client host '{host}' is forbidden."));
        }

        // If redirect URI is supplied, validate scheme and authority match or allow HTTPS redirect
        if (!string.IsNullOrEmpty(redirectUri))
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? parsedRedirect))
            {
                return Task.FromResult(ClientMetadataResult.Invalid("Redirect URI is not a valid absolute URI."));
            }

            if (!string.Equals(parsedRedirect.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parsedRedirect.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parsedRedirect.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(ClientMetadataResult.Invalid("Redirect URI must use HTTPS or loopback."));
            }
        }

        return Task.FromResult(ClientMetadataResult.Valid(clientId, host));
    }

    private static bool IsForbiddenHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
            string.Equals(host, "::1", StringComparison.Ordinal))
        {
            return true;
        }

        if (IPAddress.TryParse(host, out IPAddress? ip))
        {
            if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
            {
                return true;
            }

            byte[] bytes = ip.GetAddressBytes();
            if (bytes.Length == 4)
            {
                // RFC 1918 private IPv4
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;

                // Link-local / AWS metadata
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }

            return true; // Direct IP addresses are rejected for CIMD
        }

        return false;
    }
}
