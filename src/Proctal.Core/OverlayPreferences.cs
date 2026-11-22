using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
namespace Proctal.Core;
public sealed class OverlayPreferences:INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private double opacity=.82,fontSize=16,width=620,height=620;
    private double? left,top,physicalLeft,physicalTop;
    private bool codeWrap,compact,autoFollow=true,glass=true,alwaysOnTop=true,showCaptions=true;
    private double captionHeight=84;
    private bool expandCaptions;
    public double Opacity{get=>opacity;set=>Set(ref opacity,Limit(value,.35,1,.82));}
    public double FontSize{get=>fontSize;set=>Set(ref fontSize,Limit(value,12,24,16));}
    public double Width{get=>width;set=>Set(ref width,Limit(value,440,1800,620));}
    public double Height{get=>height;set=>Set(ref height,Limit(value,430,1600,760));}
    public double? Left{get=>left;set=>Set(ref left,value is {} n&&double.IsFinite(n)?n:null);}
    public double? Top{get=>top;set=>Set(ref top,value is {} n&&double.IsFinite(n)?n:null);}
    public double? PhysicalLeft{get=>physicalLeft;set=>Set(ref physicalLeft,value is {} n&&double.IsFinite(n)?n:null);}
    public double? PhysicalTop{get=>physicalTop;set=>Set(ref physicalTop,value is {} n&&double.IsFinite(n)?n:null);}
    public bool CodeWrap{get=>codeWrap;set=>Set(ref codeWrap,value);}
    public bool Compact{get=>compact;set=>Set(ref compact,value);}
    public bool AutoFollow{get=>autoFollow;set=>Set(ref autoFollow,value);}
    public bool Glass{get=>glass;set=>Set(ref glass,value);}
    public bool AlwaysOnTop{get=>alwaysOnTop;set=>Set(ref alwaysOnTop,value);}
    public bool ShowCaptions{get=>showCaptions;set=>Set(ref showCaptions,value);}
    public bool ExpandCaptions{get=>expandCaptions;set=>Set(ref expandCaptions,value);}
    public double CaptionHeight{get=>captionHeight;set=>Set(ref captionHeight,Limit(value,60,180,84));}
    private static double Limit(double value,double min,double max,double fallback)=>double.IsFinite(value)?Math.Clamp(value,min,max):fallback;
    private void Set<T>(ref T field,T value,[CallerMemberName]string name=""){if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;PropertyChanged?.Invoke(this,new(name));}
    public static OverlayPreferences Load(string path)=>File.Exists(path)?JsonSerializer.Deserialize<OverlayPreferences>(File.ReadAllText(path))??new():new();
    public void Save(string path){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);}
}
public readonly record struct OverlayPlacement(double Width,double Height,double Left,double Top)
{
    public OverlayPlacement Fit(OverlayPlacement workArea)
    {
        var width=Math.Clamp(double.IsFinite(Width)?Width:620,Math.Min(440,workArea.Width),workArea.Width);
        var height=Math.Clamp(double.IsFinite(Height)?Height:760,Math.Min(430,workArea.Height),workArea.Height);
        var left=Math.Clamp(double.IsFinite(Left)?Left:workArea.Left+workArea.Width-width-20,workArea.Left,workArea.Left+workArea.Width-width);
        var top=Math.Clamp(double.IsFinite(Top)?Top:workArea.Top+20,workArea.Top,workArea.Top+workArea.Height-height);
        return new(width,height,left,top);
    }
}
