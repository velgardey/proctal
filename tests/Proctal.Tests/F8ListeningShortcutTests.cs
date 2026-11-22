using Proctal.Core;
using Xunit;

namespace Proctal.Tests;

public sealed class F8ListeningShortcutTests
{
    [Fact]
    public void F8TogglesListeningOncePerDistinctPressEvenWhenHidden()
    {
        var policy=new ShortcutPolicy();
        Assert.Equal(ShortcutAction.Pause,policy.Accept(0x77,true,false,overlayVisible:false));
        Assert.True(policy.IsCaptured(0x77));
        Assert.Equal(ShortcutAction.None,policy.Accept(0x77,true,false));
        policy.Accept(0x77,false,false);
        Assert.False(policy.IsCaptured(0x77));
        Assert.Equal(ShortcutAction.Pause,policy.Accept(0x77,true,false));
    }

    [Theory]
    [InlineData(true,false,false,false)]
    [InlineData(false,true,false,false)]
    [InlineData(false,false,true,false)]
    [InlineData(false,false,false,true)]
    public void ModifiedF8IsNotCaptured(bool ctrl,bool shift,bool alt,bool rightAlt)
    {
        var policy=new ShortcutPolicy();
        Assert.Equal(ShortcutAction.None,policy.Accept(0x77,true,false,ctrl,shift,alt,rightAlt:rightAlt));
        Assert.False(policy.IsCaptured(0x77));
    }

    [Fact]
    public void InjectedF8DoesNotToggleListening()
    {
        var policy=new ShortcutPolicy();
        Assert.Equal(ShortcutAction.None,policy.Accept(0x77,true,true));
        Assert.False(policy.IsCaptured(0x77));
    }
}
