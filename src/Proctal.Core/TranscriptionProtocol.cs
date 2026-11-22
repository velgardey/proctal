using System.Text.Json;
namespace Proctal.Core;
public static class TranscriptionProtocol
{
    public static IEnumerable<string> Vocabulary(IEnumerable<string> context)=>new[]{"Azure","SQL","Python","hash map","left join","sigmoid","brute force","Big O","time complexity","space complexity"}
        .Concat(context.SelectMany(text=>System.Text.RegularExpressions.Regex.Matches(text,@"\b[A-Z][A-Za-z0-9+#.\-]{2,25}\b").Select(match=>match.Value)))
        .Distinct(StringComparer.OrdinalIgnoreCase).Take(40);
    public static string SessionUpdate(string deployment,string language,IEnumerable<string> hints)
    {
        var keywords=hints.Select(s=>s.Replace("<","").Replace(">","").Replace("\r"," ").Replace("\n"," ").Trim()).Where(s=>s.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).Take(40).Select(s=>s[..Math.Min(s.Length,80)]).ToArray();
        return JsonSerializer.Serialize(new{type="session.update",session=new{type="transcription",audio=new{input=new{format=new{type="audio/pcm",rate=24000},turn_detection=(object?)null,transcription=new{model=deployment,delay="low",languages=new[]{language},keywords}}}}});
    }
}
public sealed class VoiceActivity
{
    private double speechMs,silenceMs;
    public bool Active {get;private set;}
    public double Level {get;private set;}
    public bool Accept(byte[] pcm)
    {
        if(pcm.Length%2!=0)throw new ArgumentException("PCM16 needs complete samples.");
        if(pcm.Length==0)return false;
        double square=0;for(var i=0;i<pcm.Length;i+=2){var sample=BitConverter.ToInt16(pcm,i)/32768d;square+=sample*sample;}
        Level=Math.Sqrt(square/(pcm.Length/2));var ms=pcm.Length/48d;
        // shortcut: RMS only segments streamed audio; use adaptive VAD if noisy devices delay finalization.
        if(Level>.002){Active=true;speechMs+=ms;silenceMs=0;}else if(Active)silenceMs+=ms;
        if(Active&&((speechMs>=150&&silenceMs>=700)||speechMs>=25000))
        {Active=false;speechMs=0;silenceMs=0;return true;}
        if(Active&&speechMs<150&&silenceMs>=700){Active=false;speechMs=0;silenceMs=0;}
        return false;
    }
}
