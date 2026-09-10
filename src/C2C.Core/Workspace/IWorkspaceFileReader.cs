using C2C.Core.Common;

namespace C2C.Core.Workspace;

/// <summary>
/// Service contract for reading bounded text chunks from an allowed workspace file (UC-WS-04).
/// </summary>
public interface IWorkspaceFileReader
{
    Task<OperationResult<FileChunk>> ReadTextAsync(
        IWorkspaceContext context,
        FileReadRequest request,
        CancellationToken cancellationToken = default);
}
