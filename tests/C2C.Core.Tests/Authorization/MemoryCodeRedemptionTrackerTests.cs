using Xunit;

using C2C.Infrastructure.Authorization;

namespace C2C.Core.Tests.Authorization;

public sealed class MemoryCodeRedemptionTrackerTests
{
    [Fact]
    public void TryRedeemCode_FirstRedemption_Succeeds()
    {
        var tracker = new MemoryCodeRedemptionTracker();
        bool redeemed = tracker.TryRedeemCode("code-1");
        Assert.True(redeemed);
    }

    [Fact]
    public void TryRedeemCode_SecondRedemption_FailsClosed()
    {
        var tracker = new MemoryCodeRedemptionTracker();
        Assert.True(tracker.TryRedeemCode("code-replay"));
        Assert.False(tracker.TryRedeemCode("code-replay"));
    }
}
