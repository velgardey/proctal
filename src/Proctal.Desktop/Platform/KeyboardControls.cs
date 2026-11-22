using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Proctal.Core;
namespace Proctal.Desktop.Platform;
public sealed class KeyboardControls:IDisposable
{
    private delegate nint Hook(int code,nint message,nint data);
    [DllImport("user32.dll",SetLastError=true)]private static extern nint SetWindowsHookExW(int id,Hook callback,nint module,uint thread);
    [DllImport("user32.dll")]private static extern nint CallNextHookEx(nint hook,int code,nint message,nint data);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]private static extern short GetAsyncKeyState(int key);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern nint GetModuleHandleW(string? name);
    private readonly Hook callback;
    private readonly ShortcutPolicy policy=new();
    private readonly Dispatcher dispatcher;
    private readonly Action<ShortcutAction> action;
    private readonly Func<bool> overlayVisible;
    private readonly nint handle;
    public KeyboardControls(Dispatcher dispatcher,Action<ShortcutAction> action,Func<bool>? overlayVisible=null)
    {
        this.dispatcher=dispatcher;this.action=action;this.overlayVisible=overlayVisible??(()=>false);callback=Process;
        handle=SetWindowsHookExW(13,callback,GetModuleHandleW(null),0);if(handle==0)throw new Win32Exception(Marshal.GetLastWin32Error(),"Global shortcuts could not be enabled.");
    }
    private nint Process(int code,nint message,nint data)
    {
        if(code<0)return CallNextHookEx(handle,code,message,data);
        var msg=message.ToInt64();if(msg is not (0x100 or 0x101 or 0x104 or 0x105))return CallNextHookEx(handle,code,message,data);
        var key=(uint)Marshal.ReadInt32(data);var injected=(Marshal.ReadInt32(data,8)&0x10)!=0;
        if(injected)return CallNextHookEx(handle,code,message,data);
        var owned=policy.IsCaptured(key);var next=policy.Accept(key,msg is 0x100 or 0x104,false,GetAsyncKeyState(0x11)<0,GetAsyncKeyState(0x10)<0,GetAsyncKeyState(0x12)<0,overlayVisible(),GetAsyncKeyState(0xA5)<0);
        if(next!=ShortcutAction.None)dispatcher.BeginInvoke(()=>action(next),DispatcherPriority.Input);
        if(owned||policy.IsCaptured(key)||next!=ShortcutAction.None)return new nint(1);
        return CallNextHookEx(handle,code,message,data);
    }
    public void Dispose(){if(handle!=0)UnhookWindowsHookEx(handle);GC.KeepAlive(callback);}
}
