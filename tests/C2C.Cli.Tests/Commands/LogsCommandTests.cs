using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Cli.Commands;
using C2C.Cli.Output;
using C2C.Core.Diagnostics;
using C2C.Infrastructure.Diagnostics;

namespace C2C.Cli.Tests.Commands;

public sealed class LogsCommandTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly JsonDiagnosticLogStore _store;

    public LogsCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "c2c_cli_logs_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _store = new JsonDiagnosticLogStore(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task LogsCommand_EmptyLogFile_ReturnsEmptyPageSuccessfully()
    {
        var command = new LogsCommand(_store);

        int exitCode = await command.ExecuteAsync(tail: 10, level: null, component: null, since: null, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);
    }

    [Fact]
    public async Task LogsCommand_ReadsBoundedTail_ReturnsCorrectCount()
    {
        // Write 15 log entries
        for (int i = 1; i <= 15; i++)
        {
            await _store.WriteAsync(new DiagnosticLogEntry
            {
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(i),
                Level = "info",
                Component = "test",
                Message = $"Log message {i}"
            });
        }

        var command = new LogsCommand(_store);
        int exitCode = await command.ExecuteAsync(tail: 5, level: null, component: null, since: null, json: true);

        Assert.Equal(CliExitCode.Success, exitCode);

        var page = await _store.ReadAsync(new DiagnosticLogRequest { Tail = 5 });
        Assert.Equal(5, page.Entries.Count);
        Assert.Equal("Log message 11", page.Entries[0].Message);
        Assert.Equal("Log message 15", page.Entries[4].Message);
    }

    [Fact]
    public async Task LogsCommand_RedactsSensitiveData_InLogStore()
    {
        await _store.WriteAsync(new DiagnosticLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = "info",
            Component = "auth",
            Message = "Client authorized with Bearer eyJhbGciOiJIUzI1NiIs... and code 2345-6789"
        });

        var page = await _store.ReadAsync(new DiagnosticLogRequest { Tail = 10 });
        Assert.Single(page.Entries);

        var entry = page.Entries[0];
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIs", entry.Message);
        Assert.DoesNotContain("2345-6789", entry.Message);
        Assert.Contains("Bearer [REDACTED]", entry.Message);
        Assert.Contains("[REDACTED_CODE]", entry.Message);

        // Verify disk content is also redacted (BR-COM-009)
        string logPath = Path.Combine(_tempRoot, "logs", "diagnostics.jsonl");
        string rawOnDisk = await File.ReadAllTextAsync(logPath);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiIs", rawOnDisk);
        Assert.DoesNotContain("2345-6789", rawOnDisk);
    }

    [Fact]
    public async Task LogsCommand_FiltersByLevelAndComponent()
    {
        await _store.WriteAsync(new DiagnosticLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = "info",
            Component = "bridge",
            Message = "Bridge started"
        });

        await _store.WriteAsync(new DiagnosticLogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = "error",
            Component = "tunnel",
            Message = "Tunnel failed"
        });

        var pageLevel = await _store.ReadAsync(new DiagnosticLogRequest { Level = "error" });
        Assert.Single(pageLevel.Entries);
        Assert.Equal("tunnel", pageLevel.Entries[0].Component);

        var pageComp = await _store.ReadAsync(new DiagnosticLogRequest { Component = "bridge" });
        Assert.Single(pageComp.Entries);
        Assert.Equal("bridge", pageComp.Entries[0].Component);
    }

    [Fact]
    public async Task LogsCommand_SkipsMalformedLinesSafely()
    {
        string logDir = Path.Combine(_tempRoot, "logs");
        Directory.CreateDirectory(logDir);
        string logPath = Path.Combine(logDir, "diagnostics.jsonl");

        await File.AppendAllLinesAsync(logPath, new[]
        {
            "{\"timestamp\":\"2026-01-01T00:00:00Z\",\"level\":\"info\",\"component\":\"core\",\"message\":\"valid line 1\"}",
            "corrupted line not valid json {{{{",
            "{\"timestamp\":\"2026-01-01T00:01:00Z\",\"level\":\"info\",\"component\":\"core\",\"message\":\"valid line 2\"}"
        });

        var page = await _store.ReadAsync(new DiagnosticLogRequest { Tail = 10 });
        Assert.Equal(2, page.Entries.Count);
        Assert.Equal("valid line 1", page.Entries[0].Message);
        Assert.Equal("valid line 2", page.Entries[1].Message);
    }

    [Fact]
    public async Task LogsCommand_WithInvalidSinceFormat_ReturnsInvalidUsage2()
    {
        var command = new LogsCommand(_store);

        int exitCode = await command.ExecuteAsync(tail: 10, level: null, component: null, since: "invalid-date", json: true);

        Assert.Equal(CliExitCode.InvalidUsageOrConfig, exitCode);
    }

    [Fact]
    public async Task LogsCommand_WhenCancelled_ReturnsTimeoutOrCancelled7()
    {
        var command = new LogsCommand(_store);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int exitCode = await command.ExecuteAsync(tail: 10, level: null, component: null, since: null, json: true, cancellationToken: cts.Token);

        Assert.Equal(CliExitCode.TimeoutOrCancelled, exitCode);
    }
}
