using System;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Workspace;
using C2C.Host;
using C2C.Infrastructure.Execution;

namespace C2C.IntegrationTests.Mcp;

public sealed class ExecutionMcpToolTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly string _tempStorage;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly WorkspaceContext _context;

    public ExecutionMcpToolTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_mcp_exec_ws_" + Guid.NewGuid().ToString("N"));
        _tempStorage = Path.Combine(Path.GetTempPath(), "c2c_mcp_exec_st_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);
        Directory.CreateDirectory(_tempStorage);

        _context = new WorkspaceContext(
            new WorkspaceId("ws_integration_exec"),
            _tempWorkspace,
            "Execution Test Workspace");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IWorkspaceContext>(_context);
                services.AddSingleton(new ExecutionOptions
                {
                    StorageDirectory = _tempStorage
                });
            });
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/event-stream");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();

        try
        {
            if (Directory.Exists(_tempWorkspace))
            {
                Directory.Delete(_tempWorkspace, recursive: true);
            }
            if (Directory.Exists(_tempStorage))
            {
                Directory.Delete(_tempStorage, recursive: true);
            }
        }
        catch
        {
            // Best effort
        }
    }

    [Fact]
    public async Task PostMcp_ExecutionTools_EndToEnd()
    {
        // 1. Record evidence using IExecutionRecorder
        using var scope = _factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IExecutionRecorder>();

        ExecutionRecordRequest recordReq = new()
        {
            WorkspaceId = _context.Id.Value,
            TaskId = "task-mcp-e2e",
            Iteration = 1,
            IdempotencyKey = "key-mcp-e2e",
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
            FinishedAt = DateTimeOffset.UtcNow,
            ExitStatus = 0,
            CommandCategory = "test",
            CommandText = "dotnet test",
            ChangedFiles = ["src/Service.cs"],
            TestSummary = new TestSummaryDto
            {
                Total = 15,
                Passed = 15,
                Failed = 0,
                Skipped = 0,
                Suites = 2
            },
            Artifacts =
            [
                new ArtifactRecordRequest
                {
                    ArtifactId = "test-log",
                    Name = "test-output.log",
                    ArtifactType = "log",
                    Content = "Passed: 15, Failed: 0\nAll suites completed.\n"
                },
                new ArtifactRecordRequest
                {
                    ArtifactId = "secret-log",
                    Name = "secret.log",
                    ArtifactType = "log",
                    Content = "Authorization: Bearer secrettoken1234567890abcdef\n"
                }
            ]
        };

        var recordResult = await recorder.RecordAsync(recordReq);
        Assert.True(recordResult.IsSuccess);
        string executionId = recordResult.Value!.ExecutionId;

        // 2. Call execution_summary over MCP HTTP
        var summaryReq = new
        {
            jsonrpc = "2.0",
            id = 10,
            method = "tools/call",
            @params = new
            {
                name = "execution_summary",
                arguments = new { executionId }
            }
        };

        var summaryResp = await _client.PostAsJsonAsync("/mcp", summaryReq);
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);
        var summaryBody = await summaryResp.Content.ReadAsStringAsync();
        var summaryDoc = ExtractJsonRpcResponse(summaryBody);

        Assert.True(summaryDoc.TryGetProperty("result", out var sumResult));
        Assert.True(sumResult.TryGetProperty("content", out var sumContent));
        var sumText = sumContent[0].GetProperty("text").GetString();
        Assert.NotNull(sumText);

        using var sumJson = JsonDocument.Parse(sumText);
        Assert.Equal(executionId, sumJson.RootElement.GetProperty("executionId").GetString());
        Assert.Equal("task-mcp-e2e", sumJson.RootElement.GetProperty("taskId").GetString());

        // 3. Call test_status over MCP HTTP
        var testStatusReq = new
        {
            jsonrpc = "2.0",
            id = 11,
            method = "tools/call",
            @params = new
            {
                name = "test_status",
                arguments = new { executionId }
            }
        };

        var testStatusResp = await _client.PostAsJsonAsync("/mcp", testStatusReq);
        Assert.Equal(HttpStatusCode.OK, testStatusResp.StatusCode);
        var testStatusBody = await testStatusResp.Content.ReadAsStringAsync();
        var testStatusDoc = ExtractJsonRpcResponse(testStatusBody);

        Assert.True(testStatusDoc.TryGetProperty("result", out var tsResult));
        Assert.True(tsResult.TryGetProperty("content", out var tsContent));
        var tsText = tsContent[0].GetProperty("text").GetString();
        Assert.NotNull(tsText);

        using var tsJson = JsonDocument.Parse(tsText);
        Assert.Equal("Passed", tsJson.RootElement.GetProperty("status").GetString());

        // 4. Call execution_output on Readable artifact over MCP HTTP
        var outputReq = new
        {
            jsonrpc = "2.0",
            id = 12,
            method = "tools/call",
            @params = new
            {
                name = "execution_output",
                arguments = new
                {
                    executionId,
                    artifactId = "test-log"
                }
            }
        };

        var outputResp = await _client.PostAsJsonAsync("/mcp", outputReq);
        Assert.Equal(HttpStatusCode.OK, outputResp.StatusCode);
        var outputBody = await outputResp.Content.ReadAsStringAsync();
        var outputDoc = ExtractJsonRpcResponse(outputBody);

        Assert.True(outputDoc.TryGetProperty("result", out var outResult));
        Assert.True(outResult.TryGetProperty("content", out var outContent));
        var outText = outContent[0].GetProperty("text").GetString();
        Assert.NotNull(outText);

        using var outJson = JsonDocument.Parse(outText);
        Assert.Equal("Readable", outJson.RootElement.GetProperty("classification").GetString());
        Assert.Contains("Passed: 15", outJson.RootElement.GetProperty("content").GetString());

        // 5. Call execution_output on Restricted artifact over MCP HTTP (BR-SEC-009, BR-EXE-005)
        var secretOutputReq = new
        {
            jsonrpc = "2.0",
            id = 13,
            method = "tools/call",
            @params = new
            {
                name = "execution_output",
                arguments = new
                {
                    executionId,
                    artifactId = "secret-log"
                }
            }
        };

        var secretOutputResp = await _client.PostAsJsonAsync("/mcp", secretOutputReq);
        Assert.Equal(HttpStatusCode.OK, secretOutputResp.StatusCode);
        var secretOutputBody = await secretOutputResp.Content.ReadAsStringAsync();
        var secretOutputDoc = ExtractJsonRpcResponse(secretOutputBody);

        Assert.True(secretOutputDoc.TryGetProperty("result", out var secResult));
        Assert.True(secResult.TryGetProperty("content", out var secContent));
        var secText = secContent[0].GetProperty("text").GetString();
        Assert.NotNull(secText);

        using var secJson = JsonDocument.Parse(secText);
        Assert.Equal("Restricted", secJson.RootElement.GetProperty("classification").GetString());
        Assert.Equal("SENSITIVE_CONTENT", secJson.RootElement.GetProperty("reasonCode").GetString());
        // CRITICAL INVARIANT: Body is NEVER returned remotely!
        Assert.Equal(JsonValueKind.Null, secJson.RootElement.GetProperty("content").ValueKind);
    }

    private static JsonElement ExtractJsonRpcResponse(string sseBody)
    {
        foreach (string line in sseBody.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                string json = trimmed[5..].Trim();
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
        }

        using var fallback = JsonDocument.Parse(sseBody);
        return fallback.RootElement.Clone();
    }
}
