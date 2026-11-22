using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
namespace Proctal.Desktop.Platform;
public record CaptureStatus(bool Applied,uint Affinity,int Error);
public static class WindowPrivacy
{
    private sealed class HoverState
    {
        public bool Exclude;
        public HashSet<ToolTip> OpenTips {get;}=[];
    }
    private static readonly ConditionalWeakTable<Window,HoverState> hoverStates=new();
    static WindowPrivacy()
    {
        // ToolTipOpening is direct, so a handler on the parent window misses child controls.
        EventManager.RegisterClassHandler(typeof(FrameworkElement),ToolTipService.ToolTipOpeningEvent,new ToolTipEventHandler(PreventHoverPopup),true);
        EventManager.RegisterClassHandler(typeof(FrameworkContentElement),ToolTipService.ToolTipOpeningEvent,new ToolTipEventHandler(PreventHoverPopup),true);
        EventManager.RegisterClassHandler(typeof(ToolTip),ToolTip.OpenedEvent,new RoutedEventHandler(TipOpened),true);
        EventManager.RegisterClassHandler(typeof(ToolTip),ToolTip.ClosedEvent,new RoutedEventHandler(TipClosed),true);
    }
    private static void PreventHoverPopup(object sender,ToolTipEventArgs e)
    {
        if(sender is DependencyObject element&&Window.GetWindow(element) is Window window&&hoverStates.TryGetValue(window,out var state)&&state.Exclude)e.Handled=true;
    }
    private static void TipOpened(object sender,RoutedEventArgs e)
    {
        if(sender is ToolTip tip&&tip.PlacementTarget is DependencyObject target&&Window.GetWindow(target) is Window window&&hoverStates.TryGetValue(window,out var state))
        {
            if(state.Exclude)tip.IsOpen=false;
            else state.OpenTips.Add(tip);
        }
    }
    private static void TipClosed(object sender,RoutedEventArgs e)
    {
        if(sender is ToolTip tip&&tip.PlacementTarget is DependencyObject target&&Window.GetWindow(target) is Window window&&hoverStates.TryGetValue(window,out var state))state.OpenTips.Remove(tip);
    }
    [DllImport("user32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetWindowDisplayAffinity(nint hwnd,uint affinity);
    [DllImport("user32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetWindowDisplayAffinity(nint hwnd,out uint affinity);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)]private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)]private static extern nint SetWindowLongPtr(nint hwnd,int index,nint value);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int cx,int cy,uint flags);
    public static CaptureStatus Apply(Window window,bool exclude)
    {
        var state=hoverStates.GetOrCreateValue(window);state.Exclude=exclude;
        if(exclude)foreach(var tip in state.OpenTips.ToArray())tip.IsOpen=false;
        var hwnd=new WindowInteropHelper(window).EnsureHandle();var applied=SetWindowDisplayAffinity(hwnd,exclude?0x11u:0u);var error=applied?0:Marshal.GetLastWin32Error();
        GetWindowDisplayAffinity(hwnd,out var actual);return new CaptureStatus(applied,actual,error);
    }
    public static void ReadOnly(Window window,bool readOnly)
    {
        var hwnd=new WindowInteropHelper(window).EnsureHandle();var style=GetWindowLongPtr(hwnd,-20).ToInt64();
        const long transparent=0x20,noActivate=0x08000000,toolWindow=0x80;
        style|=toolWindow|noActivate;if(readOnly)style|=transparent;else style&=~transparent;
        SetWindowLongPtr(hwnd,-20,new nint(style));SetWindowPos(hwnd,new nint(window.Topmost?-1:-2),0,0,0,0,0x0010|0x0001|0x0002|0x0020);
        window.ShowActivated=false;
    }
    public static bool HasReadOnlyStyles(Window window)
    {var style=GetWindowLongPtr(new WindowInteropHelper(window).EnsureHandle(),-20).ToInt64();return (style&0x08000020)==0x08000020;}
    public static bool HasNoActivate(Window window)=>(GetWindowLongPtr(new WindowInteropHelper(window).EnsureHandle(),-20).ToInt64()&0x08000000)!=0;
}
