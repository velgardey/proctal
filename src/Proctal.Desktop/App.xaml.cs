using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
public partial class App:Application
{
    private ProctalController? controller;
    private OverlayWindow? overlay;
    private DashboardWindow? dashboard;
    private QuestionEditorWindow? editor;
    private KeyboardControls? keyboard;
    private System.Windows.Forms.NotifyIcon? tray;
    private Mutex? instance;
    private EventWaitHandle? launchSignal;
    private RegisteredWaitHandle? launchWait;
    private bool capturing,applyingPrivacy,showAfterCapture;
    public bool IsExiting {get;private set;}
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException+=(_,failure)=>{failure.Handled=true;controller?.ReportStatus("An operation failed. Pause and retry, or restart Proctal if it persists.");};
        TaskScheduler.UnobservedTaskException+=(_,failure)=>failure.SetObserved();
        try
        {
            if(e.Args.Contains("--check-azure")){await Diagnostics.CheckAzureAsync(e.Args);Shutdown();return;}
            if(e.Args.Contains("--self-check")){await Diagnostics.SelfCheckAsync(this,e.Args);Shutdown();return;}
            instance=new Mutex(true,"Local\\Proctal.InterviewPractice",out var created);
            launchSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\Proctal.ShowOverlay");
            if(!created){launchSignal.Set();Shutdown();return;}
            InitializeWindows();
            launchWait=ThreadPool.RegisterWaitForSingleObject(launchSignal,(_,_)=>Dispatcher.BeginInvoke(()=>{if(!IsExiting)ShowOverlay();}),null,Timeout.Infinite,false);
            await controller!.LoadHistoryAsync();
            try{keyboard=new KeyboardControls(Dispatcher,HandleShortcut,()=>overlay?.IsVisible==true||capturing);}
            catch(Win32Exception){controller.Interactive=true;controller.ReportStatus("Global shortcuts could not be enabled. Mouse controls are available; restart Proctal to retry shortcuts.");}
            tray=new System.Windows.Forms.NotifyIcon{Text="Proctal · Interview practice",Icon=System.Drawing.SystemIcons.Application,Visible=true};
            var menu=new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Show / hide overlay",null,(_,_)=>Dispatcher.BeginInvoke(ToggleOverlay));
            menu.Items.Add("Setup and history",null,(_,_)=>Dispatcher.BeginInvoke(ShowDashboard));
            menu.Items.Add("Pause / start listening",null,(_,_)=>Dispatcher.BeginInvoke(async()=>await controller.Safe(controller.ToggleListeningAsync)));
            menu.Items.Add("Exit Proctal",null,(_,_)=>Dispatcher.BeginInvoke(async()=>await ExitAsync()));tray.ContextMenuStrip=menu;
            tray.DoubleClick+=(_,_)=>Dispatcher.BeginInvoke(ShowOverlay);
            ShowStartup();
        }
        catch(Exception failure)
        {
            if(e.Args.Contains("--self-check")||e.Args.Contains("--check-azure")){Diagnostics.WriteFailure(e.Args,failure);Shutdown(1);}
            else{MessageBox.Show("Proctal could not start. Check project-folder write access and run the build script again.","Proctal");await ExitAsync();}
        }
    }
    internal void InitializeWindows(string? dataRoot=null)
    {
        controller=new ProctalController(Dispatcher,dataRoot);overlay=new OverlayWindow(controller);MainWindow=overlay;
        controller.CaptureScreenshot=CaptureScreenshotAsync;
        controller.Settings.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(AzureSettings.CaptureExclusion))ApplyPrivacy();};
        controller.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(ProctalController.Interactive))overlay.ApplyInteraction();};
    }
    internal ProctalController Controller=>controller!;
    internal Func<ScreenshotItem> CaptureFrame {get;set;}=ScreenshotService.Capture;
    internal OverlayWindow Overlay=>overlay!;
    internal DashboardWindow Dashboard=>dashboard??=new DashboardWindow(controller!);
    internal QuestionEditorWindow Editor=>editor??=new QuestionEditorWindow(controller!){Owner=overlay};
    internal bool HasDashboard=>dashboard is not null;
    internal void ShowStartup(){if(controller!.HasSavedConfiguration&&controller.Settings.LaunchOverlayOnStartup)ShowOverlay();else{ShowDashboard();if(!controller.HasSavedConfiguration){Dashboard.Workspace.SelectedIndex=2;Dashboard.SettingsPages.SelectedIndex=0;}}}
    public void ApplyOpacity(){if(overlay is not null&&controller is not null)overlay.ApplyPreferences();}
    public void EditQuestion(){LaunchOverlay();Editor.ShowEditor();ApplyPrivacy();}
    public async Task ExportAnswerAsync()
    {
        if(controller is null||overlay is null)return;controller.DialogOpen=true;
        try
        {
            var dialog=new SaveFileDialog{Title="Export answer as Markdown",Filter="Markdown|*.md",FileName="Proctal-answer.md",AddExtension=true};
            if(dialog.ShowDialog(editor?.IsVisible==true?editor:overlay)==true)await controller.Safe(async()=>{await File.WriteAllTextAsync(dialog.FileName,"# "+controller.DisplayQuestion+"\n\n"+controller.DisplayAnswer);controller.ReportStatus("Answer exported as Markdown.");});
        }
        finally{controller.DialogOpen=false;}
    }
    public void ApplyPrivacy()
    {
        if(applyingPrivacy||controller is null||overlay is null)return;
        applyingPrivacy=true;
        try
        {
            var exclude=controller.Settings.CaptureExclusion;var results=Windows.Cast<Window>().Select(w=>WindowPrivacy.Apply(w,exclude)).ToArray();
            if(exclude)controller.ReportPrivacy(results.All(a=>a.Applied&&a.Affinity==0x11));
            else controller.ReportCaptureDisabled();
        }
        finally{applyingPrivacy=false;}
    }
    public void ShowDashboard(){if(controller is null)return;var setup=Dashboard;setup.Show();setup.WindowState=WindowState.Normal;setup.Activate();ApplyPrivacy();}
    public void ShowHistory(){ShowDashboard();Dashboard.Workspace.SelectedIndex=3;Dashboard.HistoryList.SelectedItem=controller?.VisibleHistory.FirstOrDefault();}
    public void LaunchOverlay(){dashboard?.Hide();editor?.Hide();ShowOverlay();}
    public void ShowOverlay(){if(overlay is null)return;if(capturing){showAfterCapture=true;return;}ApplyOpacity();overlay.Show();overlay.ApplyInteraction();ApplyPrivacy();}
    public void HideOverlay(){editor?.Hide();overlay?.Hide();}
    public void ToggleOverlay(){if(overlay?.IsVisible==true)HideOverlay();else ShowOverlay();}
    private async void HandleShortcut(ShortcutAction action)=>await PerformShortcutAsync(action);
    internal async Task PerformShortcutAsync(ShortcutAction action)
    {
        if(controller is null||IsExiting||controller.DialogOpen||(capturing&&action is not (ShortcutAction.Answer or ShortcutAction.AnswerImages or ShortcutAction.DeepAnswer or ShortcutAction.SolAnswer or ShortcutAction.CancelAnswer or ShortcutAction.Pause or ShortcutAction.ClearImages)))return;
        await controller.Safe(async()=>
        {
            switch(action)
            {
                case ShortcutAction.Toggle:ToggleOverlay();break;
                case ShortcutAction.Screenshot:await controller.CaptureAsync();break;
                case ShortcutAction.Answer:editor?.Hide();ShowOverlay();await controller.SubmitAsync();break;
                case ShortcutAction.AnswerImages:editor?.Hide();ShowOverlay();await controller.SubmitImagesAsync();break;
                case ShortcutAction.DeepAnswer:editor?.Hide();ShowOverlay();await controller.SubmitDeepAsync();break;
                case ShortcutAction.SolAnswer:editor?.Hide();ShowOverlay();await controller.SubmitSolAsync();break;
                case ShortcutAction.History:ShowHistory();break;
                case ShortcutAction.Interactive:controller.Interactive=!controller.Interactive;ShowOverlay();break;
                case ShortcutAction.Pause:await controller.ToggleListeningAsync();break;
                case ShortcutAction.Copy:Clipboard.SetText(controller.DisplayAnswer);controller.ReportStatus("Markdown answer copied to clipboard.");break;
                case ShortcutAction.CopyCode:if(overlay?.AnswerContent.CopyFirstCode()!=true)controller.ReportStatus("This answer has no code block to copy.");break;
                case ShortcutAction.ScrollEnd:controller.Overlay.AutoFollow=true;overlay?.AnswerContent.Scroll(action);break;
                case ShortcutAction.ScrollUp:case ShortcutAction.ScrollDown:case ShortcutAction.PageUp:case ShortcutAction.PageDown:case ShortcutAction.ScrollTop:overlay?.AnswerContent.Scroll(action);break;
                case ShortcutAction.ResizeLeft:overlay?.ResizeBy(-24,0);break;case ShortcutAction.ResizeRight:overlay?.ResizeBy(24,0);break;
                case ShortcutAction.ResizeUp:overlay?.ResizeBy(0,-24);break;case ShortcutAction.ResizeDown:overlay?.ResizeBy(0,24);break;
                case ShortcutAction.ZoomIn:controller.Overlay.FontSize++;break;case ShortcutAction.ZoomOut:controller.Overlay.FontSize--;break;
                case ShortcutAction.OpacityDown:controller.Overlay.Opacity-=.05;break;case ShortcutAction.OpacityUp:controller.Overlay.Opacity+=.05;break;
                case ShortcutAction.Compact:controller.Overlay.Compact=!controller.Overlay.Compact;break;
                case ShortcutAction.Captions:controller.Overlay.ShowCaptions=!controller.Overlay.ShowCaptions;break;
                case ShortcutAction.Tune:overlay?.SetTune(overlay.TunePanel.Visibility!=Visibility.Visible);break;
                case ShortcutAction.AutoAnswer:controller.Settings.AutoAnswer=!controller.Settings.AutoAnswer;break;
                case ShortcutAction.AlwaysOnTop:controller.Overlay.AlwaysOnTop=!controller.Overlay.AlwaysOnTop;break;
                case ShortcutAction.CaptionPageUp:case ShortcutAction.CaptionPageDown:case ShortcutAction.CaptionTop:case ShortcutAction.CaptionEnd:controller.Overlay.ShowCaptions=true;controller.Overlay.ExpandCaptions=true;overlay?.CaptionContent.Scroll(action);break;
                case ShortcutAction.Pin:controller.Pinned=!controller.Pinned;break;
                case ShortcutAction.PracticeQuestion:await controller.NextPracticeQuestionAsync();break;
                case ShortcutAction.ReviewAnswer:editor?.Hide();await controller.GradePracticeAnswerAsync();break;
                case ShortcutAction.ClearImages:controller.ClearImages();break;
                case ShortcutAction.CancelAnswer:controller.CancelAnswer();break;
                case ShortcutAction.WrapCode:controller.Overlay.CodeWrap=!controller.Overlay.CodeWrap;break;
                case ShortcutAction.Glass:controller.Overlay.Glass=!controller.Overlay.Glass;break;
                case ShortcutAction.Follow:controller.Overlay.AutoFollow=!controller.Overlay.AutoFollow;if(controller.Overlay.AutoFollow)overlay?.AnswerContent.ResumeFollowing();break;
                case ShortcutAction.PreviousAnswer:controller.BrowseHistory(-1);break;case ShortcutAction.NextAnswer:controller.BrowseHistory(1);break;
                case ShortcutAction.LatestAnswer:controller.ShowLatestAnswer();controller.Overlay.AutoFollow=true;overlay?.AnswerContent.ResumeFollowing();break;
                case ShortcutAction.Explain:await controller.FollowUpAsync("explain");break;case ShortcutAction.EdgeCases:await controller.FollowUpAsync("edges");break;case ShortcutAction.Optimize:await controller.FollowUpAsync("optimize");break;
                case ShortcutAction.ResetLayout:overlay?.ResetPosition();break;case ShortcutAction.EditQuestion:EditQuestion();break;
                case ShortcutAction.ExportAnswer:await ExportAnswerAsync();break;
                case ShortcutAction.Left:MoveOverlay(-24,0);break;
                case ShortcutAction.Right:MoveOverlay(24,0);break;
                case ShortcutAction.Up:MoveOverlay(0,-24);break;
                case ShortcutAction.Down:MoveOverlay(0,24);break;
            }
        });
    }
    private void MoveOverlay(double x,double y)
    {
        if(overlay is null)return;
        overlay.MoveBy(x,y);
    }
    internal async Task<ScreenshotItem> CaptureScreenshotAsync()
    {
        if(capturing)throw new InvalidOperationException("A screenshot is already being captured.");capturing=true;
        var visible=Windows.Cast<Window>().Where(w=>w.IsVisible).ToArray();
        try
        {
            foreach(var window in visible)window.Hide();
            await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);await Task.Delay(150);
            return await Task.Run(CaptureFrame);
        }
        finally
        {
            if(!IsExiting)foreach(var window in visible){var activate=window.ShowActivated;window.ShowActivated=false;window.Show();window.ShowActivated=activate;}
            capturing=false;
            if(showAfterCapture&&!IsExiting){showAfterCapture=false;ShowOverlay();}
        }
    }
    public async Task AttachImagesAsync(Window owner)
    {
        if(controller is null)return;controller.DialogOpen=true;
        try{var dialog=new OpenFileDialog{Title="Attach interview screenshots",Filter="Images|*.png;*.jpg;*.jpeg;*.bmp",Multiselect=true};if(dialog.ShowDialog(owner)==true)await controller.Safe(async()=>{foreach(var path in dialog.FileNames)controller.AddImage(await Task.Run(()=>ScreenshotService.Load(path)));});}
        finally{controller.DialogOpen=false;}
    }
    public async Task ExitAsync()
    {
        if(IsExiting)return;IsExiting=true;keyboard?.Dispose();keyboard=null;tray?.Dispose();tray=null;
        try{if(controller is not null)await controller.ShutdownAsync();}finally{Shutdown();}
    }
    protected override void OnExit(ExitEventArgs e)
    {
        launchWait?.Unregister(null);launchSignal?.Dispose();keyboard?.Dispose();tray?.Dispose();instance?.Dispose();base.OnExit(e);
    }
}
