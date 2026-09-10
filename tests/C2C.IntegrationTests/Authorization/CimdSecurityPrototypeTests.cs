using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace C2C.IntegrationTests.Authorization;

public sealed class CimdSecurityPrototypeTests
{
    [Theory]
    [InlineData("http://client.example.com/oauth/client.json", "HTTPS only required")]
    [InlineData("https://client.example.com/", "Non-root path required")]
    [InlineData("https://user:pass@client.example.com/oauth/client.json", "Userinfo rejected")]
    [InlineData("https://client.example.com/oauth/client.json#section", "Fragment rejected")]
    public void ValidateCimdUrl_InvalidUrlFormats_AreRejected(string url, string expectedReason)
    {
        var result = CimdUrlValidator.ValidateUrl(url);
        Assert.False(result.IsValid, $"Expected invalid for: {expectedReason}");
    }

    [Theory]
    [InlineData("https://client.example.com/oauth/client.json")]
    [InlineData("https://auth.myagent.ai/.well-known/oauth-client.json")]
    public void ValidateCimdUrl_ValidHttpsNonRootUrl_PassesSyntaxGate(string url)
    {
        var result = CimdUrlValidator.ValidateUrl(url);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // AWS metadata / link-local
    public void IsPrivateOrLoopbackIp_BlocksSsrfTargets(string ipString)
    {
        IPAddress ip = IPAddress.Parse(ipString);
        bool isBlocked = CimdUrlValidator.IsPrivateOrRestrictedAddress(ip);
        Assert.True(isBlocked, $"IP {ipString} must be blocked to prevent SSRF.");
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    public void IsPrivateOrLoopbackIp_AllowsPublicIps(string ipString)
    {
        IPAddress ip = IPAddress.Parse(ipString);
        bool isBlocked = CimdUrlValidator.IsPrivateOrRestrictedAddress(ip);
        Assert.False(isBlocked, $"Public IP {ipString} should be permitted.");
    }

    [Fact]
    public void ParseCimdDocument_MismatchedClientId_IsRejected()
    {
        string documentJson = JsonSerializer.Serialize(new
        {
            client_id = "https://different.example.com/client.json",
            client_name = "Test Client",
            redirect_uris = new[] { "https://client.example.com/callback" }
        });

        var parseResult = CimdDocumentParser.ParseAndValidate(
            documentJson,
            expectedClientId: "https://client.example.com/oauth/client.json");

        Assert.False(parseResult.IsValid);
        Assert.Contains("client_id must match metadata URL", parseResult.ErrorMessage);
    }

    [Fact]
    public void ParseCimdDocument_MatchingClientIdAndRedirectUris_IsValid()
    {
        string expectedId = "https://client.example.com/oauth/client.json";
        string documentJson = JsonSerializer.Serialize(new
        {
            client_id = expectedId,
            client_name = "Valid MCP Client",
            redirect_uris = new[] { "https://client.example.com/callback" }
        });

        var parseResult = CimdDocumentParser.ParseAndValidate(documentJson, expectedId);

        Assert.True(parseResult.IsValid);
        Assert.NotNull(parseResult.Document);
        Assert.Equal("Valid MCP Client", parseResult.Document.ClientName);
    }

    // Prototype implementation of CIMD URL validator for spike proof
    private static class CimdUrlValidator
    {
        public static (bool IsValid, string? Error) ValidateUrl(string urlString)
        {
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out Uri? uri))
            {
                return (false, "Malformed URI");
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return (false, "HTTPS only required");
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                return (false, "Userinfo rejected");
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                return (false, "Fragment rejected");
            }

            if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
            {
                return (false, "Non-root path required");
            }

            return (true, null);
        }

        public static bool IsPrivateOrRestrictedAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                {
                    return true;
                }

                if (address.IsIPv4MappedToIPv6)
                {
                    address = address.MapToIPv4();
                }
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] bytes = address.GetAddressBytes();

                // 10.0.0.0/8
                if (bytes[0] == 10) return true;

                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;

                // 169.254.0.0/16 (Link-local / Cloud metadata)
                if (bytes[0] == 169 && bytes[1] == 254) return true;

                // 127.0.0.0/8
                if (bytes[0] == 127) return true;

                // 0.0.0.0/8
                if (bytes[0] == 0) return true;
            }

            return false;
        }
    }

    private sealed class CimdMetadata
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = new();
    }

    private static class CimdDocumentParser
    {
        public static (bool IsValid, CimdMetadata? Document, string? ErrorMessage) ParseAndValidate(
            string json,
            string expectedClientId)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("client_id", out var clientIdProp) ||
                    clientIdProp.GetString() != expectedClientId)
                {
                    return (false, null, "client_id must match metadata URL exactly.");
                }

                var metadata = new CimdMetadata
                {
                    ClientId = expectedClientId,
                    ClientName = root.TryGetProperty("client_name", out var nameProp) ? nameProp.GetString() ?? "" : ""
                };

                if (root.TryGetProperty("redirect_uris", out var redirectsProp) && redirectsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in redirectsProp.EnumerateArray())
                    {
                        string? uri = elem.GetString();
                        if (!string.IsNullOrEmpty(uri))
                        {
                            metadata.RedirectUris.Add(uri);
                        }
                    }
                }

                if (metadata.RedirectUris.Count == 0)
                {
                    return (false, null, "At least one redirect_uri required.");
                }

                return (true, metadata, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"JSON parse error: {ex.Message}");
            }
        }
    }
}
