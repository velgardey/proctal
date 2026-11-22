using System.Reflection;
using System.Net;
using System.Text.Json;
using Proctal.Core;
using Proctal.Desktop;
using Xunit;
namespace Proctal.Tests;
public sealed class OverlayWorkflowTests
{
    [Fact] public Task PracticeLeavesHistoryAndDisplaysItsNewQuestion()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new PracticeHandler{Requests=1};using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test",http);
        await controller.AnswerAsync("first",false);await controller.AnswerAsync("latest",false);controller.BrowseHistory(-1);Assert.True(controller.IsBrowsingHistory);
        await controller.NextPracticeQuestionAsync();Assert.False(controller.IsBrowsingHistory);Assert.Equal("practice question",controller.DisplayQuestion);Assert.Equal(controller.Answer,controller.DisplayAnswer);
    });
    [Fact] public Task SetupIsOnlyRequiredUntilAValidConfigurationIsSaved()=>ControllerTests.OnDispatcher(async controller=>
    {
        var configured=typeof(ProctalController).GetProperty("HasSavedConfiguration");Assert.NotNull(configured);
        Assert.False((bool)configured.GetValue(controller)!);controller.SaveSettings("");Assert.True((bool)configured.GetValue(controller)!);
        var restored=new ProctalController(System.Windows.Threading.Dispatcher.CurrentDispatcher,Path.GetDirectoryName(controller.DataPath));
        try{Assert.True((bool)configured.GetValue(restored)!);using var json=JsonDocument.Parse(JsonSerializer.Serialize(restored.Settings));Assert.True(json.RootElement.GetProperty("LaunchOverlayOnStartup").GetBoolean());}
        finally{await restored.ShutdownAsync();}
    });
    [Fact] public Task PartialAndFinalCaptionsReconcileThroughTheController()=>ControllerTests.OnDispatcher(controller=>
    {
        var accept=typeof(ProctalController).GetMethod("AcceptTranscriptionEvent",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(accept);
        accept.Invoke(controller,new object[]{"{\"type\":\"conversation.item.input_audio_transcription.delta\",\"item_id\":\"one\",\"delta\":\"Explain a hash\"}",AudioSource.Interviewer});
        Assert.Contains("Interviewer (live): Explain a hash",controller.Transcript);
        accept.Invoke(controller,new object[]{"{\"type\":\"conversation.item.input_audio_transcription.completed\",\"item_id\":\"one\",\"transcript\":\"Explain a hash map.\"}",AudioSource.Interviewer});
        Assert.Contains("Interviewer: Explain a hash map.",controller.Transcript);Assert.DoesNotContain("(live)",controller.Transcript);
        accept.Invoke(controller,new object[]{"{\"type\":\"conversation.item.input_audio_transcription.completed\",\"item_id\":\"two\",\"transcript\":\"I would use a dictionary.\"}",AudioSource.Candidate});
        Assert.Contains("You: I would use a dictionary.",controller.Transcript);Assert.Equal("Explain a hash map.",controller.Question);
        return Task.CompletedTask;
    });
    [Fact] public Task PracticeQuestionDoesNotLeaveAnsweringStateStuck()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new PracticeHandler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test",http);
        var old=controller.AnswerAsync("old question",false);await handler.Started.Task;
        await controller.NextPracticeQuestionAsync();await old;
        Assert.False(controller.IsAnswering);Assert.Equal("practice question",controller.Question);
    });
    [Fact] public Task DeletingViewedHistoryReturnsOverlayToLiveAnswer()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new PracticeHandler{Requests=1};using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test",http);
        await controller.AnswerAsync("first",false);await controller.AnswerAsync("latest",false);controller.BrowseHistory(-1);
        Assert.True(controller.IsBrowsingHistory);await controller.DeleteHistoryAsync(controller.History.Last());
        Assert.False(controller.IsBrowsingHistory);Assert.Equal("latest",controller.DisplayQuestion);
    });
    private sealed class PracticeHandler:HttpMessageHandler
    {
        public int Requests;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if(++Requests==1){Started.SetResult();await Task.Delay(Timeout.Infinite,ct);}
            return new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"practice question\"}\n\ndata: {\"type\":\"response.completed\"}\n\n")};
        }
    }
}
