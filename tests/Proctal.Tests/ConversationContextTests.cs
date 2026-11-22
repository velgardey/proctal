using System.Net;
using System.Text.Json;
using Proctal.Core;
using Proctal.Desktop;
using Proctal.Desktop.Platform;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;
namespace Proctal.Tests;
public sealed class ConversationContextTests
{
    [Fact] public Task LatestCompleteAnswerKeepsCodeAndTheFullQuestion()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        var question="QUESTION_START"+new string('q',2400)+"QUESTION_END";
        var answer="ANSWER_START"+new string('x',6000)+"\n```python\ndef crucial_loop():\n    return 42\n```\n"+new string('y',6000)+"ANSWER_END";
        handler.Answers.Enqueue(answer);await controller.AnswerAsync(question,false);await controller.AnswerAsync("Explain that loop",false);
        var recent=Recent(handler.Bodies.Last());Assert.Contains(question,recent);Assert.Contains(answer,recent);
    });
    [Fact] public Task VeryLargeSessionReferencesHaveALabelledInputBudget()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue(new string('x',200000)+"ANSWER_END");await controller.AnswerAsync("Huge answer",false);
        await controller.AnswerAsync("Explain it",false,reference:"REFERENCE_START"+new string('r',100000)+"REFERENCE_END");
        var recent=Recent(handler.Bodies.Last());Assert.InRange(recent.Length,1,160000);Assert.Contains("trimmed",recent,StringComparison.OrdinalIgnoreCase);Assert.Contains("ANSWER_END",recent);Assert.Contains("REFERENCE_END",recent);
    });
    [Fact] public Task ExplicitFollowUpUsesTheWholeSelectedHistoryAnswer()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        var answer="HISTORY_START"+new string('x',15000)+"HISTORY_END";
        controller.History.Add(new("loaded","Selected old problem",answer,DateTimeOffset.UtcNow.AddDays(-1)));controller.BrowseHistory(-1);
        await controller.FollowUpAsync("explain");Assert.Contains(answer,Recent(handler.Bodies.Last()));
    });
    [Fact] public Task FollowOnAnswerReceivesEarlierQuestionAndAnswerWithoutCurrentEntry()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue("Use a C# dictionary keyed by customer ID.");await controller.AnswerAsync("Design a customer lookup",false);
        await controller.AnswerAsync("What about duplicates?",false);var recent=Recent(handler.Bodies.Last());
        Assert.Contains("Design a customer lookup",recent);Assert.Contains("Use a C# dictionary keyed by customer ID.",recent);Assert.DoesNotContain("What about duplicates?",recent);
        Assert.False(controller.Settings.SaveHistory);Assert.False(File.Exists(Path.Combine(controller.DataPath,"answers.json")));
        using var json=JsonDocument.Parse(handler.Bodies.Last());Assert.Equal("none",json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());Assert.False(json.RootElement.GetProperty("store").GetBoolean());
    });
    [Fact] public Task EarlierSessionExchangesStayAvailableAfterThreeNewAnswers()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        controller.History.Add(new("loaded","OLDER_ASSESSMENT","older answer",DateTimeOffset.UtcNow.AddDays(-1)));
        for(var i=1;i<=5;i++){handler.Answers.Enqueue("ANSWER_"+i);await controller.AnswerAsync("QUESTION_"+i,false);}
        await controller.AnswerAsync("Follow up",false);var recent=Recent(handler.Bodies.Last());
        Assert.DoesNotContain("OLDER_ASSESSMENT",recent);
        foreach(var i in new[]{1,2,3,4,5}){Assert.Contains("QUESTION_"+i,recent);Assert.Contains("ANSWER_"+i,recent);}
        Assert.True(recent.IndexOf("QUESTION_3",StringComparison.Ordinal)<recent.IndexOf("QUESTION_4",StringComparison.Ordinal));Assert.True(recent.IndexOf("QUESTION_4",StringComparison.Ordinal)<recent.IndexOf("QUESTION_5",StringComparison.Ordinal));
    });
    [Fact] public Task RelevantCodeFromAnEarlierSessionAnswerIsRetrieved()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue(new string('x',3000)+"\n```python\ndef sigmoid(x):\n    return 1 / (1 + math.exp(-x))\n```\n"+new string('y',3000));
        await controller.AnswerAsync("Implement sigmoid",false);
        for(var i=0;i<4;i++)await controller.AnswerAsync("Unrelated question "+i,false);
        await controller.AnswerAsync("Explain the earlier sigmoid function",false);
        Assert.Contains("return 1 / (1 + math.exp(-x))",Recent(handler.Bodies.Last()));
    });
    [Fact] public Task ExplicitFollowUpDoesNotDuplicateItsReferencedAnswer()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue("UNIQUE_REFERENCE_CODE");await controller.AnswerAsync("Explain dictionaries",false);await controller.FollowUpAsync("explain");
        var recent=Recent(handler.Bodies.Last());Assert.Equal(1,recent.Split("UNIQUE_REFERENCE_CODE",StringSplitOptions.None).Length-1);
    });
    [Fact] public Task ImageAnswersSnapshotBothSpeakersBeyondTheLastTenCaptions()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        QualityOfLifeTests.Hear(controller,"constraint","Important constraint: input is sorted.");
        for(var i=0;i<12;i++)QualityOfLifeTests.Hear(controller,"candidate-"+i,"Candidate clarification "+i,source:AudioSource.Candidate);
        controller.AddImage(ScreenshotService.FromImage(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[16],8),"test.png"));await controller.SubmitImagesAsync();
        var recent=Recent(handler.Bodies.Last());Assert.Contains("input is sorted",recent);Assert.Contains("Candidate clarification 11",recent);
    });
    [Fact] public Task DeletedAndIncompleteAnswersDoNotEnterConversationContext()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue("DELETE_ME");await controller.AnswerAsync("Deleted question",false);await controller.DeleteHistoryAsync(controller.History.Single());
        handler.Incomplete=true;handler.Answers.Enqueue("PARTIAL_ONLY");await Assert.ThrowsAsync<InvalidDataException>(()=>controller.AnswerAsync("Broken question",false));
        handler.Incomplete=false;await controller.AnswerAsync("New question",false);var recent=Recent(handler.Bodies.Last());Assert.DoesNotContain("DELETE_ME",recent);Assert.DoesNotContain("PARTIAL_ONLY",recent);Assert.DoesNotContain("Broken question",recent);
    });
    [Fact] public Task OlderContextIsBoundedWhileTheLatestPairRemainsComplete()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        for(var i=0;i<3;i++){handler.Answers.Enqueue("ANSWER_START_"+i+new string('x',10000)+"ANSWER_END_"+i);await controller.AnswerAsync("QUESTION_START_"+i+new string('q',4000)+"QUESTION_END_"+i,false);}
        await controller.AnswerAsync("Follow up",false);var recent=Recent(handler.Bodies.Last());Assert.InRange(recent.Length,1,28000);
        Assert.Contains("ANSWER_START_2"+new string('x',10000)+"ANSWER_END_2",recent);
        for(var i=0;i<3;i++){Assert.Contains("ANSWER_START_"+i,recent);Assert.Contains("ANSWER_END_"+i,recent);Assert.Contains("QUESTION_START_"+i,recent);Assert.Contains("QUESTION_END_"+i,recent);}Assert.Contains("trimmed",recent,StringComparison.OrdinalIgnoreCase);
    });
    [Fact] public Task ImageAndDeepAnswersReceiveTheSameSessionConversation()=>ControllerTests.OnDispatcher(async controller=>
    {
        var handler=new Handler();using var http=new HttpClient(handler);controller.LunaFactory=s=>new LunaClient(s,"test-key",http);
        handler.Answers.Enqueue("BASELINE_CONTEXT");await controller.AnswerAsync("Original problem",false);
        controller.AddImage(ScreenshotService.FromImage(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[16],8),"test.png"));controller.ManualText="Use this diagram";await controller.SubmitImagesAsync();
        Assert.Contains("BASELINE_CONTEXT",Recent(handler.Bodies.Last()));Assert.Contains("input_image",handler.Bodies.Last());
        controller.ManualText="Optimize it";await controller.SubmitDeepAsync();Assert.Contains("BASELINE_CONTEXT",Recent(handler.Bodies.Last()));
        using var json=JsonDocument.Parse(handler.Bodies.Last());Assert.Equal("xhigh",json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
    });
    private static string Recent(string body)
    {
        using var json=JsonDocument.Parse(body);var input=json.RootElement.GetProperty("input");var text=input.ValueKind==JsonValueKind.String?input.GetString()!:input[0].GetProperty("content")[0].GetProperty("text").GetString()!;
        const string start="RECENT CONVERSATION\n";var from=text.IndexOf(start,StringComparison.Ordinal)+start.Length;var to=text.LastIndexOf("\n\nCURRENT QUESTION\n",StringComparison.Ordinal);return text[from..to];
    }
    private sealed class Handler:HttpMessageHandler
    {
        public readonly List<string> Bodies=[];public readonly Queue<string> Answers=[];public bool Incomplete;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsStringAsync(ct));var answer=Answers.Count>0?Answers.Dequeue():"ready";
            var delta=JsonSerializer.Serialize(new{type="response.output_text.delta",delta=answer});return new(HttpStatusCode.OK){Content=new StringContent("data: "+delta+"\n\ndata: {\"type\":\"response."+(Incomplete?"incomplete":"completed")+"\"}\n\n")};
        }
    }
}
