using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using C2C.Core.Diagnostics;

namespace C2C.Infrastructure.Diagnostics;

/// <summary>
/// Persistent structured diagnostic log sink and bounded reader adhering to UC-CLI-07, BR-COM-009, and 08-UC-CLI-07-EVIDENCE-AND-LOGS.md.
/// Strictly bounded to ~/.c2c/logs/diagnostics.jsonl; enforces double-defense redaction before persistence and on output.
/// </summary>
public sealed class JsonDiagnosticLogStore : IDiagnosticLogReader, IDiagnosticLogSink
{
    private const int MaxTailBound = 500;
    private const int DefaultTail = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly Regex BearerTokenRegex = new(@"Bearer\s+[A-Za-z0-9\-_.~+/]+=*", RegexOptions.Compiled);
    private static readonly Regex PairingCodeRegex = new(@"\b[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}\b", RegexOptions.Compiled);
    private static readonly Regex PrivateKeyRegex = new(@"-----BEGIN [A-Z ]+PRIVATE KEY-----[\s\S]*?-----END [A-Z ]+PRIVATE KEY-----", RegexOptions.Compiled);
    private static readonly Regex SecretPropertyRegex = new(@"(refresh_token|client_secret|password|access_token)""?\s*[:=]\s*""?([^"",\s]+)""?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string _logFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonDiagnosticLogStore(string? storageDirectory = null)
    {
        string rootDir = !string.IsNullOrWhiteSpace(storageDirectory)
            ? Path.GetFullPath(storageDirectory)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".c2c");

        _logFilePath = Path.Combine(rootDir, "logs", "diagnostics.jsonl");
    }

    public async Task WriteAsync(
        DiagnosticLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Pre-persistence redaction (BR-COM-009)
        var sanitized = new DiagnosticLogEntry
        {
            Timestamp = entry.Timestamp,
            Level = entry.Level.ToLowerInvariant().Trim(),
            Component = entry.Component.ToLowerInvariant().Trim(),
            Message = RedactSensitiveContent(entry.Message),
            ErrorCode = entry.ErrorCode != null ? RedactSensitiveContent(entry.ErrorCode) : null
        };

        string jsonLine = JsonSerializer.Serialize(sanitized, JsonOptions);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string? dir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.AppendAllLinesAsync(_logFilePath, new[] { jsonLine }, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<DiagnosticLogPage> ReadAsync(
        DiagnosticLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int boundTail = Math.Clamp(request.Tail ?? DefaultTail, 1, MaxTailBound);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_logFilePath))
            {
                return new DiagnosticLogPage { Entries = Array.Empty<DiagnosticLogEntry>() };
            }

            var lines = await File.ReadAllLinesAsync(_logFilePath, cancellationToken);
            var parsed = new List<DiagnosticLogEntry>();

            // Read backwards from end for tail queries
            for (int i = lines.Length - 1; i >= 0 && parsed.Count < boundTail; i--)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var entry = JsonSerializer.Deserialize<DiagnosticLogEntry>(line, JsonOptions);
                    if (entry == null)
                    {
                        continue;
                    }

                    // Apply filters
                    if (!string.IsNullOrWhiteSpace(request.Level) &&
                        !string.Equals(entry.Level, request.Level, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(request.Component) &&
                        !string.Equals(entry.Component, request.Component, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (request.Since.HasValue && entry.Timestamp < request.Since.Value)
                    {
                        continue;
                    }

                    // Secondary sanitization pass on output (Double-defense per 08-UC-CLI-07)
                    parsed.Add(new DiagnosticLogEntry
                    {
                        Timestamp = entry.Timestamp,
                        Level = entry.Level,
                        Component = entry.Component,
                        Message = RedactSensitiveContent(entry.Message),
                        ErrorCode = entry.ErrorCode != null ? RedactSensitiveContent(entry.ErrorCode) : null
                    });
                }
                catch (JsonException)
                {
                    // Malformed lines are skipped safely without crashing reader
                }
            }

            parsed.Reverse();
            return new DiagnosticLogPage { Entries = parsed };
        }
        finally
        {
            _lock.Release();
        }
    }

    public static string RedactSensitiveContent(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        string sanitized = input;
        sanitized = PrivateKeyRegex.Replace(sanitized, "[REDACTED_PRIVATE_KEY]");
        sanitized = BearerTokenRegex.Replace(sanitized, "Bearer [REDACTED]");
        sanitized = PairingCodeRegex.Replace(sanitized, "[REDACTED_CODE]");
        sanitized = SecretPropertyRegex.Replace(sanitized, "$1=[REDACTED]");

        return sanitized;
    }
}
