using System.Text.Json;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class OverlayUpgradeTests
{
    [Fact] public void PhysicalMonitorOriginSurvivesPreferencesRoundTrip()
    {
        var saved=JsonSerializer.Deserialize<OverlayPreferences>("{\"Left\":1000,\"Top\":50,\"PhysicalLeft\":2000,\"PhysicalTop\":100}")!;
        using var json=JsonDocument.Parse(JsonSerializer.Serialize(saved));
        Assert.Equal(2000,json.RootElement.GetProperty("PhysicalLeft").GetDouble());
        Assert.Equal(100,json.RootElement.GetProperty("PhysicalTop").GetDouble());
    }
    [Fact] public void AltGrTypingDoesNotTriggerCommandsAndReleaseRestoresShortcuts()
    {
        var policy=new ShortcutPolicy();
        policy.Accept(0xA5,true,false);
        foreach(uint key in new uint[]{0x43,0x4C,0x45,0x51})
        {
            Assert.Equal(ShortcutAction.None,policy.Accept(key,true,false,ctrl:true,alt:true));
            Assert.False(policy.IsCaptured(key));
            policy.Accept(key,false,false);
        }
        policy.Accept(0xA5,false,false);
        Assert.Equal(ShortcutAction.CopyCode,policy.Accept(0x43,true,false,ctrl:true,alt:true));
    }
    [Theory]
    [InlineData(0x43)][InlineData(0x25)][InlineData(0x26)][InlineData(0x27)][InlineData(0x28)]
    public void HiddenOverlayLeavesLegacyNavigationAndCopyAvailable(uint key)
    {Assert.Equal(ShortcutAction.None,new ShortcutPolicy().Accept(key,true,false,ctrl:true,shift:true,overlayVisible:false));}
    [Fact] public void PreferencesClampInvalidAppearanceAndRoundTripLocally()
    {
        var p=new OverlayPreferences{Opacity=-1,FontSize=100,Left=double.NaN,Width=10,CodeWrap=true,Compact=true};
        Assert.InRange(p.Opacity,.35,1);Assert.InRange(p.FontSize,12,24);Assert.Null(p.Left);Assert.True(p.Width>=440);
        var dir=Path.Combine(Path.GetTempPath(),"proctal-appearance-"+Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"overlay.json");
        try{p.Save(path);var restored=OverlayPreferences.Load(path);Assert.Equal(p.Opacity,restored.Opacity);Assert.True(restored.CodeWrap);Assert.True(restored.Compact);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact] public void SavedLayoutReturnsToVisibleMonitorAndFitsSmallDesktop()
    {
        var saved=new OverlayPlacement(900,1200,8000,-5000);var visible=saved.Fit(new OverlayPlacement(800,600,-800,0));
        Assert.Equal(800,visible.Width);Assert.Equal(600,visible.Height);Assert.Equal(-800,visible.Left);Assert.Equal(0,visible.Top);
    }
    [Fact] public void BareNavigationKeysAndHiddenOverlayDoNotInterceptTyping()
    {var policy=new ShortcutPolicy();Assert.Equal(ShortcutAction.None,policy.Accept(0x28,true,false));Assert.Equal(ShortcutAction.None,policy.Accept(0x28,true,false,ctrl:true,alt:true,overlayVisible:false));}
    [Theory]
    [InlineData(0x26)][InlineData(0x28)][InlineData(0x21)][InlineData(0x22)][InlineData(0x24)][InlineData(0x23)]
    public void ModifiedNavigationKeysControlOverlay(uint key)
    {var policy=new ShortcutPolicy();Assert.NotEqual(ShortcutAction.None,policy.Accept(key,true,false,ctrl:true,alt:true));}
    [Fact] public void HeldScrollKeyRepeatsWithoutRepeatingToggle()
    {
        var policy=new ShortcutPolicy();Assert.NotEqual(ShortcutAction.None,policy.Accept(0x28,true,false,ctrl:true,alt:true));
        Assert.NotEqual(ShortcutAction.None,policy.Accept(0x28,true,false,ctrl:true,alt:true));
        Assert.Equal(ShortcutAction.Toggle,policy.Accept(0x7B,true,false));Assert.Equal(ShortcutAction.None,policy.Accept(0x7B,true,false));
    }
    [Fact] public void TechnicalRequestLeadsWithCodeAndLanguageFences()
    {
        using var request=JsonDocument.Parse(ResponsesProtocol.BuildRequest("gpt-6-luna","Implement two sum",[],"","Technical"));
        var instructions=request.RootElement.GetProperty("instructions").GetString()!;
        Assert.Contains("code first",instructions,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fenced",instructions,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start immediately with 3",instructions);
        Assert.Equal("none",request.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
    }
}
