using C2C.Core.Common;
using C2C.Core.Workspace;

namespace C2C.Infrastructure.Workspace;

/// <summary>
/// Implements canonical path resolution adhering to BR-SEC-001, BR-SEC-002, and architecture/05-WORKSPACE-SECURITY.md.
/// </summary>
public sealed class CanonicalPathResolver : ICanonicalPathResolver
{
    private static readonly char[] IllegalPathCharacters = ['\0'];
    private static readonly string[] DevicePathPrefixes = [@"\\.\", @"\\?\", @"//./", @"//?/"];
    private static readonly string[] WindowsDeviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public OperationResult<string> ResolveCanonicalRoot(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Workspace path cannot be null or empty.",
                rawPath);
        }

        string expanded = ExpandHomeDirectory(rawPath.Trim());

        if (expanded.IndexOfAny(IllegalPathCharacters) >= 0)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                "Path contains invalid control characters.",
                rawPath);
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(expanded);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Invalid filesystem path syntax: {ex.Message}",
                rawPath);
        }

        if (File.Exists(fullPath))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                "Workspace root must be a directory, not a regular file.",
                rawPath);
        }

        if (!Directory.Exists(fullPath))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspaceItemNotFound,
                $"Workspace directory does not exist: {fullPath}",
                rawPath);
        }

        try
        {
            DirectoryInfo dirInfo = new(fullPath);
            FileSystemInfo? target = dirInfo.ResolveLinkTarget(returnFinalTarget: true);
            string finalPath = target != null ? target.FullName : dirInfo.FullName;

            finalPath = Path.GetFullPath(finalPath);
            finalPath = TrimTrailingSeparators(finalPath);

            if (!Directory.Exists(finalPath))
            {
                return OperationResult<string>.Failure(
                    CommonErrorCodes.WorkspaceItemNotFound,
                    $"Canonical target directory does not exist: {finalPath}",
                    rawPath);
            }

            return OperationResult<string>.Success(finalPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Failed to access or resolve workspace directory: {ex.Message}",
                rawPath);
        }
    }

    public OperationResult<string> ResolveWorkspaceRelativePath(string canonicalRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(canonicalRoot))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Canonical workspace root must be provided.",
                canonicalRoot);
        }

        if (relativePath == null)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.InvalidArgument,
                "Relative path cannot be null.",
                nameof(relativePath));
        }

        string effectiveRelativePath = string.IsNullOrWhiteSpace(relativePath) || relativePath.Trim() == "/"
            ? "."
            : relativePath;

        // 1. Syntax validation
        if (effectiveRelativePath.IndexOfAny(IllegalPathCharacters) >= 0)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                "Path contains illegal characters (e.g. NUL byte).",
                relativePath);
        }

        // Reject URI schemes (e.g. file://, http://)
        if (effectiveRelativePath.Contains("://", StringComparison.Ordinal) ||
            effectiveRelativePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                "URI schemes are not permitted in workspace-relative paths.",
                relativePath);
        }

        // Reject device paths
        foreach (string prefix in DevicePathPrefixes)
        {
            if (effectiveRelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<string>.Failure(
                    CommonErrorCodes.WorkspacePathDenied,
                    "Device paths are not permitted.",
                    relativePath);
            }
        }

        // Reject absolute paths
        if (Path.IsPathRooted(effectiveRelativePath))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathOutsideRoot,
                "Absolute paths are not allowed; relative path expected.",
                relativePath);
        }

        // Reject Windows device names
        string normalized = effectiveRelativePath.Replace('\\', '/').Trim('/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (string seg in segments)
        {
            string segWithoutExt = Path.GetFileNameWithoutExtension(seg);
            foreach (string dev in WindowsDeviceNames)
            {
                if (string.Equals(segWithoutExt, dev, StringComparison.OrdinalIgnoreCase))
                {
                    return OperationResult<string>.Failure(
                        CommonErrorCodes.WorkspacePathDenied,
                        $"Reserved device name '{seg}' is not allowed.",
                        relativePath);
                }
            }
        }

        // 2. Lexical combination & traversal check
        string combinedLexical;
        try
        {
            combinedLexical = Path.GetFullPath(Path.Combine(canonicalRoot, effectiveRelativePath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathDenied,
                $"Invalid path syntax: {ex.Message}",
                relativePath);
        }

        if (!IsContained(canonicalRoot, combinedLexical))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathOutsideRoot,
                "Path escapes workspace root via directory traversal ('..').",
                relativePath);
        }

        // 3. Deepest existing ancestor & symlink resolution
        string currentResolved = canonicalRoot;

        foreach (string segment in segments)
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                currentResolved = Path.GetDirectoryName(currentResolved) ?? canonicalRoot;
                if (!IsContained(canonicalRoot, currentResolved))
                {
                    return OperationResult<string>.Failure(
                        CommonErrorCodes.WorkspacePathOutsideRoot,
                        "Path escapes workspace root via directory traversal ('..').",
                        relativePath);
                }
                continue;
            }

            string candidate = Path.Combine(currentResolved, segment);

            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                try
                {
                    FileSystemInfo info = Directory.Exists(candidate)
                        ? new DirectoryInfo(candidate)
                        : new FileInfo(candidate);

                    FileSystemInfo? target = info.ResolveLinkTarget(returnFinalTarget: true);
                    string targetFullName = target != null ? target.FullName : info.FullName;

                    targetFullName = Path.GetFullPath(targetFullName);

                    if (!IsContained(canonicalRoot, targetFullName))
                    {
                        return OperationResult<string>.Failure(
                            CommonErrorCodes.WorkspacePathOutsideRoot,
                            $"Symlink '{segment}' points outside workspace root.",
                            relativePath);
                    }

                    currentResolved = targetFullName;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return OperationResult<string>.Failure(
                        CommonErrorCodes.WorkspacePathDenied,
                        $"Cannot resolve filesystem entry '{candidate}': {ex.Message}",
                        relativePath);
                }
            }
            else
            {
                // Non-existing segment
                if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return OperationResult<string>.Failure(
                        CommonErrorCodes.WorkspacePathDenied,
                        $"Invalid filename characters in segment '{segment}'.",
                        relativePath);
                }

                currentResolved = Path.Combine(currentResolved, segment);
            }
        }

        string finalResolved = Path.GetFullPath(currentResolved);

        if (!IsContained(canonicalRoot, finalResolved))
        {
            return OperationResult<string>.Failure(
                CommonErrorCodes.WorkspacePathOutsideRoot,
                "Resolved canonical path escapes workspace root.",
                relativePath);
        }

        return OperationResult<string>.Success(finalResolved);
    }

    private static bool IsContained(string root, string candidate)
    {
        StringComparison comp = PathComparison;

        if (string.Equals(root, candidate, comp))
        {
            return true;
        }

        string rootWithSep = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSep, comp);
    }

    private static string ExpandHomeDirectory(string path)
    {
        if (path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[2..]);
        }

        return path;
    }

    private static string TrimTrailingSeparators(string path)
    {
        if (path.Length > 1 && (path.EndsWith('/') || path.EndsWith('\\')))
        {
            // Preserve root like "/" on Unix or "C:\" on Windows
            string root = Path.GetPathRoot(path) ?? string.Empty;
            if (!string.Equals(root, path, PathComparison))
            {
                return path.TrimEnd('/', '\\');
            }
        }

        return path;
    }
}
