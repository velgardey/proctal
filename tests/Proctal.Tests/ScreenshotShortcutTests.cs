using System.Text.Json;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class ScreenshotShortcutTests
{
    [Fact] public void F10CapturesAndF11SubmitsWithoutRepetition()
    {
        var p=new ShortcutPolicy();Assert.Equal(ShortcutAction.Screenshot,p.Accept(0x79,true,false));
        Assert.Equal(ShortcutAction.None,p.Accept(0x79,true,false));
        Assert.Equal(ShortcutAction.Answer,p.Accept(0x7A,true,false));
    }
    [Fact] public void ScreenshotIsSentAsImageInputAndFastIsRequested()
    {
        var body=ResponsesProtocol.BuildRequest("gpt-6-luna","Solve the shown question",[],"","Technical",[new ImageAttachment("screen.png","data:image/png;base64,AQID")]);
        using var j=JsonDocument.Parse(body);var r=j.RootElement;
        Assert.Equal("none",r.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.Equal("priority",r.GetProperty("service_tier").GetString());
        var parts=r.GetProperty("input")[0].GetProperty("content");
        Assert.Equal("input_image",parts[1].GetProperty("type").GetString());
        Assert.Equal("data:image/png;base64,AQID",parts[1].GetProperty("image_url").GetString());
    }
}
