using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Proctal.Core;
namespace Proctal.Desktop;
public sealed class LiveCaptionView:UserControl
{
    public static readonly DependencyProperty TextProperty=DependencyProperty.Register(nameof(Text),typeof(string),typeof(LiveCaptionView),new PropertyMetadata("",Changed));
    private readonly TextBox editor=new(){IsReadOnly=true,IsUndoEnabled=false,Focusable=false,IsTabStop=false,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),FontSize=13};
    private readonly DispatcherTimer timer;
    private bool following=true,rendering;
    public string Text{get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value??"");}
    public bool IsFollowing=>following;
    public event Action? ReadingPositionChanged;
    public LiveCaptionView()
    {
        Content=editor;Focusable=false;editor.FontFamily=new("pack://application:,,,/Proctal;component/Assets/Fonts/#IBM Plex Sans");editor.SetResourceReference(ForegroundProperty,"MutedBrush");
        timer=new DispatcherTimer(TimeSpan.FromMilliseconds(80),DispatcherPriority.Background,(_,_)=>FlushPendingRender(),Dispatcher);timer.Stop();
        Loaded+=(_,_)=>FlushPendingRender();Unloaded+=(_,_)=>timer.Stop();
        IsVisibleChanged+=(_,_)=>{if(IsVisible)FlushPendingRender();else timer.Stop();};
        PreviewMouseWheel+=(_,_)=>{following=false;ReadingPositionChanged?.Invoke();};
        editor.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler((_,e)=>{if(!rendering&&e.ExtentHeightChange==0&&e.VerticalChange<0){following=false;ReadingPositionChanged?.Invoke();}}));
    }
    private static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e){var view=(LiveCaptionView)d;if(view.IsVisible&&!view.timer.IsEnabled)view.timer.Start();}
    public void FlushPendingRender()
    {
        timer.Stop();rendering=true;
        var old=editor.Text;var position=editor.VerticalOffset;var first=editor.GetFirstVisibleLineIndex();var anchor=first>=0?editor.GetCharacterIndexFromLineIndex(first):-1;
        var anchorTop=anchor>=0?editor.GetRectFromCharacterIndex(anchor).Top:0;
        var next=Text.Length>20000?Text[^20000..]:Text;var mapped=anchor;
        if(!following&&anchor>=0&&old.Length>anchor&&!next.StartsWith(old,StringComparison.Ordinal))
        {
            // Keep surviving text in place as the transcript drops its oldest turn.
            var probe=old.Substring(anchor,Math.Min(160,old.Length-anchor));mapped=next.IndexOf(probe,StringComparison.Ordinal);
            if(mapped<0){var newline=probe.IndexOf('\n');if(newline>0)mapped=next.IndexOf(probe[..newline],StringComparison.Ordinal);}
            if(mapped<0){var shared=0;while(shared<Math.Min(old.Length,next.Length)&&old[shared]==next[shared])shared++;if(shared>=anchor)mapped=anchor;}
        }
        editor.Text=next;editor.UpdateLayout();
        if(following)editor.ScrollToEnd();
        else if(mapped>=0&&mapped<next.Length){var rect=editor.GetRectFromCharacterIndex(mapped);editor.ScrollToVerticalOffset(rect.IsEmpty?position:Math.Max(0,editor.VerticalOffset+rect.Top-anchorTop));}
        else editor.ScrollToHome();
        rendering=false;ReadingPositionChanged?.Invoke();
    }
    public void Scroll(ShortcutAction action)
    {
        following=action==ShortcutAction.CaptionEnd;
        switch(action){case ShortcutAction.CaptionPageUp:editor.PageUp();break;case ShortcutAction.CaptionPageDown:editor.PageDown();break;case ShortcutAction.CaptionTop:editor.ScrollToHome();break;case ShortcutAction.CaptionEnd:editor.ScrollToEnd();break;}
        ReadingPositionChanged?.Invoke();
    }
}
