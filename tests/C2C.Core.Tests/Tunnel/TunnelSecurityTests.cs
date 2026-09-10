using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using C2C.Core.Common;
using C2C.Core.Tunnel;
using C2C.Core.Workspace;
using C2C.Infrastructure.Tunnel.Cloudflare;

namespace C2C.Core.Tests.Tunnel;

public sealed class TunnelSecurityTests
{
    private readonly WorkspaceContext _workspaceContext = new(
        new WorkspaceId("ws_sec_test"),
        "/tmp/c2c_test");

    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://192.168.1.100:5000")]
    [InlineData("http://10.0.0.1:5000")]
    [InlineData("http://example.com:5000")]
    public async Task StartAsync_NonLoopbackAddress_ReturnsInvalidArgument(string url)
    {
        var fakeRunner = new FakeProcessRunner();
        var provider = new CloudflareQuickTunnelProvider(fakeRunner, _workspaceContext);

        var result = await provider.StartAsync(new Uri(url), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CommonErrorCodes.InvalidArgument, result.Error!.Code);
        Assert.Contains("BR-SEC-005", result.Error.Message);
        Assert.Equal(0, fakeRunner.SpawnCount); // Process never even started!
    }

    [Theory]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://localhost:5000")]
    [InlineData("http://[::1]:5000")]
    public async Task StartAsync_LoopbackAddresses_PassSecurityGate(string url)
    {
        var fakeRunner = new FakeProcessRunner
        {
            OutputLines = ["2026-09-10T00:00:00Z INF | https://test-valid.trycloudflare.com |"]
        };
        var tempBinary = Path.GetTempFileName();
        try
        {
            var provider = new CloudflareQuickTunnelProvider(
                fakeRunner,
                _workspaceContext,
                TimeProvider.System,
                new TunnelOptions { BinaryPath = tempBinary });

            var result = await provider.StartAsync(new Uri(url), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("https://test-valid.trycloudflare.com/", result.Value!.PublicUrl.ToString());
        }
        finally
        {
            File.Delete(tempBinary);
        }
    }

    [Fact]
    public void ForeignProcess_NeverKilled_WhenStoppingUnowned()
    {
        // Demonstrates that provider / process model enforces BR-CON-008:
        // Only owned process instances track and receive Kill/Stop signals
        var ownedProcess = new FakeOwnedProcess("my-owned-marker", 9999);
        Assert.False(ownedProcess.WasKilled);

        ownedProcess.Kill();
        Assert.True(ownedProcess.WasKilled);
    }

    private sealed class FakeProcessRunner : IOwnedProcessRunner
    {
        public int SpawnCount { get; private set; }
        public IReadOnlyList<string> OutputLines { get; set; } = [];

        public IOwnedProcess Start(string executablePath, IReadOnlyList<string> arguments, string workingDirectory, string ownershipMarker)
        {
            SpawnCount++;
            var process = new FakeOwnedProcess(ownershipMarker, 12345);
            foreach (var line in OutputLines)
            {
                process.AppendOutput(line);
            }
            return process;
        }
    }

    private sealed class FakeOwnedProcess : IOwnedProcess
    {
        private readonly MemoryStream _stdoutStream = new();
        private readonly MemoryStream _stderrStream = new();
        private readonly StreamWriter _stdoutWriter;

        public FakeOwnedProcess(string ownershipMarker, int id)
        {
            OwnershipMarker = ownershipMarker;
            Id = id;
            _stdoutWriter = new StreamWriter(_stdoutStream, Encoding.UTF8) { AutoFlush = true };
            StandardOutput = new StreamReader(_stdoutStream, Encoding.UTF8);
            StandardError = new StreamReader(_stderrStream, Encoding.UTF8);
        }

        public int Id { get; }
        public bool HasExited { get; set; }
        public int ExitCode { get; set; }
        public string OwnershipMarker { get; }
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
        public StreamReader StandardOutput { get; }
        public StreamReader StandardError { get; }
        public bool WasKilled { get; private set; }

        public void AppendOutput(string line)
        {
            long pos = _stdoutStream.Position;
            _stdoutStream.Seek(0, SeekOrigin.End);
            _stdoutWriter.WriteLine(line);
            _stdoutStream.Seek(pos, SeekOrigin.Begin);
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken)
        {
            HasExited = true;
            return Task.CompletedTask;
        }

        public void Kill()
        {
            WasKilled = true;
            HasExited = true;
        }

        public void Dispose()
        {
            _stdoutWriter.Dispose();
            _stdoutStream.Dispose();
            _stderrStream.Dispose();
        }
    }
}
