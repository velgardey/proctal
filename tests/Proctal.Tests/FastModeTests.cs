using System.Net;
using System.Text;
using System.Text.Json;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class FastModeTests
{
    [Fact] public void MockInterviewerAsksOneQuestionWithoutCoachAnswer()
    {
        using var json=JsonDocument.Parse(ResponsesProtocol.BuildRequest("gpt-6-luna","Ask a practice question",[],"","Mock interviewer"));
        var instructions=json.RootElement.GetProperty("instructions").GetString()!;
        Assert.Contains("exactly one interview question",instructions);
        Assert.DoesNotContain("3 concise",instructions);
    }
    [Fact] public async Task UnsupportedFastTierFallsBackAndReportsActualTier()
    {
        var handler=new Handler();using var http=new HttpClient(handler);var tiers=new List<string>();
        var client=new LunaClient(new AzureSettings(),"test-key",http);client.ProcessingTier+=tiers.Add;
        var text=new StringBuilder();await foreach(var part in client.StreamAnswerAsync("question",[],"","General"))text.Append(part);
        Assert.Equal("ready",text.ToString());Assert.Equal(2,handler.Payloads.Count);
        using var first=JsonDocument.Parse(handler.Payloads[0]);using var second=JsonDocument.Parse(handler.Payloads[1]);
        Assert.Equal("none",first.RootElement.GetProperty("reasoning").GetProperty("effort").GetString());
        Assert.Equal("priority",first.RootElement.GetProperty("service_tier").GetString());
        Assert.False(second.RootElement.TryGetProperty("service_tier",out _));Assert.Equal("default",tiers.Last());
    }
    [Fact] public async Task ClientUsesSettingsSnapshot()
    {
        var settings=new AzureSettings();var handler=new Handler{RejectFast=false};using var http=new HttpClient(handler);
        var client=new LunaClient(settings,"test-key",http);settings.AnswerDeployment="changed";
        await foreach(var _ in client.StreamAnswerAsync("question",[],"","General")){}
        using var payload=JsonDocument.Parse(handler.Payloads[0]);Assert.Equal("gpt-6-luna",payload.RootElement.GetProperty("model").GetString());
    }
    private sealed class Handler:HttpMessageHandler
    {
        public bool RejectFast=true;
        public List<string> Payloads {get;}=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Payloads.Add(await request.Content!.ReadAsStringAsync(ct));
            if(RejectFast&&Payloads.Count==1)return new(HttpStatusCode.BadRequest){Content=new StringContent("unsupported service_tier")};
            return new(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\",\"response\":{\"service_tier\":\"default\"}}\n\n")};
        }
    }
}
