namespace Proctal.Core;

public sealed class ShortcutPolicy
{
    private readonly HashSet<uint> held=[];
    private readonly Func<long> monotonicMilliseconds;
    private long? previousF11Down;
    private bool altGrDown;
    public ShortcutPolicy(Func<long>? monotonicMilliseconds=null)=>this.monotonicMilliseconds=monotonicMilliseconds??(()=>Environment.TickCount64);
    public bool IsCaptured(uint key)=>held.Contains(key);
    public ShortcutAction Accept(uint key,bool down,bool injected,bool ctrl=false,bool shift=false,bool alt=false,bool overlayVisible=true,bool rightAlt=false)
    {
        if(injected)return ShortcutAction.None;
        var plainF11=key==0x7A&&!ctrl&&!shift&&!alt&&!rightAlt&&!altGrDown;
        if(down&&!plainF11)previousF11Down=null;
        if(key==0xA5){altGrDown=down;return ShortcutAction.None;}
        if(!down){held.Remove(key);return ShortcutAction.None;}
        if(rightAlt||altGrDown)return ShortcutAction.None;
        var action=ctrl&&alt&&overlayVisible?(shift?key switch
            {0x25=>ShortcutAction.ResizeLeft,0x27=>ShortcutAction.ResizeRight,0x26=>ShortcutAction.ResizeUp,0x28=>ShortcutAction.ResizeDown,0x21=>ShortcutAction.CaptionPageUp,0x22=>ShortcutAction.CaptionPageDown,0x24=>ShortcutAction.CaptionTop,0x23=>ShortcutAction.CaptionEnd,_=>ShortcutAction.None}:
            key switch
            {
                0x26=>ShortcutAction.ScrollUp,0x28=>ShortcutAction.ScrollDown,0x21=>ShortcutAction.PageUp,0x22=>ShortcutAction.PageDown,
                0x24=>ShortcutAction.ScrollTop,0x23=>ShortcutAction.ScrollEnd,0x25=>ShortcutAction.PreviousAnswer,0x27=>ShortcutAction.NextAnswer,
                0xDB=>ShortcutAction.OpacityDown,0xDD=>ShortcutAction.OpacityUp,0xBD or 0x6D=>ShortcutAction.ZoomOut,0xBB or 0x6B=>ShortcutAction.ZoomIn,
                0x43=>ShortcutAction.CopyCode,0x4D=>ShortcutAction.Compact,0x57=>ShortcutAction.WrapCode,0x46=>ShortcutAction.Follow,
                0x4C=>ShortcutAction.LatestAnswer,0x48=>ShortcutAction.History,0x31=>ShortcutAction.Explain,0x32=>ShortcutAction.EdgeCases,0x33=>ShortcutAction.Optimize,
                0x30=>ShortcutAction.ResetLayout,0x51=>ShortcutAction.EditQuestion,0x45=>ShortcutAction.ExportAnswer,0x47=>ShortcutAction.Glass,
                0x54=>ShortcutAction.Captions,0x53=>ShortcutAction.AnswerImages,0x56=>ShortcutAction.Tune,0x41=>ShortcutAction.AutoAnswer,0x42=>ShortcutAction.AlwaysOnTop,0x50=>ShortcutAction.Pin,0x4E=>ShortcutAction.PracticeQuestion,0x52=>ShortcutAction.ReviewAnswer,0x08=>ShortcutAction.ClearImages,0x1B=>ShortcutAction.CancelAnswer,_=>ShortcutAction.None
            }):key==0x7B?ShortcutAction.Toggle:
            key==0x75&&!ctrl&&!shift&&!alt?ShortcutAction.SolAnswer:
            key==0x76&&!ctrl&&!shift&&!alt?ShortcutAction.DeepAnswer:
            key==0x77&&!ctrl&&!shift&&!alt?ShortcutAction.Pause:
            key==0x78&&!ctrl&&!shift&&!alt?ShortcutAction.AnswerImages:
            key==0x79?ShortcutAction.Screenshot:key==0x7A?ShortcutAction.Answer:
            ctrl&&shift&&!alt?key switch
            {
                0x20=>ShortcutAction.Toggle,0x50=>ShortcutAction.Pause,0x49=>ShortcutAction.Interactive,
                0x43 when overlayVisible=>ShortcutAction.Copy,0x25 when overlayVisible=>ShortcutAction.Left,0x26 when overlayVisible=>ShortcutAction.Up,
                0x27 when overlayVisible=>ShortcutAction.Right,0x28 when overlayVisible=>ShortcutAction.Down,_=>ShortcutAction.None
            }:ShortcutAction.None;
        if(action==ShortcutAction.None)return ShortcutAction.None;
        if(!held.Add(key)&&!Repeats(action))return ShortcutAction.None;
        if(plainF11)
        {
            var now=monotonicMilliseconds();
            if(previousF11Down is long previous&&now>=previous&&now-previous<=500)
            {
                previousF11Down=null;
                return ShortcutAction.AnswerImages;
            }
            previousF11Down=now;
        }
        return action;
    }
    private static bool Repeats(ShortcutAction action)=>action is ShortcutAction.ScrollUp or ShortcutAction.ScrollDown or ShortcutAction.PageUp or ShortcutAction.PageDown or ShortcutAction.CaptionPageUp or ShortcutAction.CaptionPageDown or ShortcutAction.ResizeLeft or ShortcutAction.ResizeRight or ShortcutAction.ResizeUp or ShortcutAction.ResizeDown or ShortcutAction.ZoomIn or ShortcutAction.ZoomOut or ShortcutAction.OpacityUp or ShortcutAction.OpacityDown;
}
public sealed class AnswerVersion
{
    private long version;
    public long Begin()=>Interlocked.Increment(ref version);
    public bool IsCurrent(long id)=>Volatile.Read(ref version)==id;
}
