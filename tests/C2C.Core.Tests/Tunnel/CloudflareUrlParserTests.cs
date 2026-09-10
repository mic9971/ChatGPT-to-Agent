using System;

using Xunit;

using C2C.Infrastructure.Tunnel.Cloudflare;

namespace C2C.Core.Tests.Tunnel;

public sealed class CloudflareUrlParserTests
{
    [Fact]
    public void TryParseUrl_ValidBanner_ReturnsHttpsUri()
    {
        string logLine = "2026-09-10T07:15:30Z INF |  https://orange-apple-banana.trycloudflare.com  |";
        Uri? result = CloudflareUrlParser.TryParseUrl(logLine);

        Assert.NotNull(result);
        Assert.Equal("https", result.Scheme);
        Assert.Equal("orange-apple-banana.trycloudflare.com", result.Host);
    }

    [Fact]
    public void TryParseUrl_WithAnsiEscapeCodes_ReturnsCleanHttpsUri()
    {
        string logLine = "\u001b[32m2026-09-10T07:15:30Z INF\u001b[0m | \u001b[1mhttps://quick-tunnel-42.trycloudflare.com\u001b[0m |";
        Uri? result = CloudflareUrlParser.TryParseUrl(logLine);

        Assert.NotNull(result);
        Assert.Equal("https", result.Scheme);
        Assert.Equal("quick-tunnel-42.trycloudflare.com", result.Host);
    }

    [Fact]
    public void TryParseUrl_NonHttps_ReturnsNull()
    {
        string logLine = "Visit http://my-insecure.trycloudflare.com for details";
        Uri? result = CloudflareUrlParser.TryParseUrl(logLine);

        Assert.Null(result);
    }

    [Fact]
    public void TryParseUrl_ExternalDomain_ReturnsNull()
    {
        string logLine = "Check out https://attacker-controlled.com or https://evil.com/trycloudflare.com";
        Uri? result = CloudflareUrlParser.TryParseUrl(logLine);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2026-09-10T07:15:30Z INF Registered tunnel connection connIndex=0")]
    public void TryParseUrl_UnrelatedLines_ReturnsNull(string line)
    {
        Uri? result = CloudflareUrlParser.TryParseUrl(line);
        Assert.Null(result);
    }
}
