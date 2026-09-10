using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements safe workspace metadata query per UC-WS-02 and BR-COM-007.
/// </summary>
public sealed class WorkspaceInfoService : IWorkspaceInfoService
{
    private const string CurrentBridgeVersion = "0.1.0";
    private readonly WorkspaceFileReaderOptions _readerOptions;

    public WorkspaceInfoService(WorkspaceFileReaderOptions? readerOptions = null)
    {
        _readerOptions = readerOptions ?? new WorkspaceFileReaderOptions();
    }

    public Task<WorkspaceInfoDto> GetWorkspaceInfoAsync(
        IWorkspaceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool gitAvailable = false;
        string? gitBranch = null;

        try
        {
            string gitDir = Path.Combine(context.CanonicalRoot, ".git");
            if (Directory.Exists(gitDir))
            {
                gitAvailable = true;
                string headFile = Path.Combine(gitDir, "HEAD");
                if (File.Exists(headFile))
                {
                    string headContent = File.ReadAllText(headFile).Trim();
                    const string refPrefix = "ref: refs/heads/";
                    if (headContent.StartsWith(refPrefix, StringComparison.Ordinal))
                    {
                        gitBranch = headContent[refPrefix.Length..];
                    }
                }
            }
        }
        catch
        {
            // Git metadata failure degrades gracefully without leaking paths or throwing
            gitAvailable = false;
            gitBranch = null;
        }

        var dto = new WorkspaceInfoDto
        {
            WorkspaceId = context.Id.Value,
            Label = context.Label,
            GitAvailable = gitAvailable,
            GitBranch = gitBranch,
            ReadOnly = true,
            Capabilities = ["workspace.info", "workspace.read"],
            Limits = new WorkspaceLimitsDto(
                _readerOptions.DefaultMaxLines,
                _readerOptions.AbsoluteMaxLines,
                _readerOptions.DefaultMaxBytes,
                _readerOptions.AbsoluteMaxBytes),
            BridgeVersion = CurrentBridgeVersion
        };

        return Task.FromResult(dto);
    }
}
