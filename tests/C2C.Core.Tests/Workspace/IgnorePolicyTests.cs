using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class IgnorePolicyTests
{
    [Fact]
    public void IsIgnored_MatchingRule_ReturnsTrue()
    {
        var policy = new IgnorePolicy(["*.log", "bin/", "obj/"]);

        Assert.True(policy.IsIgnored("build.log"));
        Assert.True(policy.IsIgnored("logs/debug.log"));
        Assert.True(policy.IsIgnored("bin/Debug/net8.0/app.dll"));
        Assert.True(policy.IsIgnored("obj/project.assets.json"));
    }

    [Fact]
    public void IsIgnored_NonMatchingRule_ReturnsFalse()
    {
        var policy = new IgnorePolicy(["*.log", "bin/"]);

        Assert.False(policy.IsIgnored("src/Program.cs"));
        Assert.False(policy.IsIgnored("README.md"));
    }

    [Fact]
    public void IsIgnored_NegationRule_IgnoredInV1AdditivePolicy()
    {
        // In V1, .c2cignore is additive deny only. Negation does not un-ignore.
        var policy = new IgnorePolicy(["*.log", "!important.log"]);

        Assert.True(policy.IsIgnored("important.log"));
    }

    [Fact]
    public void Constructor_ExcessiveRuleCount_ThrowsInvalidOperationException()
    {
        var rules = Enumerable.Range(0, 501).Select(i => $"rule_{i}.txt");

        Assert.Throws<InvalidOperationException>(() => new IgnorePolicy(rules));
    }
}
