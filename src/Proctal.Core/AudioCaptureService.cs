using NAudio.CoreAudioApi;
using NAudio.Wave;
namespace Proctal.Core;
public record AudioDevice(string Id,string Name)
{ public override string ToString()=>Name; }
public sealed class AudioCaptureService:IDisposable
{
    private readonly List<WasapiCapture> captures=[];
    private DateTimeOffset lastInterviewerVoice=DateTimeOffset.MinValue;
    public bool IsInterviewerSpeaking=>DateTimeOffset.UtcNow-lastInterviewerVoice<TimeSpan.FromMilliseconds(500);
    public event Action<AudioSource,byte[]>? Frame;
    public event Action<AudioSource,double>? Level;
    public event Action<string>? Fault;
    public static IReadOnlyList<AudioDevice> Devices(bool input)
    {
        using var enumerator=new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(input?DataFlow.Capture:DataFlow.Render,DeviceState.Active).Select(d=>{using(d)return new AudioDevice(d.ID,d.FriendlyName);}).ToArray();
    }
    public void Start(string outputDevice,string? inputDevice)
    {
        Stop();
        try{AddCapture(outputDevice,AudioSource.Interviewer);if(inputDevice is not null)AddCapture(inputDevice,AudioSource.Candidate);}
        catch{Stop();throw;}
    }
    private void AddCapture(string id,AudioSource source)
    {
        using var enumerator=new MMDeviceEnumerator();
        var device=string.IsNullOrWhiteSpace(id)?enumerator.GetDefaultAudioEndpoint(source==AudioSource.Interviewer?DataFlow.Render:DataFlow.Capture,Role.Communications):enumerator.GetDevice(id);
        WasapiCapture capture=source==AudioSource.Interviewer?new WasapiLoopbackCapture(device):new WasapiCapture(device);
        var converter=new PcmConverter(capture.WaveFormat);
        capture.DataAvailable+=(_,e)=>
        {
            try
            {
                var pcm=converter.Convert(e.Buffer,e.BytesRecorded);if(pcm.Length==0)return;
                double sum=0;for(var i=0;i<pcm.Length;i+=2){var value=BitConverter.ToInt16(pcm,i)/32768d;sum+=value*value;}var rms=Math.Sqrt(sum/(pcm.Length/2));
                if(source==AudioSource.Interviewer&&rms>.009)lastInterviewerVoice=DateTimeOffset.UtcNow;
                Level?.Invoke(source,rms);Frame?.Invoke(source,pcm);
            }
            catch(Exception){Fault?.Invoke("Audio capture could not keep up. Pause and restart the session.");}
        };
        capture.RecordingStopped+=(_,e)=>{if(e.Exception is not null)Fault?.Invoke("The selected audio device stopped. Refresh devices and restart listening.");};
        captures.Add(capture);capture.StartRecording();
    }
    public void Stop(){foreach(var capture in captures){capture.StopRecording();capture.Dispose();}captures.Clear();lastInterviewerVoice=DateTimeOffset.MinValue;}
    public void Dispose()=>Stop();
}
