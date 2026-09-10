using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenIddict.Abstractions;

using C2C.Core.Authorization;
using C2C.Core.Common;
using C2C.Core.Execution;
using C2C.Core.Git;
using C2C.Core.Workspace;
using C2C.Host.Auth;

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

    private static readonly JsonElement GitStatusSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "pathScope": { "type": "string", "description": "Optional workspace-relative path to scope status output within." }
      }
    }
    """).RootElement;

    private static readonly JsonElement GitDiffSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "base": { "type": "string", "description": "Optional base ref (e.g. 'HEAD', 'main'). Omit to compare working tree to index." },
        "cursor": { "type": "string", "description": "Opaque pagination cursor from previous response." },
        "limit": { "type": "integer", "description": "Maximum number of diff records to return." }
      }
    }
    """).RootElement;

    private static readonly JsonElement ExecutionSummarySchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "executionId": { "type": "string", "description": "Unique identifier of the execution record." }
      },
      "required": ["executionId"]
    }
    """).RootElement;

    private static readonly JsonElement TestStatusSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "executionId": { "type": "string", "description": "Unique identifier of the execution record." }
      },
      "required": ["executionId"]
    }
    """).RootElement;

    private static readonly JsonElement ExecutionOutputSchema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "executionId": { "type": "string", "description": "Unique identifier of the execution record." },
        "artifactId": { "type": "string", "description": "Identifier of the artifact to read." },
        "cursor": { "type": "string", "description": "Opaque pagination cursor from previous response." },
        "limitLines": { "type": "integer", "description": "Maximum lines of content to return in this chunk." },
        "limitBytes": { "type": "integer", "description": "Maximum bytes of content to return in this chunk." }
      },
      "required": ["executionId", "artifactId"]
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
            },
            new Tool
            {
                Name = "git_status",
                Title = "Read Git Status",
                Description = "Returns bounded machine-readable working-tree status for workspace-visible paths only. Denied paths are excluded without leaking their names.",
                InputSchema = GitStatusSchema
            },
            new Tool
            {
                Name = "git_diff",
                Title = "Read Git Diff",
                Description = "Returns bounded diff bodies only for changed paths that pass workspace visibility policy. Uses two-stage design: paths are filtered before diff bodies are fetched.",
                InputSchema = GitDiffSchema
            },
            new Tool
            {
                Name = "execution_summary",
                Title = "Read Execution Summary",
                Description = "Returns safe execution evidence metadata, exit status, visible changed files, and artifact references for an execution record.",
                InputSchema = ExecutionSummarySchema
            },
            new Tool
            {
                Name = "test_status",
                Title = "Read Normalized Test Status",
                Description = "Returns normalized test suites, counts, and status (NotRun, Passed, Failed, Partial, Unknown) for an execution record.",
                InputSchema = TestStatusSchema
            },
            new Tool
            {
                Name = "execution_output",
                Title = "Read Sanitized Execution Output",
                Description = "Returns bounded paginated chunks of sanitized execution output. Restricted artifacts expose metadata only and never the body.",
                InputSchema = ExecutionOutputSchema
            }
        ];
    }

    private static async Task<CallToolResult> ExecuteToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        string toolName = request.Params.Name;

        // Tool-level OAuth scope verification adhering to UC-MCP-02 and BR-SEC-007
        string? requiredScope = McpToolScopes.GetRequiredScope(toolName);
        if (requiredScope != null)
        {
            var services = request.Services;
            var authOptions = services?.GetService<AuthorizationServerOptions>();

            if (authOptions?.EnableRemoteAuthorization == true)
            {
                var httpContextAccessor = services?.GetService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor?.HttpContext;
                var user = httpContext?.User;

                if (user == null || user.Identity?.IsAuthenticated != true || !user.HasScope(requiredScope))
                {
                    if (httpContext != null)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        string metadataUrl = $"{authOptions.Issuer.TrimEnd('/')}/.well-known/oauth-protected-resource";
                        httpContext.Response.Headers["WWW-Authenticate"] =
                            $"Bearer error=\"insufficient_scope\", scope=\"{requiredScope}\", resource_metadata=\"{metadataUrl}\"";
                    }

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
                                        "INSUFFICIENT_SCOPE",
                                        $"The caller does not have the required scope '{requiredScope}' for tool '{toolName}'.")
                                }, JsonOptions)
                            }
                        ]
                    };
                }
            }
        }

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

            case "git_status":
                return await HandleGitStatusAsync(request, ct);

            case "git_diff":
                return await HandleGitDiffAsync(request, ct);

            case "execution_summary":
                return await HandleExecutionSummaryAsync(request, ct);

            case "test_status":
                return await HandleTestStatusAsync(request, ct);

            case "execution_output":
                return await HandleExecutionOutputAsync(request, ct);

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

    private static async Task<CallToolResult> HandleGitStatusAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var gitReader = services.GetRequiredService<IGitReader>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? pathScope = null;

        if (args != null)
        {
            if (args.TryGetValue("pathScope", out var psElem) && psElem.ValueKind == JsonValueKind.String)
            {
                pathScope = psElem.GetString();
            }
        }

        var gitReq = new GitStatusRequest { PathScope = pathScope };
        var result = await gitReader.GetStatusAsync(context, gitReq, ct);

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

    private static async Task<CallToolResult> HandleGitDiffAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var gitReader = services.GetRequiredService<IGitReader>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? baseRef = null;
        string? cursor = null;
        int? limit = null;

        if (args != null)
        {
            if (args.TryGetValue("base", out var bElem) && bElem.ValueKind == JsonValueKind.String)
            {
                baseRef = bElem.GetString();
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

        var gitReq = new GitDiffRequest { Base = baseRef, Cursor = cursor, Limit = limit };
        var result = await gitReader.GetDiffAsync(context, gitReq, ct);

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

    private static async Task<CallToolResult> HandleExecutionSummaryAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var query = services.GetRequiredService<IExecutionQuery>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? executionId = null;
        if (args != null && args.TryGetValue("executionId", out var idElem) && idElem.ValueKind == JsonValueKind.String)
        {
            executionId = idElem.GetString();
        }

        if (string.IsNullOrWhiteSpace(executionId))
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new
                        {
                            error = new OperationError(CommonErrorCodes.InvalidArgument, "executionId is required.")
                        }, JsonOptions)
                    }
                ]
            };
        }

        var result = await query.GetSummaryAsync(context.Id.Value, executionId, ct);
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

    private static async Task<CallToolResult> HandleTestStatusAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var query = services.GetRequiredService<IExecutionQuery>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? executionId = null;
        if (args != null && args.TryGetValue("executionId", out var idElem) && idElem.ValueKind == JsonValueKind.String)
        {
            executionId = idElem.GetString();
        }

        if (string.IsNullOrWhiteSpace(executionId))
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new
                        {
                            error = new OperationError(CommonErrorCodes.InvalidArgument, "executionId is required.")
                        }, JsonOptions)
                    }
                ]
            };
        }

        var result = await query.GetTestStatusAsync(context.Id.Value, executionId, ct);
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

    private static async Task<CallToolResult> HandleExecutionOutputAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken ct)
    {
        var services = request.Services ?? throw new InvalidOperationException("Service provider is unavailable.");
        var reader = services.GetRequiredService<IExecutionArtifactReader>();
        var context = services.GetRequiredService<IWorkspaceContext>();

        var args = request.Params.Arguments;
        string? executionId = null;
        string? artifactId = null;
        string? cursor = null;
        int? limitLines = null;
        int? limitBytes = null;

        if (args != null)
        {
            if (args.TryGetValue("executionId", out var idElem) && idElem.ValueKind == JsonValueKind.String)
            {
                executionId = idElem.GetString();
            }

            if (args.TryGetValue("artifactId", out var artElem) && artElem.ValueKind == JsonValueKind.String)
            {
                artifactId = artElem.GetString();
            }

            if (args.TryGetValue("cursor", out var cElem) && cElem.ValueKind == JsonValueKind.String)
            {
                cursor = cElem.GetString();
            }

            if (args.TryGetValue("limitLines", out var llElem) && llElem.TryGetInt32(out var llVal))
            {
                limitLines = llVal;
            }

            if (args.TryGetValue("limitBytes", out var lbElem) && lbElem.TryGetInt32(out var lbVal))
            {
                limitBytes = lbVal;
            }
        }

        if (string.IsNullOrWhiteSpace(executionId))
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new
                        {
                            error = new OperationError(CommonErrorCodes.InvalidArgument, "executionId is required.")
                        }, JsonOptions)
                    }
                ]
            };
        }

        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new TextContentBlock
                    {
                        Text = JsonSerializer.Serialize(new
                        {
                            error = new OperationError(CommonErrorCodes.InvalidArgument, "artifactId is required.")
                        }, JsonOptions)
                    }
                ]
            };
        }

        var chunkReq = new ArtifactChunkRequest
        {
            ExecutionId = executionId,
            ArtifactId = artifactId,
            Cursor = cursor,
            LimitLines = limitLines,
            LimitBytes = limitBytes
        };

        var result = await reader.GetChunkAsync(context.Id.Value, chunkReq, ct);
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
