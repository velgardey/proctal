using Proctal.Core;
using Xunit;

namespace Proctal.Tests;

public sealed class F11DoubleTapTests
{
    [Fact]
    public void F9AnswersImagesWithoutAFirstSpeechRequest()
    {
        var policy=new ShortcutPolicy();
        Assert.Equal(ShortcutAction.AnswerImages,policy.Accept(0x78,true,false,overlayVisible:false));
        Assert.Equal(ShortcutAction.None,policy.Accept(0x78,true,false));
        policy.Accept(0x78,false,false);
        Assert.Equal(ShortcutAction.AnswerImages,policy.Accept(0x78,true,false));
    }

    [Fact]
    public void SecondDistinctF11PressAnswersImages()
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
        policy.Accept(0x7A,false,false);
        now=1200;
        Assert.Equal(ShortcutAction.AnswerImages,policy.Accept(0x7A,true,false));
    }

    [Theory]
    [InlineData(499,ShortcutAction.AnswerImages)]
    [InlineData(500,ShortcutAction.AnswerImages)]
    [InlineData(501,ShortcutAction.Answer)]
    [InlineData(-1,ShortcutAction.Answer)]
    public void DoubleTapUsesElapsedTime(long elapsed,ShortcutAction expected)
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        policy.Accept(0x7A,true,false);
        policy.Accept(0x7A,false,false);
        now+=elapsed;
        Assert.Equal(expected,policy.Accept(0x7A,true,false));
    }

    [Fact]
    public void HoldingF11DoesNotTriggerImagesOrExtendTheDoubleTapWindow()
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
        now=1400;
        Assert.Equal(ShortcutAction.None,policy.Accept(0x7A,true,false));
        policy.Accept(0x7A,false,false);
        now=1600;
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
    }

    [Fact]
    public void ThirdPressStartsAnotherPair()
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        foreach(var expected in new[]{ShortcutAction.Answer,ShortcutAction.AnswerImages,ShortcutAction.Answer,ShortcutAction.AnswerImages})
        {
            Assert.Equal(expected,policy.Accept(0x7A,true,false));
            policy.Accept(0x7A,false,false);
            now+=100;
        }
    }

    [Theory]
    [InlineData(0x41)]
    [InlineData(0x79)]
    [InlineData(0x7B)]
    [InlineData(0x11)]
    [InlineData(0xA5)]
    public void InterveningPhysicalKeyStartsAFreshSpeechAnswer(uint key)
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        policy.Accept(0x7A,true,false);
        policy.Accept(0x7A,false,false);
        policy.Accept(key,true,false);
        policy.Accept(key,false,false);
        now=1100;
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
    }

    [Theory]
    [InlineData(true,false,false)]
    [InlineData(false,true,false)]
    [InlineData(false,false,true)]
    public void ModifiedF11DoesNotCompleteOrStartAnImagePair(bool ctrl,bool shift,bool alt)
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        policy.Accept(0x7A,true,false);
        policy.Accept(0x7A,false,false);
        Assert.NotEqual(ShortcutAction.AnswerImages,policy.Accept(0x7A,true,false,ctrl,shift,alt));
        policy.Accept(0x7A,false,false);
        now=1100;
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
    }

    [Fact]
    public void InjectedKeysDoNotCompleteOrInterruptAPhysicalPair()
    {
        var now=1000L;
        var policy=new ShortcutPolicy(()=>now);
        policy.Accept(0x7A,true,false);
        Assert.Equal(ShortcutAction.None,policy.Accept(0x7A,false,true));
        Assert.Equal(ShortcutAction.None,policy.Accept(0x7A,true,true));
        Assert.True(policy.IsCaptured(0x7A));
        policy.Accept(0x7A,false,false);
        Assert.Equal(ShortcutAction.None,policy.Accept(0x41,true,true));
        now=1100;
        Assert.Equal(ShortcutAction.AnswerImages,policy.Accept(0x7A,true,false));
    }

    [Fact]
    public void HiddenOverlayStillAcceptsDoubleF11()
    {
        var policy=new ShortcutPolicy(()=>1000);
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false,overlayVisible:false));
        policy.Accept(0x7A,false,false);
        Assert.Equal(ShortcutAction.AnswerImages,policy.Accept(0x7A,true,false,overlayVisible:false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtrlEnterIsNotCapturedAndResetsF11Pair(bool overlayVisible)
    {
        var policy=new ShortcutPolicy(()=>1000);
        policy.Accept(0x7A,true,false);
        policy.Accept(0x7A,false,false);
        Assert.Equal(ShortcutAction.None,policy.Accept(0x0D,true,false,ctrl:true,overlayVisible:overlayVisible));
        Assert.False(policy.IsCaptured(0x0D));
        policy.Accept(0x0D,false,false);
        Assert.Equal(ShortcutAction.Answer,policy.Accept(0x7A,true,false));
    }

    [Theory]
    [InlineData(true,false,false,false)]
    [InlineData(false,true,false,false)]
    [InlineData(false,false,true,false)]
    [InlineData(false,false,false,true)]
    public void F9DoesNotCaptureModifiedKeys(bool ctrl,bool shift,bool alt,bool rightAlt)
    {
        var policy=new ShortcutPolicy();
        Assert.Equal(ShortcutAction.None,policy.Accept(0x78,true,false,ctrl,shift,alt,rightAlt:rightAlt));
        Assert.False(policy.IsCaptured(0x78));
    }
}
