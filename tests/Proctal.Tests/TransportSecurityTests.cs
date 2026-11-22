using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class TransportSecurityTests
{
    [Fact] public async Task TranscriptionSocketDoesNotForwardTheKeyOnRedirect()
    {
        await using var live=new LiveTranscriptionClient();var socket=(System.Net.WebSockets.ClientWebSocket)typeof(LiveTranscriptionClient).GetField("socket",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(live)!;socket.Options.SetRequestHeader("api-key","synthetic-test-key");
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(5));var requests=0;
        var serving=Task.Run(async()=>
        {
            try
            {
                while(!cancellation.IsCancellationRequested)
                {
                    using var peer=await listener.AcceptTcpClientAsync(cancellation.Token);using var stream=peer.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,leaveOpen:true);
                    while(await reader.ReadLineAsync(cancellation.Token) is {Length:>0}){}requests++;
                    await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 307 Temporary Redirect\r\nLocation: http://127.0.0.1:{port}/different-destination\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"),cancellation.Token);
                }
            }
            catch(OperationCanceledException){}
        });
        try{await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(()=>LiveTranscriptionClient.ConnectSocketAsync(socket,new Uri($"ws://127.0.0.1:{port}/configured-endpoint"),cancellation.Token));}
        finally{await cancellation.CancelAsync();await serving;}
        Assert.Equal(1,requests);
    }
    [Fact] public async Task AzureHttpTransportDoesNotForwardTheKeyOnRedirect()
    {
        var http=(HttpClient)typeof(LunaClient).GetField("Shared",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var port=((IPEndPoint)listener.LocalEndpoint).Port;
        using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(5));var requests=new List<string>();
        var serving=Task.Run(async()=>
        {
            try
            {
                while(!cancellation.IsCancellationRequested)
                {
                    using var peer=await listener.AcceptTcpClientAsync(cancellation.Token);using var stream=peer.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,leaveOpen:true);var headers=new StringBuilder();
                    while(await reader.ReadLineAsync(cancellation.Token) is {Length:>0} line)headers.AppendLine(line);requests.Add(headers.ToString());
                    var redirect=requests.Count==1;var response=redirect?$"HTTP/1.1 307 Temporary Redirect\r\nLocation: http://127.0.0.1:{port}/different-destination\r\n":"HTTP/1.1 200 OK\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response+"Content-Length: 0\r\nConnection: close\r\n\r\n"),cancellation.Token);
                }
            }
            catch(OperationCanceledException){}
        });
        using var request=new HttpRequestMessage(HttpMethod.Get,$"http://127.0.0.1:{port}/configured-endpoint");request.Headers.Add("api-key","synthetic-test-key");
        try
        {
            using var response=await http.SendAsync(request,cancellation.Token);Assert.Equal(HttpStatusCode.TemporaryRedirect,response.StatusCode);
        }
        finally{await cancellation.CancelAsync();await serving;}
        Assert.Single(requests);Assert.DoesNotContain(requests,h=>h.Contains("/different-destination"));
    }
}
