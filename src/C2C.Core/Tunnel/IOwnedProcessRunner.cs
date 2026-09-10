using System.Collections.Generic;

namespace C2C.Core.Tunnel;

/// <summary>
/// Abstraction for starting managed child processes using typed arguments per 07-TUNNEL-DESIGN.md.
/// </summary>
public interface IOwnedProcessRunner
{
    IOwnedProcess Start(
        string executablePath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string ownershipMarker);
}
