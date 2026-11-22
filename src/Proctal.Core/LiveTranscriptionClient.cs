using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
namespace Proctal.Core;
public sealed class LiveTranscriptionClient:IAsyncDisposable
{
    private static readonly HttpMessageInvoker HandshakeTransport=new(new SocketsHttpHandler{AllowAutoRedirect=false,UseCookies=false,PooledConnectionLifetime=TimeSpan.FromMinutes(5)});
    internal static Task ConnectSocketAsync(ClientWebSocket client,Uri uri,CancellationToken ct)=>client.ConnectAsync(uri,HandshakeTransport,ct);
    private readonly WebSocket socket;
    private readonly Channel<byte[]> audio=Channel.CreateBounded<byte[]>(new BoundedChannelOptions(50){SingleReader=true,SingleWriter=false,FullMode=BoundedChannelFullMode.Wait});
    private readonly CancellationTokenSource lifetime=new();
    private readonly object stateGate=new();
    private readonly Dictionary<string,DateTimeOffset> pendingItems=[];
    private readonly HashSet<string> partialItems=[];
    private TaskCompletionSource commitsDrained=CompletedSignal();
    private int queuedFrames,awaitingCommits;
    private DateTimeOffset? awaitingSince;
    private readonly TimeSpan sendTimeout=TimeSpan.FromSeconds(10),transcriptionTimeout=TimeSpan.FromSeconds(60);
    private bool voiceActive;
    private Task<bool>? stopTask;
    private readonly TimeSpan drainTimeout;
    private Task? sender,receiver,monitor;
    private int stopped;
    public event Action<string>? Event;
    public event Action<string>? Fault;
    public LiveTranscriptionClient():this(new ClientWebSocket()){}
    internal LiveTranscriptionClient(WebSocket connectedSocket,TimeSpan? drainTimeout=null){socket=connectedSocket;this.drainTimeout=drainTimeout??TimeSpan.FromSeconds(4);}
    public bool HasPendingAudio {get{lock(stateGate)return Volatile.Read(ref queuedFrames)>0||voiceActive||awaitingCommits>0||pendingItems.Count>0||partialItems.Count>0;}}
    private static TaskCompletionSource CompletedSignal(){var signal=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);signal.SetResult();return signal;}
    private void SpeechState(bool active,bool commit)
    {
        lock(stateGate){if(commit){if(awaitingCommits==0){awaitingSince=DateTimeOffset.UtcNow;if(pendingItems.Count==0)commitsDrained=new(TaskCreationOptions.RunContinuationsAsynchronously);}awaitingCommits++;}voiceActive=active;}
    }
    private void TrackEvent(JsonElement e,string? type)
    {
        if(!e.TryGetProperty("item_id",out var item)||item.GetString() is not {Length:>0} id)return;
        lock(stateGate)
        {
            if(type=="input_audio_buffer.committed"){if(pendingItems.TryAdd(id,DateTimeOffset.UtcNow)&&awaitingCommits>0)awaitingCommits--;if(awaitingCommits==0)awaitingSince=null;}
            else if(type=="conversation.item.input_audio_transcription.delta"&&e.TryGetProperty("delta",out var delta)&&!string.IsNullOrWhiteSpace(delta.GetString()))partialItems.Add(id);
            else if(type is "conversation.item.input_audio_transcription.completed" or "conversation.item.input_audio_transcription.failed"){pendingItems.Remove(id);partialItems.Remove(id);}
            if(awaitingCommits==0&&pendingItems.Count==0)commitsDrained.TrySetResult();
        }
    }
    public async Task ConnectAsync(AzureSettings settings,string key,IEnumerable<string> vocabulary,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(key))throw new CloudServiceException("Configure an Azure key before listening.");
        if(socket is ClientWebSocket client){client.Options.SetRequestHeader("api-key",key);client.Options.KeepAliveInterval=TimeSpan.FromSeconds(20);client.Options.KeepAliveTimeout=TimeSpan.FromSeconds(20);}
        var endpoint=new Uri(AzureSettings.NormalizeEndpoint(settings.Endpoint));var uri=new UriBuilder(endpoint){Scheme="wss",Path="/openai/v1/realtime",Query="intent=transcription"}.Uri;
        using var handshake=CancellationTokenSource.CreateLinkedTokenSource(ct,lifetime.Token);handshake.CancelAfter(TimeSpan.FromSeconds(20));
        if(socket is ClientWebSocket connecting)await ConnectSocketAsync(connecting,uri,handshake.Token);
        await Send(TranscriptionProtocol.SessionUpdate(settings.TranscriptionDeployment,settings.Language,vocabulary),handshake.Token);
        while(true)
        {
            var message=await Receive(handshake.Token);using var parsed=JsonDocument.Parse(message);var type=parsed.RootElement.GetProperty("type").GetString();
            if(type=="error")throw new CloudServiceException("Azure rejected the transcription session. Check its deployment, model, region, and supported settings.");
            if(type=="session.updated")break;
        }
        sender=SendLoop();receiver=ReceiveLoop();monitor=Monitor();
    }
    public bool Enqueue(byte[] pcm)
    {
        if(Volatile.Read(ref stopped)>0)return false;
        Interlocked.Increment(ref queuedFrames);
        if(audio.Writer.TryWrite(pcm))return true;
        Interlocked.Decrement(ref queuedFrames);
        Fault?.Invoke("Audio buffering filled up. A capture gap occurred; restart listening.");return false;
    }
    private async Task SendLoop()
    {
        var vad=new VoiceActivity();var gap=new AudioGapClock();Task<bool>? available=null;var bufferedBytes=0;
        while(!lifetime.IsCancellationRequested)
        {
            byte[]? frame=null;var queued=false;
            if(audio.Reader.TryRead(out var captured)){frame=captured;queued=true;gap.Received(DateTimeOffset.UtcNow);}
            else
            {
                available??=audio.Reader.WaitToReadAsync(lifetime.Token).AsTask();
                if(await Task.WhenAny(available,Task.Delay(100,lifetime.Token))==available)
                {var canRead=await available;available=null;if(!canRead)break;continue;}
                frame=gap.Pad(DateTimeOffset.UtcNow,vad.Active);
            }
            if(frame is null)continue;
            var commit=vad.Accept(frame)||bufferedBytes+frame.Length>=25*48000;
            SpeechState(vad.Active,commit);
            // Speech activity segments audio; it must never discard quiet words or their onset.
            await Send(JsonSerializer.Serialize(new{type="input_audio_buffer.append",audio=Convert.ToBase64String(frame)}),lifetime.Token);bufferedBytes+=frame.Length;
            if(commit){await Send("{\"type\":\"input_audio_buffer.commit\"}",lifetime.Token);bufferedBytes=0;}
            if(queued)Interlocked.Decrement(ref queuedFrames);
        }
        SpeechState(false,false);
        if(bufferedBytes>0&&socket.State==WebSocketState.Open)
        {
            if(bufferedBytes<4800)await Send(JsonSerializer.Serialize(new{type="input_audio_buffer.append",audio=Convert.ToBase64String(new byte[4800-bufferedBytes])}),lifetime.Token);
            SpeechState(false,true);await Send("{\"type\":\"input_audio_buffer.commit\"}",lifetime.Token);
        }
    }
    private async Task ReceiveLoop()
    {
        while(!lifetime.IsCancellationRequested)
        {
            var message=await Receive(lifetime.Token);using var parsed=JsonDocument.Parse(message);var type=parsed.RootElement.GetProperty("type").GetString();
            if(type is "error" or "conversation.item.input_audio_transcription.failed")throw new CloudServiceException("Live transcription was interrupted. Check the connection and deployment quota, then restart.");
            Event?.Invoke(message);
            // Queue transcript callbacks before releasing the pending-audio gate.
            TrackEvent(parsed.RootElement,type);
        }
    }
    private async Task Monitor()
    {
        if(sender is null||receiver is null)return;
        try
        {
            while(!lifetime.IsCancellationRequested)
            {
                var first=await Task.WhenAny(sender,receiver,Task.Delay(500,lifetime.Token));
                if(first==sender||first==receiver){if(first.IsFaulted||first.IsCanceled)await lifetime.CancelAsync();await Task.WhenAll(sender,receiver);break;}
                lock(stateGate)if(Volatile.Read(ref stopped)==0&&((awaitingSince is {} since&&DateTimeOffset.UtcNow-since>transcriptionTimeout)||pendingItems.Values.Any(at=>DateTimeOffset.UtcNow-at>transcriptionTimeout)))throw new TimeoutException();
            }
        }
        catch(Exception)when(lifetime.IsCancellationRequested&&Volatile.Read(ref stopped)>0){}
        catch(TimeoutException){await lifetime.CancelAsync();Fault?.Invoke("Transcription results stalled. Stored captions and completed answers are retained; press F8 to restart listening.");}
        catch(Exception){await lifetime.CancelAsync();Fault?.Invoke("The Azure transcription connection stopped. Restart listening; completed answers are retained.");}
        finally{if(lifetime.IsCancellationRequested)try{await Task.WhenAll(sender,receiver);}catch(Exception){}}
    }
    private async Task Send(string text,CancellationToken ct)
    {using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(sendTimeout);await socket.SendAsync(Encoding.UTF8.GetBytes(text),WebSocketMessageType.Text,true,timeout.Token);}
    private async Task<string> Receive(CancellationToken ct)
    {
        var buffer=new byte[16384];using var message=new MemoryStream();
        while(true)
        {
            var result=await socket.ReceiveAsync(buffer,ct);
            if(result.MessageType==WebSocketMessageType.Close)throw new IOException("Azure closed the transcription session.");
            if(message.Length+result.Count>1024*1024)throw new InvalidDataException("Unexpectedly large transcription event.");
            message.Write(buffer,0,result.Count);if(result.EndOfMessage)break;
        }
        return Encoding.UTF8.GetString(message.ToArray());
    }
    public Task<bool> StopAsync(){lock(stateGate)return stopTask??=StopCoreAsync();}
    private async Task<bool> StopCoreAsync()
    {
        Interlocked.Exchange(ref stopped,1);var drained=true;
        audio.Writer.TryComplete();
        try
        {
            if(sender is not null)await sender.WaitAsync(TimeSpan.FromSeconds(2));
            Task pending;lock(stateGate)pending=commitsDrained.Task;
            await pending.WaitAsync(drainTimeout,lifetime.Token);
            if(HasPendingAudio)drained=false;
        }
        catch(Exception){drained=false;}
        await lifetime.CancelAsync();
        try{if(socket.State==WebSocketState.Open){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(1));await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,"Stopped",timeout.Token);}}catch(Exception){}
        if(monitor is not null)try{await monitor.WaitAsync(TimeSpan.FromSeconds(2));}catch(Exception){}
        socket.Dispose();lifetime.Dispose();
        return drained;
    }
    public async ValueTask DisposeAsync(){await StopAsync();}
    public static async Task CheckAsync(AzureSettings settings,string key,CancellationToken ct)
    {await using var client=new LiveTranscriptionClient();await client.ConnectAsync(settings,key,["Azure","interview"],ct);}
}
