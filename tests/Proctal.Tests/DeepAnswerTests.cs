using System.Net;
using System.Text.Json;
using Proctal.Core;
using Proctal.Desktop;
using Proctal.Desktop.Platform;
using Xunit;
namespace Proctal.Tests;
public sealed class DeepAnswerTests
{
    [Fact] public void F7UsesOneShotDeepAnswerAndDoesNotStealModifiedKeys()
    {
        var policy=new ShortcutPolicy();Assert.Equal("DeepAnswer",policy.Accept(0x76,true,false,overlayVisible:false).ToString());
        Assert.Equal(ShortcutAction.None,policy.Accept(0x76,true,false));policy.Accept(0x76,false,false);
        Assert.Equal(ShortcutAction.None,policy.Accept(0x76,true,false,ctrl:true));Assert.False(policy.IsCaptured(0x76));
        Assert.Equal(ShortcutAction.None,policy.Accept(0x76,true,true));
        Assert.Equal("History",policy.Accept(0x48,true,false,ctrl:true,alt:true).ToString());
        Assert.Equal(ShortcutAction.Pause,policy.Accept(0x77,true,false));
    }
    [Theory]
    [InlineData("none")]
    [InlineData("low")]
    [InlineData("high")]
    [InlineData("xhigh")]
    public void EveryReasoningLevelUsesTheModelOutputLimit(string effort)
    {
        using var json=JsonDocument.Parse(ResponsesProtocol.BuildRequest("gpt-6-luna","Solve",[],"","Technical",effort:effort));
        Assert.Equal(effort,json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.False(json.RootElement.TryGetProperty("max_output_tokens",out _));
        Assert.False(json.RootElement.GetProperty("store").GetBoolean());
    }
    [Fact] public Task DeepTypedAnswerDoesNotChangeSavedFastSettings()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.ManualText="Find two sum";await Deep(controller);
        using var json=JsonDocument.Parse(handler.Bodies.Single());Assert.Equal("xhigh",json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.False(json.RootElement.TryGetProperty("max_output_tokens",out _));
        Assert.Equal("none",controller.Settings.ReasoningEffort);
        controller.ManualText="Explain hashing";await controller.SubmitAsync();using var normal=JsonDocument.Parse(handler.Bodies.Last());
        Assert.Equal("none",normal.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());Assert.False(normal.RootElement.TryGetProperty("max_output_tokens",out _));
    });
    [Fact] public Task DeepCanReanswerDisplayedHistoryAndWaitsForExplicitImages()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        await controller.AnswerAsync("Old question",false);await controller.AnswerAsync("New question",false);controller.BrowseHistory(-1);await Deep(controller);
        Assert.Contains("Old question",handler.Bodies.Last());
        var capture=new TaskCompletionSource<ScreenshotItem>();controller.CaptureScreenshot=()=>capture.Task;var capturing=controller.CaptureAsync();var answering=Deep(controller);
        Assert.False(answering.IsCompleted);capture.SetResult(Proctal.Desktop.Platform.ScreenshotService.FromImage(System.Windows.Media.Imaging.BitmapSource.Create(2,2,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[16],8),"test.png"));
        await capturing;await answering;using var json=JsonDocument.Parse(handler.Bodies.Last());Assert.Contains("input_image",handler.Bodies.Last());Assert.Equal("xhigh",json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
    });
    [Fact] public async Task UnsupportedDeepReasoningIsReportedWithoutDowngrade()
    {
        var handler=new Handler{RejectDeep=true};using var http=new HttpClient(handler);var settings=new AzureSettings{ReasoningEffort="xhigh"};
        var client=new LunaClient(settings,"test-key",http);
        var error=await Assert.ThrowsAsync<CloudServiceException>(async()=>{await foreach(var _ in client.StreamAnswerAsync("q",[],"","Technical")){} });
        Assert.Contains("xhigh",error.Message);Assert.Single(handler.Bodies);
    }
    [Fact] public Task DeepSupersedesFastAnswerButRepeatedDeepDoesNotDuplicateIt()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new HoldingHandler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.ManualText="Optimize two sum";var fast=controller.SubmitAsync();await handler.First.Task;
        var deep=Deep(controller);await handler.Second.Task;await Deep(controller);Assert.Equal(2,handler.Bodies.Count);
        handler.Release.TrySetResult();await deep;await fast;
        using var json=JsonDocument.Parse(handler.Bodies.Last());Assert.Equal("xhigh",json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());Assert.Equal("deep",controller.Answer);
    });
    [Fact] public Task RepeatedDeepFromStoredSpeechKeepsTheExistingRequest()=>ControllerTests.OnDispatcher(async controller=>
    {
        using var handler=new QualityOfLifeTests.ResponseHandler{Hold=true};using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        QualityOfLifeTests.Hear(controller,"question","Explain a hash map.");
        var first=controller.SubmitDeepAsync();await handler.Started.Task;var second=controller.SubmitDeepAsync();
        try{Assert.Equal(1,handler.Calls);Assert.Contains("TRANSCRIPT SNAPSHOT",handler.Body);}
        finally{handler.Release.TrySetResult();await Task.WhenAll(first,second);}
    });
    [Fact] public Task RejectedDeepKeepsTypedInputAndScreenshotsForRetry()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler{RejectDeep=true};using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.ManualText="Solve this";controller.AddImage(ScreenshotService.FromImage(System.Windows.Media.Imaging.BitmapSource.Create(2,2,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[16],8),"test.png"));
        await Assert.ThrowsAsync<CloudServiceException>(()=>Deep(controller));Assert.Equal("Solve this",controller.ManualText);Assert.Single(controller.Screenshots);Assert.Equal("none",controller.Settings.ReasoningEffort);
    });
    [Fact] public Task RepeatedDeepDuringCaptureKeepsTypedPromptAndSendsOnlyOneRequest()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        var frame=new TaskCompletionSource<ScreenshotItem>();controller.CaptureScreenshot=()=>frame.Task;
        controller.ManualText="Use C# and explain this diagram";var capturing=controller.CaptureAsync();var first=Deep(controller);var second=Deep(controller);
        frame.SetResult(ScreenshotService.FromImage(System.Windows.Media.Imaging.BitmapSource.Create(2,2,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[16],8),"test.png"));
        await capturing;await Task.WhenAll(first,second);Assert.Single(handler.Bodies);Assert.Contains("Use C# and explain this diagram",handler.Bodies[0]);Assert.Equal("Use C# and explain this diagram",controller.Question);
    });
    private static Task Deep(ProctalController controller)
    {var method=typeof(ProctalController).GetMethod("SubmitDeepAsync");Assert.NotNull(method);return (Task)method.Invoke(controller,null)!;}
    private sealed class Handler:HttpMessageHandler
    {
        public bool RejectDeep;public List<string> Bodies=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            return RejectDeep?new(HttpStatusCode.BadRequest){Content=new StringContent("unsupported reasoning.effort xhigh")}:new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n")};
        }
    }
    private sealed class HoldingHandler:HttpMessageHandler
    {
        public readonly TaskCompletionSource First=new(TaskCreationOptions.RunContinuationsAsynchronously),Second=new(TaskCreationOptions.RunContinuationsAsynchronously),Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<string> Bodies=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            if(Bodies.Count==1){First.TrySetResult();await Task.Delay(Timeout.Infinite,ct);}
            Second.TrySetResult();await Release.Task.WaitAsync(ct);return new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"deep\"}\n\ndata: {\"type\":\"response.completed\"}\n\n")};
        }
    }
}
