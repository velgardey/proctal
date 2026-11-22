using System.Text.Json;
namespace Proctal.Core;
public sealed class TranscriptBuffer
{
    private readonly object gate=new();
    private readonly Dictionary<string,TranscriptTurn> turns=[];
    private readonly List<string> order=[];
    public TranscriptTurn? Accept(string json,AudioSource source)
    {
        using var parsed=JsonDocument.Parse(json);var e=parsed.RootElement;
        if(!e.TryGetProperty("type",out var tp))return null;
        var type=tp.GetString();
        if(type is not ("input_audio_buffer.committed" or "conversation.item.input_audio_transcription.delta" or "conversation.item.input_audio_transcription.completed"))return null;
        if(!e.TryGetProperty("item_id",out var item))return null;var id=item.GetString()??"";if(id.Length==0)return null;
        var key=$"{source}:{id}";
        lock(gate)
        {
            if(!turns.TryGetValue(key,out var previous)){previous=new TranscriptTurn(id,source,"",false,DateTimeOffset.UtcNow);turns[key]=previous;order.Add(key);}
            if(order.Count>300){turns.Remove(order[0]);order.RemoveAt(0);}
            if(previous.IsFinal)return null;
            if(type=="input_audio_buffer.committed")return null;
            var final=type=="conversation.item.input_audio_transcription.completed";
            var text=final?(e.TryGetProperty("transcript",out var tx)?tx.GetString()??"":""):
                previous.Text+(e.TryGetProperty("delta",out var dx)?dx.GetString()??"":"");
            var current=previous with {Text=text,IsFinal=final,CompletedAt=final?DateTimeOffset.UtcNow:null};turns[key]=current;
            return current;
        }
    }
    public IReadOnlyList<TranscriptTurn> Snapshot(){lock(gate)return order.Select(k=>turns[k]).Where(t=>t.Text.Length>0).ToArray();}
}
public sealed class QuestionAssembler
{
    private readonly List<TranscriptTurn> pending=[];
    public void Accept(TranscriptTurn t)
    {
        if(t.Source==AudioSource.Interviewer&&t.IsFinal&&!string.IsNullOrWhiteSpace(t.Text)&&!pending.Any(x=>x.ItemId==t.ItemId))pending.Add(t);
    }
    public string? Flush(DateTimeOffset now)
    {
        if(pending.Count==0||now-pending.Max(t=>t.CompletedAt??t.At)<TimeSpan.FromMilliseconds(700))return null;
        var text=string.Join(" ",pending.OrderBy(t=>t.At).Select(t=>t.Text.Trim()));pending.Clear();return text;
    }
    public void Clear()=>pending.Clear();
    public void Discard(IEnumerable<string> ids){var consumed=ids.ToHashSet();pending.RemoveAll(t=>consumed.Contains(t.ItemId));}
    public IReadOnlyList<TranscriptTurn> Snapshot()=>pending.OrderBy(t=>t.At).ToArray();
    public static bool LooksLikeQuestion(string text)=>text.Contains('?')||System.Text.RegularExpressions.Regex.IsMatch(text.Trim(),@"^(how|what|why|when|where|which|who|tell|describe|explain|walk|design|implement|write|can|could|would|give|suppose|imagine|let's)\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
