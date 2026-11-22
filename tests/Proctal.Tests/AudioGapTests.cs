using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class AudioGapTests
{
    [Fact] public void LoopbackGapSuppliesSilenceSoTheSpeechTurnCanCommit()
    {
        var clock=new AudioGapClock();var start=DateTimeOffset.UtcNow;clock.Received(start);
        Assert.Null(clock.Pad(start.AddMilliseconds(50),true));
        var padded=clock.Pad(start.AddMilliseconds(200),true);
        Assert.NotNull(padded);Assert.Equal(4800,padded.Length);Assert.All(padded,b=>Assert.Equal(0,b));
        Assert.Null(clock.Pad(start.AddMilliseconds(500),false));
    }
}
