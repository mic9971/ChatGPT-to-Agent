using System.Text.Json.Serialization;

namespace C2C.Core.Execution;

/// <summary>
/// Normalized test execution status adhering to UC-EXE-03 and BR-EXE-007.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TestStatus
{
    /// <summary>
    /// Tests were not executed (e.g. exit code 0 without test metadata).
    /// </summary>
    NotRun = 0,

    /// <summary>
    /// All executed test suites passed.
    /// </summary>
    Passed = 1,

    /// <summary>
    /// One or more tests failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Partial test suite execution (e.g. tests skipped or interrupted).
    /// </summary>
    Partial = 3,

    /// <summary>
    /// Test status cannot be determined from available metadata.
    /// </summary>
    Unknown = 4
}
