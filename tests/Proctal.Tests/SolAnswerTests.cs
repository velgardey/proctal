using System.Net;
using System.Text.Json;
using Proctal.Core;
using Proctal.Desktop;
using Xunit;
namespace Proctal.Tests;
public sealed class SolAnswerTests
{
    [Fact] public void F6RequestsSolOnceAndIgnoresModifiedOrInjectedKeys()
    {
        var policy=new ShortcutPolicy();Assert.Equal("SolAnswer",policy.Accept(0x75,true,false,overlayVisible:false).ToString());
        Assert.Equal(ShortcutAction.None,policy.Accept(0x75,true,false));policy.Accept(0x75,false,false);
        Assert.Equal(ShortcutAction.None,policy.Accept(0x75,true,false,ctrl:true));Assert.False(policy.IsCaptured(0x75));
        Assert.Equal(ShortcutAction.None,policy.Accept(0x75,true,true));
    }
    [Fact] public Task SolUsesStoredSpeechAndSessionContextWithoutChangingLuna()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        await controller.AnswerAsync("Previous question",false);
        QualityOfLifeTests.Hear(controller,"follow-up","Now use DFS.");await Sol(controller);
        using var sol=JsonDocument.Parse(handler.Bodies.Last());Assert.Equal("gpt-6.1-sol",sol.RootElement.GetProperty("model").GetString());
        Assert.Equal("high",sol.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.Contains("Previous question",handler.Bodies.Last());Assert.Contains("Now use DFS",handler.Bodies.Last());
        Assert.Equal("gpt-6-luna",controller.Settings.AnswerDeployment);Assert.Equal("none",controller.Settings.ReasoningEffort);
        controller.ManualText="Explain complexity";await controller.SubmitAsync();using var luna=JsonDocument.Parse(handler.Bodies.Last());
        Assert.Equal("gpt-6-luna",luna.RootElement.GetProperty("model").GetString());
    });
    [Fact] public Task SwitchingModelReplacesActiveStreamAndRepeatedSolDoesNotDuplicate()=>ControllerTests.OnDispatcher(async controller=>
    {
        using var handler=new QualityOfLifeTests.ResponseHandler{Hold=true};using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.Settings.ReasoningEffort="high";
        controller.ManualText="Solve two sum";var luna=controller.SubmitAsync();await handler.Started.Task;
        var sol=Sol(controller);
        try{Assert.Equal(2,handler.Calls);await Sol(controller);Assert.Equal(2,handler.Calls);Assert.Contains("gpt-6.1-sol",handler.Body);}
        finally{handler.Release.TrySetResult();await Task.WhenAll(luna,sol);}
    });
    [Fact] public Task SolConfigurationRoundTripsAndMissingDeploymentKeepsInput()=>ControllerTests.OnDispatcher(async controller=>
    {
        controller.Settings.SolDeployment="custom-sol";controller.SaveSettings("");
        Assert.Equal("custom-sol",AzureSettings.Load(Path.Combine(controller.DataPath,"settings.json")).SolDeployment);
        controller.Settings.SolDeployment="";controller.ManualText="Explain the code";
        await Assert.ThrowsAsync<ArgumentException>(async()=>await controller.SubmitSolAsync());Assert.Equal("Explain the code",controller.ManualText);
    });
    [Fact] public Task LatestModelWinsWhileCaptureIsPending()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        var frame=new TaskCompletionSource<Proctal.Desktop.Platform.ScreenshotItem>();controller.CaptureScreenshot=()=>frame.Task;
        controller.ManualText="Use DFS for the pictured problem";var capture=controller.CaptureAsync();var luna=controller.SubmitImagesAsync();var sol=controller.SubmitSolAsync();var repeated=controller.SubmitSolAsync();
        frame.SetResult(Proctal.Desktop.Platform.ScreenshotService.FromImage(System.Windows.Media.Imaging.BitmapSource.Create(2,2,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[16],8),"test.png"));
        await Task.WhenAll(capture,luna,sol,repeated);
        Assert.Single(handler.Bodies);Assert.Contains("gpt-6.1-sol",handler.Bodies[0]);Assert.Contains("Use DFS",handler.Bodies[0]);Assert.Contains("input_image",handler.Bodies[0]);
    });
    private static Task Sol(ProctalController controller)
    {var method=typeof(ProctalController).GetMethod("SubmitSolAsync");Assert.NotNull(method);return (Task)method.Invoke(controller,null)!;}
    private sealed class Handler:HttpMessageHandler
    {
        public List<string> Bodies=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n")};
        }
    }
}
