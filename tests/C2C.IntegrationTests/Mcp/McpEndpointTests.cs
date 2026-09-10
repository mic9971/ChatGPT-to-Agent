using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using C2C.Core.Common;
using C2C.Core.Workspace;
using C2C.Host;

namespace C2C.IntegrationTests.Mcp;

public sealed class McpEndpointTests : IDisposable
{
    private readonly string _tempWorkspace;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public McpEndpointTests()
    {
        _tempWorkspace = Path.Combine(Path.GetTempPath(), "c2c_mcp_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWorkspace);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override IWorkspaceContext to point to temporary test workspace
                services.AddSingleton<IWorkspaceContext>(new WorkspaceContext(
                    new WorkspaceId("ws_integration_test"),
                    _tempWorkspace,
                    "Integration Test Workspace"));
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
        }
        catch
        {
        }
    }

    [Fact]
    public async Task PostMcp_ListTools_ReturnsOnlyApprovedReadOnlyTools()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.TryGetProperty("tools", out var toolsElem));

        var toolNames = toolsElem.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        // 6 approved read-only tools in V0.2 (workspace + git evidence)
        Assert.Contains("workspace_info", toolNames);
        Assert.Contains("read_file", toolNames);
        Assert.Contains("list_directory", toolNames);
        Assert.Contains("search_workspace", toolNames);
        Assert.Contains("git_status", toolNames);
        Assert.Contains("git_diff", toolNames);
        Assert.Equal(6, toolNames.Count);

        // BR-COM-002: Zero write or shell execution tools
        Assert.DoesNotContain("write_file", toolNames);
        Assert.DoesNotContain("delete_file", toolNames);
        Assert.DoesNotContain("execute_command", toolNames);
        Assert.DoesNotContain("git_commit", toolNames);
    }

    [Fact]
    public async Task PostMcp_CallWorkspaceInfo_ReturnsSafeMetadataWithoutLocalPaths()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "workspace_info",
                arguments = new { }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.TryGetProperty("content", out var contentElem));

        var textContent = contentElem[0].GetProperty("text").GetString();
        Assert.NotNull(textContent);

        using var infoDoc = JsonDocument.Parse(textContent);
        var info = infoDoc.RootElement;

        Assert.Equal("ws_integration_test", info.GetProperty("workspaceId").GetString());
        Assert.Equal("Integration Test Workspace", info.GetProperty("label").GetString());
        Assert.True(info.GetProperty("readOnly").GetBoolean());
        Assert.Equal("0.1.0", info.GetProperty("bridgeVersion").GetString());

        // BR-COM-007 / BR-SEC-006: Absolute path must NOT leak
        Assert.DoesNotContain(_tempWorkspace, textContent);
    }

    [Fact]
    public async Task PostMcp_CallReadFile_AllowedFile_ReturnsTextChunk()
    {
        string filePath = Path.Combine(_tempWorkspace, "allowed.txt");
        await File.WriteAllTextAsync(filePath, "Line 1: Hello from MCP\nLine 2: World");

        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "read_file",
                arguments = new
                {
                    path = "allowed.txt"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.False(resultElem.GetProperty("isError").GetBoolean());

        var textContent = resultElem.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.NotNull(textContent);
        Assert.Contains("Hello from MCP", textContent);
    }

    [Fact]
    public async Task PostMcp_CallReadFile_SensitiveEnv_ReturnsError()
    {
        string envFile = Path.Combine(_tempWorkspace, ".env");
        await File.WriteAllTextAsync(envFile, "DATABASE_PASSWORD=secret");

        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new
            {
                name = "read_file",
                arguments = new
                {
                    path = ".env"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.GetProperty("isError").GetBoolean());

        var textContent = resultElem.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.NotNull(textContent);
        Assert.Contains(CommonErrorCodes.SensitiveContentDenied, textContent);
        Assert.DoesNotContain("DATABASE_PASSWORD", textContent);
    }

    [Fact]
    public async Task PostMcp_CallReadFile_DirectoryTraversal_ReturnsOutsideRootError()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 5,
            method = "tools/call",
            @params = new
            {
                name = "read_file",
                arguments = new
                {
                    path = "../../outside.txt"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.GetProperty("isError").GetBoolean());

        var textContent = resultElem.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.NotNull(textContent);
        Assert.Contains(CommonErrorCodes.WorkspacePathOutsideRoot, textContent);
    }

    [Fact]
    public async Task PostMcp_CallUnknownTool_ReturnsError()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 6,
            method = "tools/call",
            @params = new
            {
                name = "malicious_write_tool",
                arguments = new { }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.GetProperty("isError").GetBoolean());
    }

    [Fact]
    public async Task PostMcp_CallListDirectory_ReturnsFilteredDirectoryEntries()
    {
        // Safe files
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "mcp_list_file.txt"), "listable content");
        Directory.CreateDirectory(Path.Combine(_tempWorkspace, "mcp_sub"));

        // Sensitive & ignored files
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, ".env"), "SECRET=mcp");
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "secret.key"), "PRIVATE KEY");

        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 10,
            method = "tools/call",
            @params = new
            {
                name = "list_directory",
                arguments = new
                {
                    path = "."
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.False(resultElem.GetProperty("isError").GetBoolean());

        var contentArray = resultElem.GetProperty("content").EnumerateArray().ToList();
        Assert.NotEmpty(contentArray);
        var textPayload = contentArray[0].GetProperty("text").GetString();
        Assert.NotNull(textPayload);

        using var pageDoc = JsonDocument.Parse(textPayload);
        var items = pageDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var relativePaths = items.Select(i => i.GetProperty("relativePath").GetString()).ToList();
        Assert.Contains("mcp_list_file.txt", relativePaths);
        Assert.Contains("mcp_sub", relativePaths);

        // Pre-disclosure filtering verification: sensitive items never leak
        Assert.DoesNotContain(".env", relativePaths);
        Assert.DoesNotContain("secret.key", relativePaths);
    }

    [Fact]
    public async Task PostMcp_CallListDirectory_Pagination_ReturnsNextCursorAndFollowUp()
    {
        string pagedDir = Path.Combine(_tempWorkspace, "paged_dir");
        Directory.CreateDirectory(pagedDir);
        for (int i = 1; i <= 8; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(pagedDir, $"p_{i:D2}.txt"), $"item {i}");
        }

        // Page 1: limit 4
        var page1Payload = new
        {
            jsonrpc = "2.0",
            id = 11,
            method = "tools/call",
            @params = new
            {
                name = "list_directory",
                arguments = new
                {
                    path = "paged_dir",
                    limit = 4
                }
            }
        };

        var response1 = await _client.PostAsJsonAsync("/mcp", page1Payload);
        var body1 = await response1.Content.ReadAsStringAsync();
        var root1 = ExtractJsonRpcResponse(body1);

        var text1 = root1.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        using var doc1 = JsonDocument.Parse(text1);
        var items1 = doc1.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(4, items1.Count);

        string? nextCursor = doc1.RootElement.GetProperty("nextCursor").GetString();
        Assert.NotNull(nextCursor);

        // Page 2: limit 4 with nextCursor
        var page2Payload = new
        {
            jsonrpc = "2.0",
            id = 12,
            method = "tools/call",
            @params = new
            {
                name = "list_directory",
                arguments = new
                {
                    path = "paged_dir",
                    cursor = nextCursor,
                    limit = 4
                }
            }
        };

        var response2 = await _client.PostAsJsonAsync("/mcp", page2Payload);
        var body2 = await response2.Content.ReadAsStringAsync();
        var root2 = ExtractJsonRpcResponse(body2);

        var text2 = root2.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        using var doc2 = JsonDocument.Parse(text2);
        var items2 = doc2.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(4, items2.Count);

        // Disjoint sets of items
        var names1 = items1.Select(i => i.GetProperty("name").GetString()).ToHashSet();
        var names2 = items2.Select(i => i.GetProperty("name").GetString()).ToHashSet();
        Assert.Empty(names1.Intersect(names2));
    }

    [Fact]
    public async Task PostMcp_CallListDirectory_Traversal_FailsWithOutsideRoot()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 13,
            method = "tools/call",
            @params = new
            {
                name = "list_directory",
                arguments = new
                {
                    path = "../outside"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.GetProperty("isError").GetBoolean());

        var text = resultElem.GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Contains(CommonErrorCodes.WorkspacePathOutsideRoot, text);
    }

    [Fact]
    public async Task PostMcp_CallSearchWorkspace_ReturnsMatchesWithoutLeakingSensitiveFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, "mcp_search_target.txt"), "first line\nmatch_mcp_query text\nthird line");
        await File.WriteAllTextAsync(Path.Combine(_tempWorkspace, ".env"), "SECRET=match_mcp_query");

        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 20,
            method = "tools/call",
            @params = new
            {
                name = "search_workspace",
                arguments = new
                {
                    query = "match_mcp_query"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.False(resultElem.GetProperty("isError").GetBoolean());

        var text = resultElem.GetProperty("content")[0].GetProperty("text").GetString()!;
        using var doc = JsonDocument.Parse(text);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.Single(items);
        Assert.Equal("mcp_search_target.txt", items[0].GetProperty("relativePath").GetString());
        Assert.Equal(2, items[0].GetProperty("lineNumber").GetInt32());
    }

    [Fact]
    public async Task PostMcp_CallSearchWorkspace_Pagination_ReturnsNextCursor()
    {
        List<string> lines = [];
        for (int i = 1; i <= 6; i++)
        {
            lines.Add($"search_paged_token {i}");
        }
        await File.WriteAllLinesAsync(Path.Combine(_tempWorkspace, "paged_search.txt"), lines);

        // Page 1: limit 3
        var page1Payload = new
        {
            jsonrpc = "2.0",
            id = 21,
            method = "tools/call",
            @params = new
            {
                name = "search_workspace",
                arguments = new
                {
                    query = "search_paged_token",
                    limit = 3
                }
            }
        };

        var response1 = await _client.PostAsJsonAsync("/mcp", page1Payload);
        var body1 = await response1.Content.ReadAsStringAsync();
        var root1 = ExtractJsonRpcResponse(body1);

        var text1 = root1.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        using var doc1 = JsonDocument.Parse(text1);
        var items1 = doc1.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items1.Count);

        string? nextCursor = doc1.RootElement.GetProperty("nextCursor").GetString();
        Assert.NotNull(nextCursor);

        // Page 2: with cursor
        var page2Payload = new
        {
            jsonrpc = "2.0",
            id = 22,
            method = "tools/call",
            @params = new
            {
                name = "search_workspace",
                arguments = new
                {
                    query = "search_paged_token",
                    cursor = nextCursor,
                    limit = 3
                }
            }
        };

        var response2 = await _client.PostAsJsonAsync("/mcp", page2Payload);
        var body2 = await response2.Content.ReadAsStringAsync();
        var root2 = ExtractJsonRpcResponse(body2);

        var text2 = root2.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        using var doc2 = JsonDocument.Parse(text2);
        var items2 = doc2.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items2.Count);
        Assert.Null(doc2.RootElement.GetProperty("nextCursor").GetString());
    }

    [Fact]
    public async Task PostMcp_CallSearchWorkspace_TraversalScope_FailsWithOutsideRoot()
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = 23,
            method = "tools/call",
            @params = new
            {
                name = "search_workspace",
                arguments = new
                {
                    query = "anything",
                    pathScope = "../escape"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/mcp", requestPayload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var root = ExtractJsonRpcResponse(body);

        Assert.True(root.TryGetProperty("result", out var resultElem));
        Assert.True(resultElem.GetProperty("isError").GetBoolean());

        var text = resultElem.GetProperty("content")[0].GetProperty("text").GetString()!;
        Assert.Contains(CommonErrorCodes.WorkspacePathOutsideRoot, text);
    }

    [Theory]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://127.0.0.1:5001;http://localhost:5000")]
    [InlineData("http://[::1]:5000")]
    public void ValidateBindingAddresses_Loopback_Succeeds(string urls)
    {
        // Should not throw
        Program.ValidateBindingAddresses(urls);
    }

    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://*:5000")]
    [InlineData("http://+:5000")]
    [InlineData("http://127.0.0.1:5000;http://0.0.0.0:5001")]
    public void ValidateBindingAddresses_Wildcard_ThrowsInvalidOperationException(string urls)
    {
        Assert.Throws<InvalidOperationException>(() => Program.ValidateBindingAddresses(urls));
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
