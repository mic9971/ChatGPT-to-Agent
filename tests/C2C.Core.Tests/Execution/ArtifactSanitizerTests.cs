using System;

using Xunit;

using C2C.Core.Execution;
using C2C.Infrastructure.Execution;

namespace C2C.Core.Tests.Execution;

public sealed class ArtifactSanitizerTests
{
    private readonly ArtifactSanitizer _sanitizer;

    public ArtifactSanitizerTests()
    {
        _sanitizer = new ArtifactSanitizer(new ExecutionOptions
        {
            MaxArtifactBytes = 10_000
        });
    }

    [Fact]
    public void Sanitize_CleanText_ReturnsReadable()
    {
        string text = "Build succeeded.\n0 Warning(s)\n0 Error(s)\n";
        SanitizedArtifactResult result = _sanitizer.Sanitize("build.log", text);

        Assert.Equal(ArtifactClassification.Readable, result.Classification);
        Assert.Null(result.ReasonCode);
        Assert.Equal(text, result.SanitizedContent);
        Assert.Equal(4, result.LineCount);
        Assert.False(string.IsNullOrEmpty(result.Sha256Fingerprint));
    }

    [Fact]
    public void Sanitize_BearerToken_ReturnsRestricted()
    {
        string text = "Request header: Bearer abcdef1234567890abcdef1234567890\nDone.";
        SanitizedArtifactResult result = _sanitizer.Sanitize("request.log", text);

        Assert.Equal(ArtifactClassification.Restricted, result.Classification);
        Assert.Equal("SENSITIVE_CONTENT", result.ReasonCode);
        Assert.Empty(result.SanitizedContent); // Body never stored in sanitized output
    }

    [Fact]
    public void Sanitize_PrivateKeyBlock_ReturnsRestricted()
    {
        string text = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA...\n-----END RSA PRIVATE KEY-----\n";
        SanitizedArtifactResult result = _sanitizer.Sanitize("id_rsa", text);

        Assert.Equal(ArtifactClassification.Restricted, result.Classification);
        Assert.Equal("SENSITIVE_CONTENT", result.ReasonCode);
        Assert.Empty(result.SanitizedContent);
    }

    [Fact]
    public void Sanitize_PairingCode_ReturnsRestricted()
    {
        string text = "Pairing code generated: C2C-AB12-CD34 for user.";
        SanitizedArtifactResult result = _sanitizer.Sanitize("auth.log", text);

        Assert.Equal(ArtifactClassification.Restricted, result.Classification);
        Assert.Equal("SENSITIVE_CONTENT", result.ReasonCode);
        Assert.Empty(result.SanitizedContent);
    }

    [Fact]
    public void Sanitize_BinaryNullBytes_ReturnsRestricted()
    {
        string text = "ELF\0\0\0\0executable";
        SanitizedArtifactResult result = _sanitizer.Sanitize("binary.bin", text);

        Assert.Equal(ArtifactClassification.Restricted, result.Classification);
        Assert.Equal("BINARY_CONTENT", result.ReasonCode);
        Assert.Empty(result.SanitizedContent);
    }

    [Fact]
    public void Sanitize_OversizedContent_ReturnsRestricted()
    {
        string bigText = new('a', 15_000);
        SanitizedArtifactResult result = _sanitizer.Sanitize("huge.log", bigText);

        Assert.Equal(ArtifactClassification.Restricted, result.Classification);
        Assert.Equal("OUTPUT_LIMIT_EXCEEDED", result.ReasonCode);
        Assert.Empty(result.SanitizedContent);
    }

    [Fact]
    public void Sanitize_HomePaths_RedactedToTilde()
    {
        string text = "Error in /Users/johndoe/project/file.cs at line 42";
        SanitizedArtifactResult result = _sanitizer.Sanitize("error.log", text);

        Assert.Equal(ArtifactClassification.Readable, result.Classification);
        Assert.DoesNotContain("/Users/johndoe", result.SanitizedContent);
        Assert.Contains("~/project/file.cs", result.SanitizedContent);
    }
}
