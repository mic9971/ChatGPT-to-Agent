using System.Text.Json;
using Xunit;

using C2C.Cli.Output;

namespace C2C.Cli.Tests.Output;

public sealed class CliEnvelopeTests
{
    [Fact]
    public void CliEnvelope_Serializes_WithRequiredSchemaVersionAndFields()
    {
        var envelope = new CliEnvelope<object>
        {
            Command = "status",
            Status = "ready",
            ActionRequired = "none",
            Data = new { foo = "bar" },
            Warnings = ["A non-fatal warning"]
        };

        string json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("status", root.GetProperty("command").GetString());
        Assert.Equal("ready", root.GetProperty("status").GetString());
        Assert.Equal("none", root.GetProperty("actionRequired").GetString());
        Assert.Equal("bar", root.GetProperty("data").GetProperty("foo").GetString());
        Assert.Single(root.GetProperty("warnings").EnumerateArray());
    }

    [Fact]
    public void CliExitCodes_FollowDocumentedValues()
    {
        Assert.Equal(0, CliExitCode.Success);
        Assert.Equal(1, CliExitCode.RuntimeFailure);
        Assert.Equal(2, CliExitCode.InvalidUsageOrConfig);
        Assert.Equal(3, CliExitCode.ActionRequired);
        Assert.Equal(4, CliExitCode.SecurityDenied);
        Assert.Equal(5, CliExitCode.DependencyMissing);
        Assert.Equal(6, CliExitCode.Conflict);
    }
}
