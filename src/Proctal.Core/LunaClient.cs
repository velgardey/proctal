using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
namespace Proctal.Core;
public sealed class CloudServiceException(string message):Exception(message);
public sealed class LunaClient
{
    private readonly AzureSettings settings;
    private readonly string key;
    private readonly HttpClient http;
    public event Action<string>? ProcessingTier;
    public event Action<string>? RequestStatus;
    private static readonly HttpClient Shared=new(new SocketsHttpHandler{AllowAutoRedirect=false,UseCookies=false,PooledConnectionLifetime=TimeSpan.FromMinutes(5)}){Timeout=Timeout.InfiniteTimeSpan};
    public LunaClient(AzureSettings settings,string key,HttpClient? http=null){this.settings=settings.Snapshot();this.key=key;this.http=http??Shared;}
    public async IAsyncEnumerable<string> StreamAnswerAsync(string question,IEnumerable<ContextPassage> context,string recent,string mode,[EnumeratorCancellation]CancellationToken ct=default,IReadOnlyList<ImageAttachment>? images=null)
    {
        if(string.IsNullOrWhiteSpace(key))throw new CloudServiceException("Add an Azure key in Setup, or set AZURE_OPENAI_API_KEY before launching.");
        if(string.IsNullOrWhiteSpace(question))throw new ArgumentException("Enter a question first.");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(settings.ReasoningEffort is "high" or "xhigh"?TimeSpan.FromMinutes(5):TimeSpan.FromSeconds(90));
        var fast=settings.FastProcessing;
        string Payload(bool priority)=>ResponsesProtocol.BuildRequest(settings.AnswerDeployment,question,context,recent,mode,images,priority,settings.ReasoningEffort,settings.CodeFirst,settings.CodingLanguage,settings.Instructions,settings.ResponseStructure);
        var payload=Payload(fast);
        HttpResponseMessage? response=null;
        try
        {
            for(var attempt=0;attempt<3;attempt++)
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(new Uri(AzureSettings.NormalizeEndpoint(settings.Endpoint)),"responses")){Content=new StringContent(payload,Encoding.UTF8,"application/json")};
                request.Headers.Add("api-key",key);response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,timeout.Token);
                if(response.StatusCode==HttpStatusCode.BadRequest)
                {
                    var error=await response.Content.ReadAsStringAsync(timeout.Token);
                    if(settings.ReasoningEffort is "high" or "xhigh"&&(error.Contains("reasoning",StringComparison.OrdinalIgnoreCase)||error.Contains(settings.ReasoningEffort,StringComparison.OrdinalIgnoreCase)))
                        throw new CloudServiceException($"This Azure deployment rejected {settings.ReasoningEffort} reasoning. Check its model/version; F11 remains available for fast answers. Your inputs are retained.");
                    if(fast&&attempt<2&&error.Contains("service_tier",StringComparison.OrdinalIgnoreCase))
                    {
                        fast=false;payload=Payload(false);
                        ProcessingTier?.Invoke("Fast tier unsupported; using standard processing · reasoning "+settings.ReasoningEffort);response.Dispose();response=null;continue;
                    }
                }
                if(response.StatusCode!=HttpStatusCode.TooManyRequests||attempt==2)break;
                var retry=response.Headers.RetryAfter;
                var delay=retry?.Delta??(retry?.Date is {} when?when-DateTimeOffset.UtcNow:TimeSpan.FromSeconds(attempt+1));
                delay=TimeSpan.FromMilliseconds(Math.Clamp(delay.TotalMilliseconds,0,20000));RequestStatus?.Invoke($"Azure is busy; retrying in {Math.Ceiling(delay.TotalSeconds):0} seconds…");response.Dispose();response=null;await Task.Delay(delay,timeout.Token);
            }
            if(response is null)throw new CloudServiceException("Azure could not be reached.");
            if(!response.IsSuccessStatusCode)throw new CloudServiceException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden=>"Azure rejected the key or access permissions. Check Setup.",
                HttpStatusCode.NotFound=>"Azure could not find this deployment. Check the deployment name in Setup.",
                HttpStatusCode.TooManyRequests=>"Azure is throttling requests. Pause briefly and retry; check the deployment quota.",
                HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect=>"Azure redirected the request. Check the resource endpoint in Setup; credentials were not forwarded.",
                _=>$"Azure returned {(int)response.StatusCode}. Check the deployment and retry."
            });
            using var stream=await response.Content.ReadAsStreamAsync(timeout.Token);
            await foreach(var part in ResponsesProtocol.ReadAsync(stream,timeout.Token,tier=>ProcessingTier?.Invoke(tier)))
            {
                // Bound a stalled stream without cutting off an active, longer answer.
                timeout.CancelAfter(TimeSpan.FromSeconds(90));yield return part;
            }
        }
        finally{response?.Dispose();}
    }
}
