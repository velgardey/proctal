using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Proctal.Core;
using Proctal.Desktop;
using Proctal.Desktop.Platform;
using Xunit;
namespace Proctal.Tests;
public sealed class QualityOfLifeTests
{
    [Theory] [InlineData(true)] [InlineData(false)] public Task StreamingHistoryIsCheckpointedOnlyWhenEnabled(bool saveHistory)=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new CheckpointHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);c.Settings.SaveHistory=saveHistory;
        var answering=c.AnswerAsync("Explain joins",false);
        try
        {
            for(var i=0;i<50&&c.Answer.Length==0;i++)await Task.Delay(10);
            Assert.Contains("Partial explanation",c.Answer);
            var stored=await new SessionStore(c.DataPath).ReadAsync();
            if(saveHistory){var record=Assert.Single(stored);Assert.StartsWith("[Partial]",record.Question);Assert.Contains("Partial explanation",record.Answer);}
            else Assert.Empty(stored);
        }
        finally{handler.Stream.Release.TrySetResult();await answering;}
        if(saveHistory){var final=Assert.Single(await new SessionStore(c.DataPath).ReadAsync());Assert.Equal("Explain joins",final.Question);}
    });
    [Fact] public Task SupersededStreamIsLabelledPartialEvenIfTheTransportIgnoresCancellation()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new CheckpointHandler();handler.Stream.IgnoreCancellation=true;using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);
        var older=c.AnswerAsync("First question",false);
        for(var i=0;i<50&&c.Answer.Length==0;i++)await Task.Delay(10);
        await c.AnswerAsync("Replacement question",false);handler.Stream.Release.TrySetResult();await older;
        Assert.StartsWith("[Partial]",c.History.Single(h=>h.Question.Contains("First question",StringComparison.Ordinal)).Question);Assert.Equal("Replacement question",c.Question);Assert.Equal("ready",c.Answer);
    });
    [Fact] public Task NewCompletedSpeechTakesPriorityOverOldUnfinishedSpeech()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"old","Old incomplete fragment",false);Hear(c,"new","Explain a transaction.");await Task.Delay(750);typeof(ProctalController).GetMethod("CheckQuestion",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null);
        await c.StopListeningAsync();await c.SubmitAsync();Assert.Equal("Explain a transaction.",c.Question);
    });
    [Fact] public Task FinalContinuationDuringPauseIsKeptInTheAnswerQuestion()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"one","Describe a migration.");_=System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(()=>Hear(c,"two","Include rollback."),System.Windows.Threading.DispatcherPriority.Background);
        await c.StopListeningAsync();await c.SubmitAsync();Assert.Equal("Describe a migration. Include rollback.",c.Question);
    });
    [Fact] public Task AutomaticQuestionWaitsForTheCurrentAnswerInsteadOfBeingLost()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler{Hold=true};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);
        c.ManualText="Current question";var first=c.SubmitAsync();await handler.Started.Task;Hear(c,"next","How do transactions work?");await Task.Delay(750);
        typeof(ProctalController).GetMethod("CheckQuestion",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null);Assert.Equal("Current question",c.DisplayQuestion);
        handler.Release.TrySetResult();await first;for(var i=0;i<40&&handler.Calls<2;i++)await Task.Delay(10);
        Assert.Equal(2,handler.Calls);Assert.Equal("How do transactions work?",c.Question);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,false);
    });
    [Fact] public Task RepeatingSpeechAnswerKeepsTheWholeQuestionIncludingItsPartialContinuation()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"one","Describe a migration.");Hear(c,"two","Include rollback",false);await c.SubmitAsync();await c.StopListeningAsync();await c.SubmitAsync();Assert.Equal("Describe a migration. Include rollback",c.Question);
        Hear(c,"two","Include rollback.");await c.SubmitAsync();Assert.Equal("Describe a migration. Include rollback.",c.Question);
    });
    [Fact] public Task AutomaticNewQuestionDoesNotIncludeAnAlreadyAnsweredSpeechFragment()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);
        Hear(c,"one","How does an index work?");await c.SubmitAsync();Hear(c,"two","What is a transaction?");await Task.Delay(750);
        typeof(ProctalController).GetMethod("CheckQuestion",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null);for(var i=0;i<40&&handler.Calls<2;i++)await Task.Delay(10);
        Assert.Equal(2,handler.Calls);Assert.Equal("What is a transaction?",c.Question);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,false);
    });
    [Fact] public Task ContinuedPartialSpeechRefreshesTheWholeReadyQuestion()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"one","Describe a migration.");Hear(c,"two","Include rollback",false);await c.SubmitAsync();Hear(c,"two"," and monitoring",false);await c.SubmitAsync();
        Assert.Equal("Describe a migration. Include rollback and monitoring",c.Question);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,false);
    });
    [Fact] public Task ManualPartialAnswerIsNotRestartedByItsFinalTranscript()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);
        Hear(c,"one","How does a hash map work",false);await c.SubmitAsync();Hear(c,"one","How does a hash map work?");
        await Task.Delay(750);typeof(ProctalController).GetMethod("CheckQuestion",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null);Assert.Equal(1,handler.Calls);
        typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,false);
    });
    [Fact] public Task PausingKeepsTheCompleteQuestionForManualAnswer()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"one","Describe a migration.");Hear(c,"two","Include rollback.");await c.StopListeningAsync();await c.SubmitAsync();Assert.Equal("Describe a migration. Include rollback.",c.Question);
    });
    [Fact] public Task PracticeDoesNotAnswerStaleInterviewSpeech()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);
        Hear(c,"old","Old incomplete question",false);await c.NextPracticeQuestionAsync();await c.SubmitAsync();Assert.Equal("ready",c.Question);
    });
    [Fact] public void ImageAnswerHasItsOwnVisibleOverlayShortcut()
    {
        Assert.Equal(ShortcutAction.AnswerImages,new ShortcutPolicy().Accept(0x53,true,false,ctrl:true,alt:true));Assert.Equal(ShortcutAction.None,new ShortcutPolicy().Accept(0x53,true,false,ctrl:true,alt:true,overlayVisible:false));
    }
    internal static void Hear(ProctalController controller,string id,string text,bool final=true,AudioSource source=AudioSource.Interviewer)
    {
        var json=System.Text.Json.JsonSerializer.Serialize(final?new{type="conversation.item.input_audio_transcription.completed",item_id=id,transcript=text,delta=""}:new{type="conversation.item.input_audio_transcription.delta",item_id=id,transcript="",delta=text});
        typeof(ProctalController).GetMethod("AcceptTranscriptionEvent",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(controller,new object[]{json,source});
    }
    private sealed class CheckpointHandler:HttpMessageHandler
    {
        public readonly CheckpointStream Stream=new();private int requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=Interlocked.Increment(ref requests)==1?new StreamContent(Stream):new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n")});
    }
    private sealed class CheckpointStream:Stream
    {
        public readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);public bool IgnoreCancellation;private int read;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)
        {
            var part=Interlocked.Increment(ref read);if(part>2)return 0;
            if(part==2)await Release.Task.WaitAsync(IgnoreCancellation?CancellationToken.None:ct);
            var text=part==1?"data: {\"type\":\"response.output_text.delta\",\"delta\":\"Partial explanation\"}\n\n":"data: {\"type\":\"response.completed\"}\n\n";
            return Encoding.UTF8.GetBytes(text,buffer.Span);
        }
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken ct)=>ReadAsync(buffer.AsMemory(offset,count),ct).AsTask();
        public override bool CanRead=>true;public override bool CanWrite=>false;public override bool CanSeek=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException();public override void Flush()=>throw new NotSupportedException();public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
    }
    [Fact] public Task TranscriptionVocabularyPrioritizesTechnicalPhrasesAndJobContext()=>ControllerTests.OnDispatcher(c=>
    {
        c.Settings.Role="SQL Engineer";c.Settings.JobDescription="Use ONNX and PyTorch.";
        c.Documents.Add(new ContextDocument("vocabulary","Resume",string.Join(" ",Enumerable.Range(0,60).Select(i=>"Company"+i))));
        var hints=((IEnumerable<string>)typeof(ProctalController).GetMethod("Vocabulary",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null)!).ToArray();
        Assert.Contains("sigmoid",hints);Assert.Contains("hash map",hints);Assert.Contains("left join",hints);Assert.Contains("ONNX",hints);Assert.Contains("PyTorch",hints);
        Assert.True(hints.Length<=40);Assert.Equal(hints.Length,hints.Distinct(StringComparer.OrdinalIgnoreCase).Count());return Task.CompletedTask;
    });
    [Fact] public Task SpeechAnswerDoesNotWaitForOrConsumeImages()=>ControllerTests.OnDispatcher(async c=>
    {
        var capture=new TaskCompletionSource<ScreenshotItem>();using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);c.CaptureScreenshot=()=>capture.Task;
        Hear(c,"one","Explain a hash map",false);var taking=c.CaptureAsync();
        try{await c.SubmitAsync().WaitAsync(TimeSpan.FromSeconds(1));Assert.False(taking.IsCompleted);Assert.Contains("Explain a hash map",handler.Body);Assert.DoesNotContain("input_image",handler.Body);}
        finally{capture.TrySetResult(Image());await taking;}
        Assert.Single(c.Screenshots);Hear(c,"two","Describe an index.");await c.SubmitAsync();Assert.DoesNotContain("input_image",handler.Body);Assert.Single(c.Screenshots);
    });
    [Fact] public Task SpeechAnswerIncludesHeardContinuationAndExcludesCandidateSpeech()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        Hear(c,"one","Describe a migration.");Hear(c,"two","Include a rollback plan",false);Hear(c,"mine","Do not use this as the question",false,AudioSource.Candidate);
        await c.SubmitAsync();Assert.Equal("Describe a migration. Include a rollback plan",c.Question);
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsListening))!.SetValue(c,false);
    });
    [Fact] public Task DuplicateAnswerKeepsTheExistingRequestAndNewSpeechKeepsItsQuestionStable()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler{Hold=true};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);
        c.ManualText="first question";var answering=c.SubmitAsync();await handler.Started.Task;
        c.ManualText="first question";await c.SubmitAsync();Assert.Equal(1,handler.Calls);
        Hear(c,"new","A different next question.");Assert.Equal("first question",c.DisplayQuestion);
        handler.Release.TrySetResult();await answering;Assert.Equal("ready",c.Answer);
    });
    [Fact] public Task F11StartsImmediatelyFromLiveSpeechWhileAudioIsPending()=>ControllerTests.OnDispatcher(async c=>
    {
        using var socket=new TranscriptionSocket();await using var speech=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        await speech.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        speech.Enqueue(TranscriptionLifecycleTests.Speech());speech.Enqueue(TranscriptionLifecycleTests.Speech());await socket.TwoAppends.Task;
        typeof(ProctalController).GetField("clients",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(c,new Dictionary<AudioSource,LiveTranscriptionClient>{{AudioSource.Interviewer,speech}});
        typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,true);c.Settings.AutoAnswer=false;
        using var handler=new ResponseHandler{Hold=true};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test-key",http);
        Hear(c,"question","Explain a hash map.");Hear(c,"continuation","Include duplicate values",false);Hear(c,"candidate","I would sort the array",false,AudioSource.Candidate);
        Assert.Equal(ShortcutAction.Answer,new ShortcutPolicy().Accept(0x7A,true,false));
        var answer=c.SubmitAsync();
        try
        {
            Assert.True(handler.Started.Task.IsCompleted);Assert.True(speech.HasPendingAudio);
            Assert.Equal("Explain a hash map. Include duplicate values",c.Question);Assert.DoesNotContain("input_image",handler.Body);
            Hear(c,"continuation","Include duplicate values and negative numbers.");Assert.Equal("Explain a hash map. Include duplicate values",c.Question);
        }
        finally{handler.Release.TrySetResult();await answer;typeof(ProctalController).GetProperty(nameof(c.IsListening))!.SetValue(c,false);}
    });
    [Fact] public Task ClearingImagesAlsoDiscardsAnUnfinishedCapture()=>ControllerTests.OnDispatcher(async c=>
    {
        var pending=new TaskCompletionSource<ScreenshotItem>();c.CaptureScreenshot=()=>pending.Task;var taking=c.CaptureAsync();
        var clear=typeof(ProctalController).GetMethod("ClearImages");Assert.NotNull(clear);clear.Invoke(c,null);pending.SetResult(Image());await taking;Assert.Empty(c.Screenshots);
    });
    [Fact] public Task F11UsesStoredMicrophoneTextBeforeAnyInterviewerSpeech()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler{Hold=true};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test-key",http);
        Assert.False(c.CanSubmitText);Hear(c,"blank","   ",false,AudioSource.Candidate);Assert.False(c.CanSubmitText);
        Hear(c,"candidate","Explain how a hash map finds duplicate values",false,AudioSource.Candidate);
        Assert.True(c.CanSubmitText);Assert.Contains("stored",c.AnswerInputHint,StringComparison.OrdinalIgnoreCase);
        var answer=c.SubmitAsync();
        try
        {
            Assert.True(handler.Started.Task.IsCompleted);Assert.Equal("Explain how a hash map finds duplicate values",c.Question);
            Assert.Contains("Candidate: Explain how a hash map finds duplicate values",handler.Body);
            Hear(c,"later","Words recorded after F11",false,AudioSource.Candidate);Assert.DoesNotContain("Words recorded after F11",handler.Body);
        }
        finally{handler.Release.TrySetResult();await answer;}
    });
    [Fact] public Task F11IncludesTheWholeRetainedTranscriptSnapshot()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler();using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test-key",http);
        for(var i=0;i<14;i++)Hear(c,"turn-"+i,"Stored detail "+i,true,i%2==0?AudioSource.Interviewer:AudioSource.Candidate);
        typeof(ProctalController).GetField("speechSince",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(c,DateTimeOffset.UtcNow.AddMinutes(1));
        await c.SubmitAsync();
        Assert.Contains("TRANSCRIPT SNAPSHOT",handler.Body);Assert.Contains("Interviewer: Stored detail 0",handler.Body);Assert.Contains("Candidate: Stored detail 13",handler.Body);
    });
    [Fact] public Task NewStoredContextStartsANewF11RequestWhileTheQuestionIsUnchanged()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler{Hold=true};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test-key",http);
        Hear(c,"question","Explain a hash map.");Hear(c,"candidate","The input includes",false,AudioSource.Candidate);
        var first=c.SubmitAsync();await handler.Started.Task;var repeated=c.SubmitAsync();Task second=Task.CompletedTask;
        try
        {
            Assert.Equal(1,handler.Calls);Assert.True(repeated.IsCompleted);
            Hear(c,"candidate"," duplicate values",false,AudioSource.Candidate);second=c.SubmitAsync();
            Assert.Equal(2,handler.Calls);Assert.Contains("The input includes duplicate values",handler.Body);
        }
        finally{handler.Release.TrySetResult();await Task.WhenAll(first,repeated,second);}
    });
    [Fact] public Task ConnectionCanBeCancelledWhileItIsStarting()=>ControllerTests.OnDispatcher(c=>
    {
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsStarting))!.SetValue(c,true);Assert.True(c.CanToggleListen);Assert.Contains("Cancel",c.ListenButtonText);
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsStarting))!.SetValue(c,false);return Task.CompletedTask;
    });
    [Fact] public Task StreamingBurstKeepsFinalTextAndDoesNotNotifyForEveryToken()=>ControllerTests.OnDispatcher(async c=>
    {
        using var handler=new ResponseHandler{Chunks=200};using var http=new HttpClient(handler);c.LunaFactory=s=>new LunaClient(s,"test",http);
        var notifications=0;c.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(c.Answer))notifications++;};await c.AnswerAsync("Explain the approach",false);
        Assert.Equal(new string('x',200),c.Answer);Assert.True(notifications<60,$"Received {notifications} text updates for 200 immediately available chunks.");
    });
    private static ScreenshotItem Image()=>ScreenshotService.FromImage(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[16],8),"test.png");
    internal sealed class ResponseHandler:HttpMessageHandler
    {
        public bool Hold;public int Calls,Chunks;public string Body="";
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously),Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Calls++;Body=await request.Content!.ReadAsStringAsync(ct);Started.TrySetResult();if(Hold)await Release.Task.WaitAsync(ct);
            var events=Chunks==0?"data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\n":string.Concat(Enumerable.Repeat("data: {\"type\":\"response.output_text.delta\",\"delta\":\"x\"}\n\n",Chunks));
            return new(HttpStatusCode.OK){Content=new StringContent(events+"data: {\"type\":\"response.completed\"}\n\n",Encoding.UTF8,"text/event-stream")};
        }
    }
}
