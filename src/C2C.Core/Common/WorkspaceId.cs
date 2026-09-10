using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2C.Core.Common;

/// <summary>
/// Strongly-typed identifier for a workspace, derived deterministically without leaking local paths.
/// </summary>
[JsonConverter(typeof(WorkspaceIdJsonConverter))]
public readonly record struct WorkspaceId
{
    public string Value { get; }

    public WorkspaceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Workspace ID cannot be null or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(WorkspaceId id) => id.Value;
}

public sealed class WorkspaceIdJsonConverter : JsonConverter<WorkspaceId>
{
    public override WorkspaceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? val = reader.GetString();
        return string.IsNullOrWhiteSpace(val) ? default : new WorkspaceId(val);
    }

    public override void Write(Utf8JsonWriter writer, WorkspaceId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

