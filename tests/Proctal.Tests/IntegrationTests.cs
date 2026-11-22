using System.Net;
using System.Text;
using System.IO.Compression;
using NAudio.Wave;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class IntegrationTests
{
    [Theory]
    [InlineData("[{\"Question\":\"original fragment\", incomplete")]
    [InlineData("[null]")]
    [InlineData("[{\"Id\":\"id\",\"Question\":null,\"Answer\":\"answer\"}]")]
    public async Task CorruptHistoryIsPreservedAndFreshEntriesCanBeSaved(string broken)
    {
        var directory=Path.Combine(Path.GetTempPath(),"proctal-history-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory,"answers.json"),broken);var store=new SessionStore(directory);
            Assert.Empty(await store.ReadAsync());var backup=Assert.Single(Directory.GetFiles(directory,"answers.corrupt-*.json"));Assert.Equal(broken,await File.ReadAllTextAsync(backup));
            var answer=new AnswerRecord("new","New question","New answer",DateTimeOffset.UtcNow);await store.SaveAsync(answer);Assert.Equal(answer,Assert.Single(await store.ReadAsync()));Assert.Equal(broken,await File.ReadAllTextAsync(backup));
        }
        finally{Directory.Delete(directory,true);}
    }
    [Fact] public void Stereo48KhzConvertsToMono24KhzWithoutDoublingVolume()
    {
        var converter=new PcmConverter(new WaveFormat(48000,16,2));var input=new byte[19200];
        for(var i=0;i<input.Length;i+=4){input[i]=0x10;input[i+1]=0x27;}
        var output=converter.Convert(input,input.Length);
        Assert.InRange(output.Length,4500,5000);Assert.Equal(0,output.Length%2);
        Assert.InRange(BitConverter.ToInt16(output,output.Length/2/2*2),4500,5500);
    }
    [Fact] public async Task DocxImportPreservesParagraphBoundaries()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".docx");
        try{using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("word/document.xml").Open());await writer.WriteAsync("<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Azure engineer</w:t></w:r></w:p><w:p><w:r><w:t>Production migration owner</w:t></w:r></w:p></w:body></w:document>");}var doc=await new DocumentImporter().ImportAsync(path);Assert.Equal("Azure engineer\nProduction migration owner",doc.Text);}finally{File.Delete(path);}
    }
    [Fact] public async Task AzureClientSendsExpectedDeploymentAndStreamsAnswer()
    {
        var handler=new FixtureHandler((request,attempt)=>
        {
            Assert.Equal("https://your-resource.services.ai.azure.com/openai/v1/responses",request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-key",request.Headers.GetValues("api-key").Single());
            var body=request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();Assert.Contains("gpt-6-luna",body);
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"Useful answer\"}\n\ndata: {\"type\":\"response.completed\"}\n\n",Encoding.UTF8,"text/event-stream")};
        });
        var client=new LunaClient(new AzureSettings(),"test-key",new HttpClient(handler));var text=new StringBuilder();
        await foreach(var part in client.StreamAnswerAsync("Explain a rollback",[],"","General"))text.Append(part);
        Assert.Equal("Useful answer",text.ToString());
    }
    [Fact] public async Task ThrottlingRetriesBeforeStreamingAndKeepsKeyOutOfErrors()
    {
        var handler=new FixtureHandler((request,attempt)=>
        {
            if(attempt==1){var r=new HttpResponseMessage(HttpStatusCode.TooManyRequests);r.Headers.RetryAfter=new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);return r;}
            return new HttpResponseMessage(HttpStatusCode.Unauthorized){Content=new StringContent("DO NOT LOG THIS: test-key")};
        });
        var client=new LunaClient(new AzureSettings(),"test-key",new HttpClient(handler));
        var error=await Assert.ThrowsAsync<CloudServiceException>(async()=>{await foreach(var part in client.StreamAnswerAsync("question",[],"","General")){_=part;}});
        Assert.DoesNotContain("test-key",error.Message);Assert.Contains("key",error.Message,StringComparison.OrdinalIgnoreCase);Assert.Equal(2,handler.Attempts);
    }
    private sealed class FixtureHandler(Func<HttpRequestMessage,int,HttpResponseMessage> response):HttpMessageHandler
    {
        public int Attempts {get;private set;}
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(response(request,++Attempts));
    }
}
