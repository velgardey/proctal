using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class TranscriptionLifecycleTests
{
    [Fact] public async Task StalledAudioSendRaisesAFaultWithoutWaitingForMoreSpeech()
    {
        using var socket=new TranscriptionSocket{BlockAudio=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        typeof(LiveTranscriptionClient).GetField("sendTimeout",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(client,TimeSpan.FromMilliseconds(50));
        var fault=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);client.Fault+=message=>fault.TrySetResult(message);
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);client.Enqueue(Speech());
        try{Assert.True(await Task.WhenAny(fault.Task,Task.Delay(1500))==fault.Task,"Stalled send left listening apparently active.");}
        finally{socket.BlockAudio.TrySetResult();}
    }
    [Fact] public async Task CommittedTranscriptionThatNeverFinishesRaisesAFault()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        typeof(LiveTranscriptionClient).GetField("transcriptionTimeout",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.SetValue(client,TimeSpan.FromMilliseconds(50));
        var fault=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);client.Fault+=message=>fault.TrySetResult(message);
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);client.Enqueue(Speech());client.Enqueue(Speech());for(var i=0;i<7;i++)client.Enqueue(new byte[4800]);
        await socket.Commits.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(await Task.WhenAny(fault.Task,Task.Delay(1500))==fault.Task,"Missing final captions stalled indefinitely without a fault.");
    }
    [Fact] public async Task QuietLeadingAudioReachesAzureBeforeLouderSpeech()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        var quiet=Quiet(100);client.Enqueue(quiet);client.Enqueue(Speech());await socket.WaitForAppendsAsync(1);
        Assert.Equal(quiet,socket.AudioFrames.First());
    }
    [Fact] public async Task QuietPartialCaptionKeepsTheAudioGatePendingUntilFinal()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(300));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        client.Enqueue(Quiet(100));await socket.WaitForAppendsAsync(1);socket.Partial("item-1","Quiet spoken words");
        for(var count=0;count<20&&!client.HasPendingAudio;count++)await Task.Delay(10);
        Assert.True(client.HasPendingAudio,"A soft ongoing utterance must keep automatic answers from firing early.");
        var stopping=client.StopAsync();var id=await socket.Commits.Reader.ReadAsync();socket.Complete(id,"Quiet spoken words");
        Assert.True(await stopping);Assert.False(client.HasPendingAudio);
    }
    [Theory] [InlineData(100)] [InlineData(20)] public async Task PauseCommitsQuietAudioAndPadsShortInput(int milliseconds)
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(300));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        var quiet=Quiet(milliseconds);client.Enqueue(quiet);var stopping=client.StopAsync();var committed=socket.Commits.Reader.ReadAsync().AsTask();
        await Task.WhenAny(committed,stopping);
        Assert.True(committed.IsCompletedSuccessfully,"Pause discarded quiet speech without committing it.");
        var id=await committed;socket.Complete(id,"Quiet spoken words");Assert.True(await stopping);
        Assert.Equal(quiet,socket.AudioFrames.First());Assert.True(socket.AudioFrames.Sum(frame=>frame.Length)>=4800);
    }
    [Fact] public async Task LongQuietAudioIsCommittedBeforeItsBufferExceeds25Seconds()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        for(var count=1;count<=250;count++){Assert.True(client.Enqueue(Quiet(100)));await socket.WaitForAppendsAsync(count);}
        var committed=socket.Commits.Reader.ReadAsync().AsTask();await Task.WhenAny(committed,Task.Delay(1000));
        Assert.True(committed.IsCompletedSuccessfully,"Quiet audio exceeded the maximum buffered turn without a commit.");
        socket.Complete(await committed,"Quiet spoken words");Assert.True(await client.StopAsync());
    }
    [Fact] public async Task PauseImmediatelyAfterDurationCommitClearsTheSpeakingGate()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        for(var count=1;count<=250;count++){Assert.True(client.Enqueue(count==1?Quiet(100):Speech()));await socket.WaitForAppendsAsync(count);}
        var id=await socket.Commits.Reader.ReadAsync();socket.Complete(id,"A long spoken question.");
        Assert.True(await client.StopAsync());Assert.False(client.HasPendingAudio);
    }
    [Fact] public async Task MissingFinalCaptionReportsIncompletePause()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket,TimeSpan.FromMilliseconds(100));
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        client.Enqueue(Speech());client.Enqueue(Speech());await socket.TwoAppends.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(await client.StopAsync());
    }
    [Fact] public async Task OutOfOrderCompletionKeepsEarlierAudioPending()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket);
        var later=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.Event+=json=>{if(json.Contains("transcription.completed")&&json.Contains("second"))later.TrySetResult();};
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        client.Enqueue(Speech());client.Enqueue(Speech());for(var i=0;i<7;i++)client.Enqueue(new byte[4800]);
        var first=await socket.Commits.Reader.ReadAsync();
        client.Enqueue(Speech());client.Enqueue(Speech());for(var i=0;i<7;i++)client.Enqueue(new byte[4800]);
        var second=await socket.Commits.Reader.ReadAsync();socket.Complete(second,"second");await later.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(client.HasPendingAudio);socket.Complete(first,"first");Assert.True(await client.StopAsync());
    }
    [Fact] public async Task PauseRetainsFinalCaptionArrivingAfter350Milliseconds()
    {
        using var socket=new TranscriptionSocket();await using var client=new LiveTranscriptionClient(socket);
        var finals=new List<string>();client.Event+=json=>{if(json.Contains("transcription.completed"))finals.Add(json);};
        await client.ConnectAsync(new AzureSettings(),"test-key",[],CancellationToken.None);
        client.Enqueue(Speech());client.Enqueue(Speech());await socket.TwoAppends.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var stopping=client.StopAsync();var id=await socket.Commits.Reader.ReadAsync();
        await Task.Delay(650);socket.Complete(id,"Include a rollback plan.");await stopping;
        Assert.Single(finals);Assert.Contains("Include a rollback plan.",finals[0]);
    }
    internal static byte[] Speech(){var frame=new byte[4800];for(var i=0;i<frame.Length;i+=2){frame[i]=0x10;frame[i+1]=0x27;}return frame;}
    internal static byte[] Quiet(int milliseconds){var frame=new byte[milliseconds*48];for(var i=0;i<frame.Length;i+=2)frame[i]=5;return frame;}
}
internal sealed class TranscriptionSocket:WebSocket
{
    private readonly Channel<byte[]> incoming=Channel.CreateUnbounded<byte[]>();
    private int appended,committed;private WebSocketState state=WebSocketState.Open;
    private readonly Channel<int> appends=Channel.CreateUnbounded<int>();
    public readonly TaskCompletionSource TwoAppends=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly Channel<string> Commits=Channel.CreateUnbounded<string>();
    public readonly ConcurrentQueue<byte[]> AudioFrames=new();
    public TaskCompletionSource? BlockAudio;
    public async Task WaitForAppendsAsync(int count)
    {using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(2));while(await appends.Reader.ReadAsync(timeout.Token)<count){}}
    public override WebSocketCloseStatus? CloseStatus=>null;
    public override string? CloseStatusDescription=>null;
    public override WebSocketState State=>state;
    public override string? SubProtocol=>null;
    public void Complete(string id,string text)=>Publish(JsonSerializer.Serialize(new{type="conversation.item.input_audio_transcription.completed",event_id="final-"+id,item_id=id,content_index=0,transcript=text}));
    public void Partial(string id,string text)=>Publish(JsonSerializer.Serialize(new{type="conversation.item.input_audio_transcription.delta",item_id=id,delta=text}));
    private void Publish(string json)=>incoming.Writer.TryWrite(Encoding.UTF8.GetBytes(json));
    public override async Task SendAsync(ArraySegment<byte> buffer,WebSocketMessageType type,bool endOfMessage,CancellationToken ct)
    {
        using var json=JsonDocument.Parse(buffer.AsMemory());
        if(json.RootElement.GetProperty("type").GetString()=="input_audio_buffer.append"&&BlockAudio is not null)await BlockAudio.Task.WaitAsync(ct);
        switch(json.RootElement.GetProperty("type").GetString())
        {
            case "session.update":Publish("{\"type\":\"session.updated\",\"event_id\":\"session-ready\",\"session\":{\"type\":\"transcription\"}}");break;
            case "input_audio_buffer.append":AudioFrames.Enqueue(Convert.FromBase64String(json.RootElement.GetProperty("audio").GetString()!));var count=Interlocked.Increment(ref appended);appends.Writer.TryWrite(count);if(count==2)TwoAppends.TrySetResult();break;
            case "input_audio_buffer.commit":
                var number=Interlocked.Increment(ref committed);var id="item-"+number;
                Publish(JsonSerializer.Serialize(new{type="input_audio_buffer.committed",event_id="commit-"+id,item_id=id,previous_item_id=number==1?null:"item-"+(number-1)}));
                Commits.Writer.TryWrite(id);break;
        }
    }
    public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,CancellationToken ct)
    {
        var bytes=await incoming.Reader.ReadAsync(ct);bytes.AsSpan().CopyTo(buffer.AsSpan());
        return new WebSocketReceiveResult(bytes.Length,WebSocketMessageType.Text,true);
    }
    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus,string? statusDescription,CancellationToken ct){state=WebSocketState.CloseSent;return Task.CompletedTask;}
    public override Task CloseAsync(WebSocketCloseStatus closeStatus,string? statusDescription,CancellationToken ct){state=WebSocketState.Closed;return Task.CompletedTask;}
    public override void Abort()=>state=WebSocketState.Aborted;
    public override void Dispose()=>state=WebSocketState.Closed;
}
