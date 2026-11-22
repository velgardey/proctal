using System.Net;
using System.Text;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using Proctal.Core;
using Proctal.Desktop;
using Proctal.Desktop.Platform;
using Xunit;
namespace Proctal.Tests;
public sealed class ControllerTests
{
    [Fact] public Task MissingMicrophoneStopsBeforeReadingCredentialsOrConnecting()=>OnDispatcher(async controller=>
    {
        controller.SelectedOutput=new AudioDevice("test-output","Selected output");controller.SelectedInput=null;controller.Settings.IncludeMicrophone=true;
        await File.WriteAllBytesAsync(Path.Combine(controller.DataPath,"azure-key.bin"),[0]);
        var error=await Assert.ThrowsAsync<InvalidDataException>(controller.StartListeningAsync);Assert.Contains("microphone",error.Message,StringComparison.OrdinalIgnoreCase);Assert.False(controller.IsStarting);Assert.False(controller.IsListening);
    });
    [Fact] public Task SavingOtherSettingsKeepsUnavailableAudioIds()=>OnDispatcher(controller=>
    {
        controller.Settings.OutputDeviceId="saved-output";controller.Settings.InputDeviceId="saved-input";controller.SelectedOutput=null;controller.SelectedInput=null;
        controller.SaveSettings("");var saved=AzureSettings.Load(Path.Combine(controller.DataPath,"settings.json"));
        Assert.Equal("saved-output",saved.OutputDeviceId);Assert.Equal("saved-input",saved.InputDeviceId);return Task.CompletedTask;
    });
    [Fact] public Task FailedBatchImportKeepsExistingDocumentsUnchanged()=>OnDispatcher(async controller=>
    {
        controller.AddNotes("Existing","Existing context must remain saved and visible.");
        var before=controller.Documents.ToArray();var saved=File.ReadAllText(Path.Combine(controller.DataPath,"documents.json"));
        var good=Path.Combine(controller.DataPath,"good.txt");var bad=Path.Combine(controller.DataPath,"bad.bin");
        await File.WriteAllTextAsync(good,"A valid new context document for interview practice.");await File.WriteAllTextAsync(bad,"Unsupported document.");
        await Assert.ThrowsAsync<InvalidDataException>(()=>controller.ImportDocumentsAsync([good,bad]));
        Assert.Equal(before,controller.Documents.ToArray());Assert.Equal(saved,File.ReadAllText(Path.Combine(controller.DataPath,"documents.json")));
    });
    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    [InlineData("remove")]
    public Task FailedContextWriteKeepsTheVisibleAndSavedDocument(string action)=>OnDispatcher(controller=>
    {
        controller.AddNotes("Existing","Existing context must remain saved and visible.");
        var original=Assert.Single(controller.Documents);var saved=File.ReadAllText(Path.Combine(controller.DataPath,"documents.json"));
        Directory.CreateDirectory(Path.Combine(controller.DataPath,"documents.json.tmp"));
        var failure=Record.Exception(()=>
        {
            if(action=="add")controller.AddNotes("New","New context must not appear if its save fails.");
            else if(action=="edit")controller.SaveDocument(original,"Changed context must not appear if its save fails.");
            else controller.RemoveDocument(original);
        });
        Assert.NotNull(failure);Assert.Equal(original,Assert.Single(controller.Documents));Assert.Equal(saved,File.ReadAllText(Path.Combine(controller.DataPath,"documents.json")));
        return Task.CompletedTask;
    });
    [Fact] public Task CorruptHistoryDoesNotBreakLoading()=>OnDispatcher(async controller=>
    {
        await File.WriteAllTextAsync(Path.Combine(controller.DataPath,"answers.json"),"{ incomplete history");
        await controller.LoadHistoryAsync();Assert.Empty(controller.History);Assert.Contains("history",controller.Status,StringComparison.OrdinalIgnoreCase);
        Assert.Single(Directory.GetFiles(controller.DataPath,"answers.corrupt-*.json"));
    });
    [Fact] public Task BrowsingHistoryKeepsLiveAnswerAndFollowupUsesDisplayedContext()=>OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        await controller.AnswerAsync("Describe migration one",false);await controller.AnswerAsync("Describe migration two",false);
        controller.BrowseHistory(-1);Assert.Equal("Describe migration one",controller.DisplayQuestion);Assert.Equal("Describe migration two",controller.Question);
        await controller.FollowUpAsync("explain");Assert.Contains("Describe migration one",handler.Body);Assert.Contains("ready",handler.Body);
        controller.ShowLatestAnswer();Assert.False(controller.IsBrowsingHistory);
        controller.HistoryQuery="migration two";Assert.Single(controller.VisibleHistory);
    });
    [Fact] public Task AutomaticQuestionWaitsForHeardContinuationBeforeCommit()=>OnDispatcher(async controller=>
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(ProctalController);
        ((DispatcherTimer)type.GetField("timer",flags)!.GetValue(controller)!).Stop();
        var assembler=(QuestionAssembler)type.GetField("questions",flags)!.GetValue(controller)!;
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket);
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        type.GetField("clients",flags)!.SetValue(controller,new Dictionary<AudioSource,LiveTranscriptionClient>{{AudioSource.Interviewer,client}});
        type.GetProperty("IsListening")!.SetValue(controller,true);controller.Settings.AutoAnswer=false;
        assembler.Accept(new TranscriptTurn("first",AudioSource.Interviewer,"Describe a migration",true,DateTimeOffset.UtcNow.AddSeconds(-2)));
        client.Enqueue(TranscriptionLifecycleTests.Speech());client.Enqueue(TranscriptionLifecycleTests.Speech());
        for(var i=0;i<5;i++)client.Enqueue(new byte[4800]);
        await socket.WaitForAppendsAsync(7);
        type.GetMethod("CheckQuestion",flags)!.Invoke(controller,null);
        Assert.Equal("Your next interview question will appear here.",controller.Question);
        var received=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Event+=json=>{if(!json.Contains("transcription.completed"))return;
            var dispatcher=(Dispatcher)type.GetField("dispatcher",flags)!.GetValue(controller)!;
            dispatcher.BeginInvoke(()=>{assembler.Accept(new TranscriptTurn("second",AudioSource.Interviewer,"Include a rollback plan",true,DateTimeOffset.UtcNow));received.SetResult();});};
        client.Enqueue(new byte[4800]);client.Enqueue(new byte[4800]);
        var id=await socket.Commits.Reader.ReadAsync();socket.Complete(id,"Include a rollback plan");await received.Task;
        await Task.Delay(750);type.GetMethod("CheckQuestion",flags)!.Invoke(controller,null);
        Assert.Equal("Describe a migration Include a rollback plan",controller.Question);
    });
    [Fact] public Task DeletingStreamingAnswerDoesNotResurrectItOnDisk()=>OnDispatcher(async controller=>
    {
        controller.Settings.SaveHistory=true;var handler=new DelayedHandler();using var http=new HttpClient(handler);
        controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        var answering=controller.AnswerAsync("Describe a migration",false);await handler.Requested.Task;
        await controller.DeleteHistoryAsync(controller.History.Single());handler.Release.SetResult();await answering;
        Assert.Empty(controller.History);Assert.Empty(await new SessionStore(controller.DataPath).ReadAsync());
    });
    [Fact] public Task ExplicitImageAnswerWaitsForPendingScreenshot()=>OnDispatcher(async controller=>
    {
        var capture=new TaskCompletionSource<ScreenshotItem>();var handler=new Handler();using var http=new HttpClient(handler);
        controller.LunaFactory=s=>new LunaClient(s,"test-key",http);controller.CaptureScreenshot=()=>capture.Task;
        var taking=controller.CaptureAsync();controller.ManualText="Explain this screenshot";var answering=controller.SubmitImagesAsync();
        capture.SetResult(Image());await taking;await answering;
        Assert.Contains("input_image",handler.Body);Assert.Empty(controller.Screenshots);Assert.Equal("ready",controller.Answer);
    });
    [Fact] public Task FailedAnswerPreservesScreenshotForRetry()=>OnDispatcher(async controller=>
    {
        using var http=new HttpClient(new Handler{Status=HttpStatusCode.Unauthorized});controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.AddImage(Image());await Assert.ThrowsAsync<CloudServiceException>(controller.SubmitImagesAsync);Assert.Single(controller.Screenshots);
    });
    [Fact] public Task CaptureCanRetryAfterAttachmentLimit()=>OnDispatcher(async controller=>
    {
        controller.CaptureScreenshot=()=>Task.FromResult(Image());for(var i=0;i<50;i++)controller.AddImage(Image());
        await Assert.ThrowsAsync<ArgumentException>(controller.CaptureAsync);controller.Screenshots.Clear();await controller.CaptureAsync();Assert.Single(controller.Screenshots);
    });
    [Fact] public Task FiftyImagesAreSubmittedTogetherAndClearedAfterSuccess()=>OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        for(var i=0;i<50;i++)controller.AddImage(Image());
        Assert.Throws<ArgumentException>(()=>controller.AddImage(Image()));Assert.Equal(50,controller.Screenshots.Count);
        await controller.SubmitImagesAsync();
        using var payload=System.Text.Json.JsonDocument.Parse(handler.Body);
        Assert.Equal(50,payload.RootElement.GetProperty("input")[0].GetProperty("content").EnumerateArray().Count(item=>item.GetProperty("type").GetString()=="input_image"));
        Assert.Empty(controller.Screenshots);
    });
    [Fact] public Task FailedFiftyImageAnswerKeepsEntireBatchForRetry()=>OnDispatcher(async controller=>
    {
        using var http=new HttpClient(new Handler{Status=HttpStatusCode.Unauthorized});controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        for(var i=0;i<50;i++)controller.AddImage(Image());
        await Assert.ThrowsAsync<CloudServiceException>(controller.SubmitImagesAsync);Assert.Equal(50,controller.Screenshots.Count);
    });
    private static ScreenshotItem Image()=>ScreenshotService.FromImage(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[16],8),"test.png");
    internal static Task OnDispatcher(Func<ProctalController,Task> run)
    {
        var result=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            var dispatcher=Dispatcher.CurrentDispatcher;SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(async()=>
            {
                var path=Path.Combine(Path.GetTempPath(),"proctal-test-"+Guid.NewGuid().ToString("N"));ProctalController? controller=null;
                try{controller=new ProctalController(dispatcher,path);await run(controller);result.SetResult();}
                catch(Exception error){result.SetException(error);}
                finally{if(controller is not null)await controller.ShutdownAsync();if(Directory.Exists(path))Directory.Delete(path,true);dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);}
            });
            Dispatcher.Run();
        });thread.SetApartmentState(ApartmentState.STA);thread.IsBackground=true;thread.Start();return result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
    private sealed class Handler:HttpMessageHandler
    {
        public HttpStatusCode Status=HttpStatusCode.OK;public string Body="";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Body=await request.Content!.ReadAsStringAsync(ct);return new(Status){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n",Encoding.UTF8,"text/event-stream")};
        }
    }
    private sealed class DelayedHandler:HttpMessageHandler
    {
        public readonly TaskCompletionSource Requested=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Requested.SetResult();await Release.Task.WaitAsync(ct);
            return new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n",Encoding.UTF8,"text/event-stream")};
        }
    }
}
