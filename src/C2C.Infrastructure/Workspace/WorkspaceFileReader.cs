using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements workspace file reader adhering to UC-WS-04, BR-COM-002, BR-COM-006, and BR-APP-003.
/// </summary>
public sealed class WorkspaceFileReader : IWorkspaceFileReader
{
    private readonly IWorkspaceAccessPolicy _accessPolicy;
    private readonly WorkspaceFileReaderOptions _options;

    public WorkspaceFileReader(
        IWorkspaceAccessPolicy accessPolicy,
        WorkspaceFileReaderOptions? options = null)
    {
        _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
        _options = options ?? new WorkspaceFileReaderOptions();
    }

    public async Task<OperationResult<FileChunk>> ReadTextAsync(
        IWorkspaceContext context,
        FileReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate request parameters
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Path cannot be null or whitespace.",
                nameof(request.Path));
        }

        if (request.StartLine < 1)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.InvalidArgument,
                "StartLine must be 1 or greater.",
                nameof(request.StartLine));
        }

        if (request.StartByte < 0)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.InvalidArgument,
                "StartByte must be 0 or greater.",
                nameof(request.StartByte));
        }

        int maxLines = request.MaxLines ?? _options.DefaultMaxLines;
        if (maxLines > _options.AbsoluteMaxLines)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.OutputLimitExceeded,
                $"Requested MaxLines ({maxLines}) exceeds hard cap of {_options.AbsoluteMaxLines}.",
                nameof(request.MaxLines));
        }

        int maxBytes = request.MaxBytes ?? _options.DefaultMaxBytes;
        if (maxBytes > _options.AbsoluteMaxBytes)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.OutputLimitExceeded,
                $"Requested MaxBytes ({maxBytes}) exceeds hard cap of {_options.AbsoluteMaxBytes}.",
                nameof(request.MaxBytes));
        }

        // 2. Authorize via existing single workspace access policy gate
        var accessResult = _accessPolicy.EvaluatePath(context, request.Path);
        if (accessResult.IsFailure)
        {
            return OperationResult<FileChunk>.Failure(accessResult.Error!);
        }

        var resolvedPath = accessResult.Value!;

        if (!resolvedPath.Exists)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.WorkspaceItemNotFound,
                $"File not found: {request.Path}",
                request.Path);
        }

        if (resolvedPath.IsDirectory)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Target path is a directory, not a regular file: {request.Path}",
                request.Path);
        }

        try
        {
            FileInfo fileInfo = new(resolvedPath.CanonicalFullPath);

            // 3. Compute deterministic fingerprint
            string fingerprint = ComputeFingerprint(fileInfo);

            if (!string.IsNullOrWhiteSpace(request.ExpectedFingerprint) &&
                !string.Equals(request.ExpectedFingerprint, fingerprint, StringComparison.Ordinal))
            {
                return OperationResult<FileChunk>.Failure(
                    CommonErrorCodes.Conflict,
                    "File was modified since previous read; continuation must restart.",
                    request.Path);
            }

            // 4. Open with safe sharing semantics (concurrent writers allowed, no exclusive lock)
            await using FileStream fs = new(
                resolvedPath.CanonicalFullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true);

            // 5. Binary detection sample
            if (await IsBinaryFileAsync(fs, _options.BinarySampleSizeBytes, cancellationToken))
            {
                return OperationResult<FileChunk>.Failure(
                    CommonErrorCodes.BinaryContentDenied,
                    "Binary content detected; only text files may be read.",
                    request.Path);
            }

            // Reset stream to beginning for text reading
            fs.Seek(0, SeekOrigin.Begin);

            // 6. Read text within bounded line/byte window
            using StreamReader reader = new(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            int currentLineNumber = 1;
            // Skip lines up to StartLine - 1
            while (currentLineNumber < request.StartLine)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? skippedLine = await reader.ReadLineAsync(cancellationToken);
                if (skippedLine == null)
                {
                    // Reached EOF before StartLine
                    return OperationResult<FileChunk>.Success(new FileChunk
                    {
                        RelativePath = resolvedPath.RelativePath,
                        Content = string.Empty,
                        StartLine = request.StartLine,
                        EndLine = request.StartLine,
                        TotalLinesRead = 0,
                        StartByte = fileInfo.Length,
                        EndByte = fileInfo.Length,
                        FileSizeBytes = fileInfo.Length,
                        Encoding = reader.CurrentEncoding.WebName,
                        Fingerprint = fingerprint,
                        IsTruncated = false,
                        NextCursor = null
                    });
                }
                currentLineNumber++;
            }

            StringBuilder contentBuilder = new();
            int linesRead = 0;
            int chunkStartLine = currentLineNumber;
            int chunkEndLine = currentLineNumber;
            int totalBytesAccumulated = 0;
            bool reachedLimit = false;

            while (linesRead < maxLines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }

                byte[] lineBytes = reader.CurrentEncoding.GetBytes(line + Environment.NewLine);
                if (linesRead > 0 && (totalBytesAccumulated + lineBytes.Length) > maxBytes)
                {
                    // Adding this line would exceed maxBytes
                    reachedLimit = true;
                    break;
                }

                if (contentBuilder.Length > 0)
                {
                    contentBuilder.AppendLine();
                }

                contentBuilder.Append(line);
                totalBytesAccumulated += lineBytes.Length;
                chunkEndLine = currentLineNumber;
                linesRead++;
                currentLineNumber++;

                if (totalBytesAccumulated >= maxBytes)
                {
                    reachedLimit = true;
                    break;
                }
            }

            // Check if there are remaining lines in the file
            bool hasMoreLines = false;
            if (!reader.EndOfStream)
            {
                hasMoreLines = true;
            }

            bool isTruncated = reachedLimit || hasMoreLines;
            string? nextCursor = null;

            if (isTruncated)
            {
                var cursorPayload = new CursorPayload(currentLineNumber, fingerprint);
                string cursorJson = JsonSerializer.Serialize(cursorPayload);
                nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(cursorJson));
            }

            return OperationResult<FileChunk>.Success(new FileChunk
            {
                RelativePath = resolvedPath.RelativePath,
                Content = contentBuilder.ToString(),
                StartLine = chunkStartLine,
                EndLine = linesRead > 0 ? chunkEndLine : chunkStartLine,
                TotalLinesRead = linesRead,
                StartByte = request.StartByte,
                EndByte = request.StartByte + totalBytesAccumulated,
                FileSizeBytes = fileInfo.Length,
                Encoding = reader.CurrentEncoding.WebName,
                Fingerprint = fingerprint,
                IsTruncated = isTruncated,
                NextCursor = nextCursor
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return OperationResult<FileChunk>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Cannot access file: {ex.Message}",
                request.Path);
        }
    }

    private static string ComputeFingerprint(FileInfo fileInfo)
    {
        byte[] input = Encoding.UTF8.GetBytes($"{fileInfo.Length}:{fileInfo.LastWriteTimeUtc.Ticks}");
        byte[] hash = SHA256.HashData(input);
        return "fp_" + Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static async Task<bool> IsBinaryFileAsync(
        FileStream stream,
        int sampleSize,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[Math.Min(sampleSize, (int)Math.Min(stream.Length, 8192))];
        if (buffer.Length == 0)
        {
            return false;
        }

        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

        // Check for NUL bytes indicating binary content (skipping possible UTF-16 / UTF-32 BOMs)
        int startIndex = 0;
        if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
        {
            startIndex = 2; // UTF-16 LE BOM
        }
        else if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
        {
            startIndex = 2; // UTF-16 BE BOM
        }
        else if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
        {
            startIndex = 3; // UTF-8 BOM
        }

        for (int i = startIndex; i < bytesRead; i++)
        {
            if (buffer[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record CursorPayload(int Line, string Fp);
}
