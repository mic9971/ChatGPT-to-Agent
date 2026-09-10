using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2C.Cli.Output;

/// <summary>
/// Output writer for CLI commands adhering to 03-CLI-CONTRACT-AND-EXIT-CODES.md.
/// Strictly isolates machine JSON stdout from human/diagnostic messages.
/// </summary>
public static class CliOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = true
    };

    public static void WriteJson<T>(CliEnvelope<T> envelope)
    {
        string json = JsonSerializer.Serialize(envelope, JsonOptions);
        Console.Out.WriteLine(json);
    }

    public static void WriteHuman(string message)
    {
        Console.Out.WriteLine(message);
    }

    public static void WriteError(string errorMessage)
    {
        Console.Error.WriteLine(errorMessage);
    }

    public static void WriteWarning(string warningMessage)
    {
        Console.Error.WriteLine($"[WARN] {warningMessage}");
    }
}
