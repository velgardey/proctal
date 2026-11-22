using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Proctal.Core;
namespace Proctal.Desktop.Platform;
internal static class WindowAppearance
{
    [StructLayout(LayoutKind.Sequential)]private struct Margins{public int Left,Right,Top,Bottom;}
    [DllImport("dwmapi.dll")]private static extern int DwmExtendFrameIntoClientArea(nint hwnd,ref Margins margins);
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(nint hwnd,int attribute,ref int value,int size);
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct Point{public int X,Y;}
    [DllImport("user32.dll")]private static extern bool GetWindowRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(nint hwnd,nint after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")]private static extern nint MonitorFromPoint(Point point,uint flags);
    [DllImport("shcore.dll")]private static extern int GetDpiForMonitor(nint monitor,int type,out uint x,out uint y);
    public static void ApplyOpacity(Window window,double opacity)
    {
        window.Opacity=Math.Clamp(double.IsFinite(opacity)?opacity:1,.35,1);
    }
    public static void KeepNonactivating(Window window)
    {
        var source=HwndSource.FromHwnd(new WindowInteropHelper(window).EnsureHandle());
        source?.AddHook((nint hwnd,int msg,nint w,nint l,ref bool handled)=>
        {
            if(msg==0x21){handled=true;return new nint(3);}
            if(msg==0x84&&window.AllowsTransparency&&!WindowPrivacy.HasReadOnlyStyles(window)&&GetWindowRect(hwnd,out var rect))
            {
                var x=(short)(l.ToInt64()&0xffff);var y=(short)((l.ToInt64()>>16)&0xffff);
                var hit=ResizeHit(x-rect.Left,y-rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top,8*VisualTreeHelper.GetDpi(window).DpiScaleX);
                if(hit!=0){handled=true;return new nint(hit);}
            }
            return nint.Zero;
        });
    }
    internal static int ResizeHit(double x,double y,double width,double height,double edge)
    {
        if(x<0||y<0||x>width||y>height)return 0;
        var left=x<edge;var right=x>width-edge;var top=y<edge;var bottom=y>height-edge;
        return top?left?13:right?14:12:bottom?left?16:right?17:15:left?10:right?11:0;
    }
    public static bool Apply(Window window,bool glass)
    {
        var hwnd=new WindowInteropHelper(window).EnsureHandle();var source=HwndSource.FromHwnd(hwnd);if(source?.CompositionTarget is {} target)target.BackgroundColor=Colors.Transparent;
        if(window.AllowsTransparency)return false;
        var margins=new Margins{Left=-1,Right=-1,Top=-1,Bottom=-1};DwmExtendFrameIntoClientArea(hwnd,ref margins);
        var dark=1;DwmSetWindowAttribute(hwnd,20,ref dark,4);var corners=2;DwmSetWindowAttribute(hwnd,33,ref corners,4);
        var backdrop=glass&&!SystemParameters.HighContrast?3:1;
        return OperatingSystem.IsWindowsVersionAtLeast(10,0,22621)&&DwmSetWindowAttribute(hwnd,38,ref backdrop,4)==0&&backdrop==3;
    }
    public static (OverlayPlacement Work,double Scale) MonitorArea(Window window,double? physicalLeft=null,double? physicalTop=null)
    {
        var bounds=PhysicalBounds(window);
        var point=new Point{X=(int)(physicalLeft??bounds.Left),Y=(int)(physicalTop??bounds.Top)};
        var screen=System.Windows.Forms.Screen.FromPoint(new(point.X,point.Y)).WorkingArea;
        var scale=VisualTreeHelper.GetDpi(window).DpiScaleX;
        if(GetDpiForMonitor(MonitorFromPoint(point,2),0,out var dpi,out _)==0&&dpi>0)scale=dpi/96d;
        return (new(screen.Width/scale,screen.Height/scale,screen.Left/scale,screen.Top/scale),scale);
    }
    public static OverlayPlacement WorkArea(Window window)=>MonitorArea(window).Work;
    public static OverlayPlacement PhysicalBounds(Window window)
    {GetWindowRect(new WindowInteropHelper(window).EnsureHandle(),out var rect);return new(rect.Right-rect.Left,rect.Bottom-rect.Top,rect.Left,rect.Top);}
    public static void Place(Window window,OverlayPlacement layout,double scale)
    {
        // Win32 coordinates use physical pixels, independent of the window's startup monitor.
        SetWindowPos(new WindowInteropHelper(window).EnsureHandle(),0,(int)Math.Round(layout.Left*scale),(int)Math.Round(layout.Top*scale),(int)Math.Round(layout.Width*scale),(int)Math.Round(layout.Height*scale),0x14);
    }
}
