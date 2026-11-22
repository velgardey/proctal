namespace Proctal.Core;
public sealed class AudioGapClock
{
    private DateTimeOffset last=DateTimeOffset.MinValue;
    public void Received(DateTimeOffset now)=>last=now;
    public byte[]? Pad(DateTimeOffset now,bool active)
    {
        if(!active||now-last<TimeSpan.FromMilliseconds(100))return null;
        last=now;return new byte[4800];
    }
}
