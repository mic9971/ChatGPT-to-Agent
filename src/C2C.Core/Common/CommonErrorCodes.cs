namespace C2C.Core.Common;

/// <summary>
/// Stable machine-readable error codes across C2C.NET.
/// </summary>
public static class CommonErrorCodes
{
    public const string WorkspaceItemNotFound = "WORKSPACE_ITEM_NOT_FOUND";
    public const string WorkspacePathDenied = "WORKSPACE_PATH_DENIED";
    public const string WorkspacePathOutsideRoot = "WORKSPACE_PATH_OUTSIDE_ROOT";
    public const string SensitiveContentDenied = "SENSITIVE_CONTENT_DENIED";
    public const string BinaryContentDenied = "BINARY_CONTENT_DENIED";
    public const string Conflict = "C2C_CONFLICT";
    public const string InvalidArgument = "C2C_INVALID_ARGUMENT";
    public const string OutputLimitExceeded = "OUTPUT_LIMIT_EXCEEDED";
    public const string Timeout = "C2C_TIMEOUT";
    public const string NotReady = "C2C_NOT_READY";
}
