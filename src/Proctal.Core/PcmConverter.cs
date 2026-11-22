using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Proctal.Core;
public sealed class PcmConverter
{
    private readonly BufferedWaveProvider buffer;
    private readonly IWaveProvider output;
    private readonly WaveFormat inputFormat;
    public PcmConverter(WaveFormat format)
    {
        inputFormat=format;
        buffer=new BufferedWaveProvider(format){ReadFully=false,BufferDuration=TimeSpan.FromSeconds(2)};
        ISampleProvider samples=buffer.ToSampleProvider();
        if(samples.WaveFormat.Channels>1)samples=new Downmix(samples);
        if(samples.WaveFormat.SampleRate!=24000)samples=new WdlResamplingSampleProvider(samples,24000);
        output=new SampleToWaveProvider16(samples);
    }
    public byte[] Convert(byte[] data,int count)
    {
        if(count<0||count>data.Length||count%inputFormat.BlockAlign!=0)throw new ArgumentException("Audio data contains an incomplete frame.");
        buffer.AddSamples(data,0,count);
        var expected=(int)Math.Ceiling(count/(double)inputFormat.BlockAlign*24000/inputFormat.SampleRate)*2;
        var bytes=new byte[Math.Max(2,expected)];var read=output.Read(bytes,0,bytes.Length);return bytes[..read];
    }
    private sealed class Downmix(ISampleProvider input):ISampleProvider
    {
        public WaveFormat WaveFormat {get;}=WaveFormat.CreateIeeeFloatWaveFormat(input.WaveFormat.SampleRate,1);
        private float[] scratch=[];
        public int Read(float[] target,int offset,int count)
        {
            var channels=input.WaveFormat.Channels;if(scratch.Length<count*channels)scratch=new float[count*channels];
            var read=input.Read(scratch,0,count*channels);var frames=read/channels;
            for(var frame=0;frame<frames;frame++){float sum=0;for(var c=0;c<channels;c++)sum+=scratch[frame*channels+c];target[offset+frame]=sum/channels;}
            return frames;
        }
    }
}
