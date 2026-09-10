using System.Text;
using System.Text.Json;
using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements workspace text search service adhering to UC-WS-05, BR-APP-003, and BR-APP-005.
/// </summary>
public sealed class WorkspaceSearchService : IWorkspaceSearchService
{
    private readonly IWorkspaceAccessPolicy _accessPolicy;
    private readonly ISearchBackend _searchBackend;
    private readonly WorkspaceSearchOptions _options;

    public WorkspaceSearchService(
        IWorkspaceAccessPolicy accessPolicy,
        ISearchBackend searchBackend,
        WorkspaceSearchOptions? options = null)
    {
        _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        _searchBackend = searchBackend ?? throw new ArgumentNullException(nameof(searchBackend));
        _options = options ?? new WorkspaceSearchOptions();
    }

    public async Task<OperationResult<PageResult<WorkspaceSearchMatch>>> SearchAsync(
        IWorkspaceContext context,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Workspace context cannot be null.",
                nameof(context));
        }

        if (request == null)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Search request cannot be null.",
                nameof(request));
        }

        // 1. Validate query length & format
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Search query cannot be empty or whitespace.",
                nameof(request.Query));
        }

        if (request.Query.Length > _options.MaxQueryLength)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.InvalidArgument,
                $"Search query exceeds maximum allowable length of {_options.MaxQueryLength} characters.",
                nameof(request.Query));
        }

        // 2. Authorize and resolve path scope
        string effectiveScope = string.IsNullOrWhiteSpace(request.PathScope) || request.PathScope.Trim() == "/"
            ? "."
            : request.PathScope;

        OperationResult<ResolvedWorkspacePath> scopeResult = _accessPolicy.EvaluatePath(context, effectiveScope);
        if (scopeResult.IsFailure)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(scopeResult.Error!);
        }

        ResolvedWorkspacePath resolvedScope = scopeResult.Value!;
        if (!resolvedScope.Exists)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.WorkspaceItemNotFound,
                $"Search scope directory not found: '{request.PathScope}'.",
                request.PathScope);
        }

        // 3. Decode pagination cursor
        int offset = 0;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            OperationResult<int> decodeResult = DecodeCursor(request.Cursor);
            if (decodeResult.IsFailure)
            {
                return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(decodeResult.Error!);
            }

            offset = decodeResult.Value;
        }

        // 4. Bound limit
        int limit = request.Limit ?? _options.DefaultLimit;
        if (limit <= 0)
        {
            limit = _options.DefaultLimit;
        }
        else if (limit > _options.MaxLimit)
        {
            limit = _options.MaxLimit;
        }

        // 5. Execute search with timeout bound
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.Timeout);

        try
        {
            IReadOnlyList<WorkspaceSearchMatch> allMatches =
                await _searchBackend.SearchAsync(context, resolvedScope, request.Query, _options, cts.Token);

            List<WorkspaceSearchMatch> pagedItems;
            string? nextCursor = null;

            if (offset >= allMatches.Count)
            {
                pagedItems = [];
            }
            else
            {
                pagedItems = allMatches.Skip(offset).Take(limit).ToList();
                if (offset + limit < allMatches.Count)
                {
                    nextCursor = EncodeCursor(offset + limit);
                }
            }

            return OperationResult<PageResult<WorkspaceSearchMatch>>.Success(
                new PageResult<WorkspaceSearchMatch>(pagedItems, nextCursor));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return OperationResult<PageResult<WorkspaceSearchMatch>>.Failure(
                CommonErrorCodes.Timeout,
                $"Search operation timed out after {_options.Timeout.TotalSeconds} seconds.",
                nameof(request.Query));
        }
    }

    private static string EncodeCursor(int offset)
    {
        string json = $"{{\"offset\":{offset}}}";
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"cur_{base64}";
    }

    private static OperationResult<int> DecodeCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor) || !cursor.StartsWith("cur_", StringComparison.Ordinal))
        {
            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Invalid cursor format: cursor must start with 'cur_'.",
                nameof(cursor));
        }

        try
        {
            string payload = cursor[4..]
                .Replace('-', '+')
                .Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("offset", out JsonElement offsetProp) &&
                offsetProp.TryGetInt32(out int offset) &&
                offset >= 0)
            {
                return OperationResult<int>.Success(offset);
            }

            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Malformed cursor payload: non-negative integer 'offset' expected.",
                nameof(cursor));
        }
        catch
        {
            return OperationResult<int>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Malformed or corrupted cursor string.",
                nameof(cursor));
        }
    }
}
