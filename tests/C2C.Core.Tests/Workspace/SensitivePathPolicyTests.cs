using C2C.Infrastructure.Workspace;
using Xunit;

namespace C2C.Core.Tests.Workspace;

public sealed class SensitivePathPolicyTests
{
    private readonly SensitivePathPolicy _policy = new();

    [Theory]
    [InlineData(".env")]
    [InlineData(".env.local")]
    [InlineData(".env.production")]
    [InlineData(".env.staging")]
    [InlineData("config/.env")]
    [InlineData("sub/deep/.env.local")]
    [InlineData(".ENV")]
    [InlineData(".Env.Production")]
    public void IsSensitive_EnvFiles_ReturnsTrue(string path)
    {
        Assert.True(_policy.IsSensitive(path));
    }

    [Theory]
    [InlineData(".env.example")]
    [InlineData(".env.sample")]
    [InlineData(".env.template")]
    [InlineData("config/.env.example")]
    [InlineData("sub/.env.sample")]
    [InlineData(".ENV.EXAMPLE")]
    public void IsSensitive_AllowedEnvExamples_ReturnsFalse(string path)
    {
        Assert.False(_policy.IsSensitive(path));
    }

    [Theory]
    [InlineData("server.key")]
    [InlineData("cert.pem")]
    [InlineData("identity.p12")]
    [InlineData("bundle.pfx")]
    [InlineData("certs/tls.key")]
    [InlineData("SUB/PRIVATE.KEY")]
    public void IsSensitive_PrivateKeyAndCertPatterns_ReturnsTrue(string path)
    {
        Assert.True(_policy.IsSensitive(path));
    }

    [Theory]
    [InlineData("id_rsa")]
    [InlineData("id_rsa.pub")]
    [InlineData("id_ed25519")]
    [InlineData("id_ecdsa")]
    [InlineData("id_dsa")]
    [InlineData(".ssh/id_rsa")]
    [InlineData(".ssh/config")]
    [InlineData(".ssh/known_hosts")]
    [InlineData(".aws/credentials")]
    [InlineData(".aws/config")]
    [InlineData(".azure/tokens.json")]
    [InlineData(".gcp/credentials.json")]
    [InlineData("service-account-key.json")]
    [InlineData("credentials.json")]
    [InlineData("login.keychain")]
    [InlineData(".git/config")]
    [InlineData(".git/HEAD")]
    public void IsSensitive_CloudAndSshCredentials_ReturnsTrue(string path)
    {
        Assert.True(_policy.IsSensitive(path));
    }

    [Theory]
    [InlineData("src/Program.cs")]
    [InlineData("README.md")]
    [InlineData("package.json")]
    [InlineData("appsettings.json")]
    [InlineData("docs/security.md")]
    [InlineData("test/MyKeyTests.cs")]
    [InlineData("env_helper.py")]
    public void IsSensitive_NormalWorkspaceFiles_ReturnsFalse(string path)
    {
        Assert.False(_policy.IsSensitive(path));
    }
}
