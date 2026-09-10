using System;
using System.Collections.Generic;

namespace C2C.Core.Execution;

/// <summary>
/// Structured test summary adhering to UC-EXE-03 and BR-EXE-003.
/// </summary>
public sealed class TestSummaryDto
{
    public int Total { get; init; }

    public int Passed { get; init; }

    public int Failed { get; init; }

    public int Skipped { get; init; }

    public int Suites { get; init; }

    public IReadOnlyList<string> FailingTests { get; init; } = Array.Empty<string>();
}
