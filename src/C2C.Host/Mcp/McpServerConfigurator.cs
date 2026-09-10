using System.Text.Json;
using C2C.Core.Common;
using C2C.Core.Workspace;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace C2C.Host.Mcp;

/// <summary>
/// Configures official C# MCP SDK HTTP transport and read-only tools adhering to UC-MCP-01, UC-WS-02, and UC-WS-04.
/// </summary>
public static class McpServerConfigurator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonElement WorkspaceInfoSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {}
    }
    """).RootElement;

    private static readonly JsonElement ReadFileSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "Relative path to file in workspace." },
        "startLine": { "type": "integer", "description": "1-based start line number." },
        "maxLines": { "type": "integer", "description": "Maximum lines to read." },
        "startByte": { "type": "integer", "description": "0-based start byte offset." },
        "maxBytes": { "type": "integer", "description": "Maximum bytes to read." },
        "expectedFingerprint": { "type": "string", "description": "Expected fingerprint for continuation checks." }
      },
      "required": ["path"]
    }
    """).RootElement;

    private static readonly JsonElement ListDirectorySchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "path": { "type": "string", "description": "Relative path of directory to list. Defaults to workspace root ('.' or empty)." },
        "cursor": { "type": "string", "description": "Opaque pagination cursor from previous response." },
        "limit": { "type": "integer", "description": "Maximum number of entries to return (default 100, max 500)." }
      }
    }
    """).RootElement;

    private static readonly JsonElement SearchWorkspaceSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "query": { "type": "string", "description": "Text to search for across visible workspace files." },
        "pathScope": { "type": "string", "description": "Optional relative path to scope search within." },
        "cursor": { "type": "string", "description": "Opaque pagination cursor from previous response." },
        "limit": { "type": "integer", "description": "Maximum number of matches to return (default 50, max 200)." }
      },
      "required": ["query"]
    }
    """).RootElement;

    public static void Configure(IMcpServerBuilder mcpBuilder)
    {
        // 1. Stateless HTTP transport (ADR-003, UC-MCP-01)
        mcpBuilder.WithHttpTransport(options =>
        {
            options.SessionMode = HttpServerSessionMode.Stateless;
        });

        // 2. Read-only tool discovery (BR-COM-002, UC-MCP-01)
        mcpBuilder.WithListToolsHandler((request, ct) =>
        {
            var tools = GetApprovedTools();
            return ValueTask.FromResult(new ListToolsResult
            {
                Tools = tools
            });
        });

        // 3. Tool invocation handler
        mcpBuilder.WithCallToolHandler(async (request, ct) =>
        {
            return await ExecuteToolAsync(request, ct);
        });
    }

    public static List<Tool> GetApprovedTools()
    {
        return
        [
            new Tool
            {
                Name = "workspace_info",
                Title = "Get Workspace Info",
                Description = "Returns safe metadata describing the bound workspace and supported capabilities without leaking local paths.",
                InputSchema = WorkspaceInfoSchema
            },
            new Tool
            {
                Name = "read_file",
                Title = "Read Workspace File",
                Description = "Reads a bounded text chunk from an allowed workspace-relative file.",
                InputSchema = ReadFileSchema
            },
            new Tool
            {
                Name = "list_directory",
                Title = "List Workspace Directory",
                Description = "Lists direct children under a workspace-relative directory with pre-disclosure filtering and pagination.",
                InputSchema = ListDirectorySchema
            },
            new Tool
            {
                Name = "search_workspace",
                Title = "Search Workspace Text",
                Description = "Searches visible text files within the workspace with pre-disclosure filtering, bounded matches, and pagination.",
                InputSchema = SearchWorkspaceSchema
            }
        ];
    }

    private static async Task<CallToolResult> ExecuteToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        string toolName = request.Params.Name;

        switch (toolName)
        {
            case "workspace_info":
                return await HandleWorkspaceInfoAsync(request, ct);

            case "read_file":
                return await HandleReadFileAsync(request, ct);

            case "list_directory":
                return await HandleListDirectoryAsync(request, ct);

            case "search_workspace":
                return await HandleSearchWorkspaceAsync(request, ct);

            default:
                return new CallToolResult
                {
                    IsError = true,
                    Content =
                    [
                        new TextContentBlock
                        {
                            Text = JsonSerializer.Serialize(new
                            {
                                error = new OperationError(
                                    CommonErrorCodes.InvalidArgument,
                                    $"Unknown or unapproved tool '{toolName}'.")
                            }, JsonOptions)
                        }
                    ]
                };
        }
    }

    private static async Task<CallToolResult> HandleWorkspaceInfoAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var infoService = services.GetRequiredService<IWorkspaceInfoService>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var dto = await infoService.GetWorkspaceInfoAsync(context, ct);

        return new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(dto, JsonOptions)
                }
            ]
        };
    }

    private static async Task<CallToolResult> HandleReadFileAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var fileReader = services.GetRequiredService<IWorkspaceFileReader>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string path = string.Empty;
        int startLine = 1;
        int? maxLines = null;
        long startByte = 0;
        int? maxBytes = null;
        string? expectedFingerprint = null;

        if (args != null)
        {
            if (args.TryGetValue("path", out var pElem) && pElem.ValueKind == JsonValueKind.String)
            {
                path = pElem.GetString() ?? string.Empty;
            }

            if (args.TryGetValue("startLine", out var slElem) && slElem.TryGetInt32(out var slVal))
            {
                startLine = slVal;
            }

            if (args.TryGetValue("maxLines", out var mlElem) && mlElem.TryGetInt32(out var mlVal))
            {
                maxLines = mlVal;
            }

            if (args.TryGetValue("startByte", out var sbElem) && sbElem.TryGetInt64(out var sbVal))
            {
                startByte = sbVal;
            }

            if (args.TryGetValue("maxBytes", out var mbElem) && mbElem.TryGetInt32(out var mbVal))
            {
                maxBytes = mbVal;
            }

            if (args.TryGetValue("expectedFingerprint", out var fpElem) && fpElem.ValueKind == JsonValueKind.String)
            {
                expectedFingerprint = fpElem.GetString();
            }
        }

        var readReq = new FileReadRequest(
            Path: path,
            StartLine: startLine,
            MaxLines: maxLines,
            StartByte: startByte,
            MaxBytes: maxBytes,
            ExpectedFingerprint: expectedFingerprint);

        var result = await fileReader.ReadTextAsync(context, readReq, ct);

        if (result.IsFailure)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new { error = result.Error }, JsonOptions)
                    }
                ]
            };
        }

        return new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(result.Value, JsonOptions)
                }
            ]
        };
    }

    private static async Task<CallToolResult> HandleListDirectoryAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var dirReader = services.GetRequiredService<IWorkspaceDirectoryReader>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? path = null;
        string? cursor = null;
        int? limit = null;

        if (args != null)
        {
            if (args.TryGetValue("path", out var pElem) && pElem.ValueKind == JsonValueKind.String)
            {
                path = pElem.GetString();
            }

            if (args.TryGetValue("cursor", out var cElem) && cElem.ValueKind == JsonValueKind.String)
            {
                cursor = cElem.GetString();
            }

            if (args.TryGetValue("limit", out var lElem) && lElem.TryGetInt32(out var lVal))
            {
                limit = lVal;
            }
        }

        var pageReq = new PageRequest(cursor, limit);
        var result = await dirReader.ListAsync(context, path, pageReq, ct);

        if (result.IsFailure)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new { error = result.Error }, JsonOptions)
                    }
                ]
            };
        }

        return new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(result.Value, JsonOptions)
                }
            ]
        };
    }

    private static async Task<CallToolResult> HandleSearchWorkspaceAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var searchService = services.GetRequiredService<IWorkspaceSearchService>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string query = string.Empty;
        string? pathScope = null;
        string? cursor = null;
        int? limit = null;

        if (args != null)
        {
            if (args.TryGetValue("query", out var qElem) && qElem.ValueKind == JsonValueKind.String)
            {
                query = qElem.GetString() ?? string.Empty;
            }

            if (args.TryGetValue("pathScope", out var psElem) && psElem.ValueKind == JsonValueKind.String)
            {
                pathScope = psElem.GetString();
            }

            if (args.TryGetValue("cursor", out var cElem) && cElem.ValueKind == JsonValueKind.String)
            {
                cursor = cElem.GetString();
            }

            if (args.TryGetValue("limit", out var lElem) && lElem.TryGetInt32(out var lVal))
            {
                limit = lVal;
            }
        }

        var searchReq = new WorkspaceSearchRequest(query, pathScope, cursor, limit);
        var result = await searchService.SearchAsync(context, searchReq, ct);

        if (result.IsFailure)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new { error = result.Error }, JsonOptions)
                    }
                ]
            };
        }

        return new CallToolResult
        {
            IsError = false,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(result.Value, JsonOptions)
                }
            ]
        };
    }
}
