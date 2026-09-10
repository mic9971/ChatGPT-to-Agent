using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using C2C.Core.Execution;

namespace C2C.Infrastructure.Execution;

/// <summary>
/// Local, deterministic execution artifact sanitizer adhering to BR-SEC-009, BR-SEC-012, BR-EXE-004, and 08-EXECUTION-REVIEW.md.
/// </summary>
public sealed class ArtifactSanitizer : IArtifactSanitizer
{
    private static readonly Regex BearerTokenRegex = new(
        @"(?:Bearer\s+[A-Za-z0-9\-\._~\+\/]{16,}=*|sk-[A-Za-z0-9]{20,}|(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{36})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PrivateKeyRegex = new(
        @"-----BEGIN (?:[A-Z0-9_-]+\s+)?PRIVATE KEY-----",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PairingCodeRegex = new(
        @"\bC2C-[A-Z0-9]{4}-[A-Z0-9]{4}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GenericHomePathRegex = new(
        @"(?:/Users/[a-zA-Z0-9_.-]+|/home/[a-zA-Z0-9_.-]+|[a-zA-Z]:\\Users\\[a-zA-Z0-9_.-]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ExecutionOptions _options;
    private readonly string? _userProfilePath;

    public ArtifactSanitizer(ExecutionOptions? options = null)
    {
        _options = options ?? new ExecutionOptions();
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
        {
            _userProfilePath = profile;
        }
    }

    public SanitizedArtifactResult Sanitize(string artifactName, string rawContent)
    {
        ArgumentNullException.ThrowIfNull(artifactName);

        if (rawContent == null)
        {
            return new SanitizedArtifactResult
            {
                Classification = ArtifactClassification.Missing,
                SanitizedContent = string.Empty,
                SizeBytes = 0,
                LineCount = 0,
                ReasonCode = "MISSING_CONTENT"
            };
        }

        byte[] rawBytes = Encoding.UTF8.GetBytes(rawContent);
        long sizeBytes = rawBytes.Length;
        int lineCount = CountLines(rawContent);
        string sha256Fingerprint = ComputeSha256(rawBytes);

        // 1. Check size limits
        if (sizeBytes > _options.MaxArtifactBytes)
        {
            return new SanitizedArtifactResult
            {
                Classification = ArtifactClassification.Restricted,
                SanitizedContent = string.Empty,
                SizeBytes = sizeBytes,
                LineCount = lineCount,
                ReasonCode = "OUTPUT_LIMIT_EXCEEDED",
                Sha256Fingerprint = sha256Fingerprint
            };
        }

        // 2. Binary / Non-UTF8 content check (null bytes)
        if (rawContent.Contains('\0'))
        {
            return new SanitizedArtifactResult
            {
                Classification = ArtifactClassification.Restricted,
                SanitizedContent = string.Empty,
                SizeBytes = sizeBytes,
                LineCount = lineCount,
                ReasonCode = "BINARY_CONTENT",
                Sha256Fingerprint = sha256Fingerprint
            };
        }

        // 3. Secret checks: private keys, bearer/API tokens, pairing codes
        if (PrivateKeyRegex.IsMatch(rawContent) ||
            BearerTokenRegex.IsMatch(rawContent) ||
            PairingCodeRegex.IsMatch(rawContent))
        {
            return new SanitizedArtifactResult
            {
                Classification = ArtifactClassification.Restricted,
                SanitizedContent = string.Empty,
                SizeBytes = sizeBytes,
                LineCount = lineCount,
                ReasonCode = "SENSITIVE_CONTENT",
                Sha256Fingerprint = sha256Fingerprint
            };
        }

        // 4. Safe redaction of home directory paths (BR-SEC-012)
        string sanitized = rawContent;
        if (!string.IsNullOrEmpty(_userProfilePath))
        {
            sanitized = sanitized.Replace(_userProfilePath, "~", StringComparison.Ordinal);
        }
        sanitized = GenericHomePathRegex.Replace(sanitized, "~");

        byte[] sanitizedBytes = Encoding.UTF8.GetBytes(sanitized);

        return new SanitizedArtifactResult
        {
            Classification = ArtifactClassification.Readable,
            SanitizedContent = sanitized,
            SizeBytes = sanitizedBytes.Length,
            LineCount = CountLines(sanitized),
            ReasonCode = null,
            Sha256Fingerprint = sha256Fingerprint
        };
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }
        return count;
    }

    private static string ComputeSha256(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
