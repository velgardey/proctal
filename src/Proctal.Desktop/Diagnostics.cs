using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
internal static class Diagnostics
{
    private sealed class LocalAnswerHandler:System.Net.Http.HttpMessageHandler
    {
        public string Body="";public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellation)
        {Requests++;Body=await request.Content!.ReadAsStringAsync(cancellation);return new(System.Net.HttpStatusCode.OK){Content=new StringContent("data: {\"type\":\"response.output_text.delta\",\"delta\":\"ready\"}\n\ndata: {\"type\":\"response.completed\"}\n\n",Encoding.UTF8,"text/event-stream")};}
    }
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern nint SendMessage(nint hwnd,int message,nint w,nint l);
    private sealed class BindingErrors:System.Diagnostics.TraceListener
    {
        public List<string> Errors {get;}=[];
        public override void Write(string? message){}
        public override void WriteLine(string? message){}
        public override void TraceEvent(System.Diagnostics.TraceEventCache? cache,string source,System.Diagnostics.TraceEventType type,int id,string? message){if(type==System.Diagnostics.TraceEventType.Error)Errors.Add(message??"");}
    }
    private static string ReportPath(string[] args)
    {var index=Array.FindIndex(args,s=>s is "--self-check" or "--check-azure");return index>=0&&index+1<args.Length?Path.GetFullPath(args[index+1]):Path.Combine(AppContext.BaseDirectory,"verification.json");}
    private static void Write(string[] args,object report)
    {var path=ReportPath(args);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));}
    public static void WriteFailure(string[] args,Exception error)=>Write(args,new{passed=false,error=args.Contains("--self-check")?error.ToString():error is CloudServiceException or InvalidDataException or ArgumentException?error.Message:"Check failed: "+error.GetType().Name});
    public static async Task SelfCheckAsync(App app,string[] args)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath(args))!);
        var directory=Path.GetDirectoryName(ReportPath(args))!;
        using var bindingErrors=new BindingErrors();System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        app.InitializeWindows(Path.Combine(directory,"profile-"+Guid.NewGuid().ToString("N")));
        var checks=new Dictionary<string,bool>();var overlay=app.Overlay;
        var savedOutput=app.Controller.SelectedOutput?.Id??"";var savedInput=app.Controller.SelectedInput?.Id??"";
        app.Controller.Settings.OutputDeviceId="unavailable-output";app.Controller.Settings.InputDeviceId="unavailable-input";app.Controller.SelectedOutput=null;app.Controller.SelectedInput=null;app.Controller.RefreshDevices();
        checks["missingSavedAudioDevicesNeedSelection"]=app.Controller.SelectedOutput is null&&app.Controller.SelectedInput is null;
        app.Controller.Settings.OutputDeviceId=savedOutput;app.Controller.Settings.InputDeviceId=savedInput;app.Controller.SelectedOutput=null;app.Controller.SelectedInput=null;app.Controller.RefreshDevices();
        checks["lazySetup"]=!app.HasDashboard&&app.MainWindow==overlay;
        app.ShowStartup();checks["firstRunSetup"]=app.Dashboard.IsVisible&&!overlay.IsVisible&&app.Dashboard.Workspace.SelectedIndex==2;
        checks["setupStartNavigation"]=(app.Dashboard.Workspace.Items[0] as System.Windows.Controls.TabItem)?.Header?.ToString()=="Start";
        checks["setupKeepsPracticeInOverlay"]=app.Dashboard.FindName("DashboardImageAnswerControl") is null;
        checks["setupConnectionTestAvailable"]=app.Dashboard.FindName("TestConnectionControl") is System.Windows.Controls.Button;
        checks["setupSolTestAvailable"]=app.Dashboard.FindName("TestSolConnectionControl") is System.Windows.Controls.Button;
        app.Dashboard.SettingsPages.SelectedIndex=1;app.Dashboard.UpdateLayout();
        checks["setupAnswerModeVisible"]=app.Dashboard.AnswerModePicker.IsVisible&&!app.Dashboard.AnswerOptions.IsExpanded;
        checks["setupInitialPromptVisible"]=app.Dashboard.AnswerInstructionsEditor.IsVisible&&!app.Dashboard.AnswerInstructionsEditor.IsReadOnly;
        app.Dashboard.SettingsPages.SelectedIndex=0;
        var savedEndpoint=app.Controller.Settings.Endpoint;app.Controller.Settings.Endpoint="invalid endpoint";
        app.Dashboard.TestConnectionControl.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));await Task.Delay(10);
        checks["setupValidationRestoresControls"]=app.Dashboard.TestConnectionControl.IsEnabled&&app.Dashboard.ConnectionFields.IsEnabled&&app.Dashboard.SaveSettingsControl.IsEnabled&&app.Dashboard.LaunchOverlayControl.IsEnabled&&app.Controller.Status.Contains("Azure");
        app.Controller.Settings.Endpoint=savedEndpoint;
        app.Dashboard.LaunchOverlayControl.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));checks["setupHandoff"]=app.Controller.HasSavedConfiguration&&overlay.IsVisible&&!app.Dashboard.IsVisible&&!app.Controller.IsListening;
        app.HideOverlay();app.ShowStartup();checks["configuredStartup"]=overlay.IsVisible&&!app.Dashboard.IsVisible;
        app.Controller.Settings.LaunchOverlayOnStartup=false;app.HideOverlay();app.ShowStartup();checks["startupOption"]=app.Dashboard.IsVisible&&!overlay.IsVisible;app.Controller.Settings.LaunchOverlayOnStartup=true;
        app.ShowOverlay();await Task.Delay(200);
        var moreBounds=WindowAppearance.PhysicalBounds(overlay);overlay.SetTune(true);overlay.UpdateLayout();
        checks["morePreservesGeometry"]=WindowAppearance.PhysicalBounds(overlay)==moreBounds&&overlay.TextAnswerControl.IsVisible;
        overlay.SetTune(false);
        var capture=WindowPrivacy.Apply(overlay,true);checks["captureAffinity"]=capture.Applied&&capture.Affinity==0x11;
        checks["overlayHoverPopupsBlocked"]=HoverHelpBlocked(overlay);
        checks["setupHoverPopupsBlocked"]=HoverHelpBlocked(app.Dashboard);
        overlay.ApplyInteraction();checks["readOnlyStyles"]=WindowPrivacy.HasReadOnlyStyles(overlay);
        app.Controller.Interactive=true;checks["interactiveStyles"]=!WindowPrivacy.HasReadOnlyStyles(overlay);checks["mouseModeNoActivate"]=WindowPrivacy.HasNoActivate(overlay);app.Controller.Interactive=false;
        var resizeBounds=WindowAppearance.PhysicalBounds(overlay);var resizeX=(int)(resizeBounds.Left+resizeBounds.Width-3);var resizeY=(int)(resizeBounds.Top+resizeBounds.Height-3);var coordinate=new nint((resizeY&0xffff)<<16|(resizeX&0xffff));
        app.Controller.Interactive=true;checks["mouseResizeEdges"]=SendMessage(new System.Windows.Interop.WindowInteropHelper(overlay).EnsureHandle(),0x84,0,coordinate)==17;app.Controller.Interactive=false;
        using(var hooks=new KeyboardControls(app.Dispatcher,_=>{}))checks["keyboardHook"]=true;
        var oldOpacity=app.Controller.Overlay.Opacity;var oldGlass=app.Controller.Overlay.Glass;
        app.Controller.Overlay.Glass=false;app.Controller.Overlay.Opacity=.45;overlay.ApplyPreferences();
        checks["nativeOpacityNoGlass"]=overlay.AllowsTransparency&&System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(overlay).EnsureHandle())?.CompositionTarget?.UsesPerPixelOpacity==true&&Math.Abs(overlay.Opacity-.45)<.01&&RenderedAlpha(overlay) is >=108 and <=122;
        app.Controller.Overlay.Opacity=1;overlay.ApplyPreferences();
        checks["nativeOpacityOpaque"]=RenderedAlpha(overlay)==255;
        app.Controller.Overlay.Glass=true;overlay.ApplyPreferences();checks["glassSurface"]=RenderedAlpha(overlay) is >=201 and <=207;
        app.Controller.Overlay.Glass=false;overlay.OpacityControl.SetCurrentValue(System.Windows.Controls.Primitives.RangeBase.ValueProperty,.6);overlay.UpdateLayout();
        checks["opacitySliderBinding"]=Math.Abs(app.Controller.Overlay.Opacity-.6)<.01&&RenderedAlpha(overlay) is >=148 and <=160;
        app.Controller.Overlay.Glass=oldGlass;app.Controller.Overlay.Opacity=oldOpacity;
        string? screenshotError=null;ScreenshotItem? image=null;
        try{image=await app.CaptureScreenshotAsync();checks["screenshot"]=image.Preview.PixelWidth>100&&image.Payload.DataUrl.StartsWith("data:image/png;base64,");}
        catch(System.ComponentModel.Win32Exception failure){screenshotError="Screen capture unavailable in this Windows session (error "+failure.NativeErrorCode+"). Run verify.ps1 in a normal, unlocked Windows session.";checks["screenshot"]=false;}
        checks["windowsRestored"]=overlay.IsVisible&&app.Dashboard.IsVisible;
        var testPixels=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{180,130,70,255,180,130,70,255,180,130,70,255,180,130,70,255},8);app.Controller.AddImage(image??ScreenshotService.FromImage(testPixels,"test.png"));checks["imageAttachment"]=app.Controller.Screenshots.Count==1;app.Controller.Screenshots.Clear();
        app.ToggleOverlay();checks["toggleHidden"]=!overlay.IsVisible;app.ToggleOverlay();checks["toggleShown"]=overlay.IsVisible;
        const string sample="```python\ndef two_sum(nums, target):\n    seen = {}\n    for i, value in enumerate(nums):\n        needed = target - value\n        if needed in seen:\n            return [seen[needed], i]\n        seen[value] = i\n    return []\n```\n\n## Why it works\nStore each visited value and check its complement before inserting. This also handles duplicate values.\n\n| Cost | Bound |\n| --- | --- |\n| Time | O(n) |\n| Space | O(n) |\n\n### Edge cases\n- Two equal values, such as `[3, 3]` with target `6`.\n- Negative values and zero.\n- No valid pair returns an empty list.\n\n> The lookup avoids a nested scan while preserving the original indexes.";
        const string mathSample="\n\n### Mathematical model\nThe brute-force cost is $n(n-1)/2$. A linear scan uses $O(n)$ lookups.\n\n\\[\\sum_{i=1}^{n} i = \\frac{n(n+1)}{2}\\]";
        overlay.AnswerContent.SetCurrentValue(MarkdownView.TextProperty,sample+mathSample);overlay.AnswerContent.FlushPendingRender();overlay.UpdateLayout();
        checks["nativeMath"]=EquationCount(overlay.AnswerContent)>0;
        ((System.Windows.Controls.TextBlock)overlay.QuestionScroller.Content).SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty,"Return two indexes whose values add up to the target.");
        checks["markdownCode"]=overlay.AnswerContent.CodeBlocks.Count==1;
        for(var i=0;i<3;i++)app.Controller.AddImage(ScreenshotService.FromImage(testPixels,$"status-{i}.png"));
        overlay.AnswerContent.Scroll(ShortcutAction.ScrollTop);await app.Controller.Safe(app.Controller.CaptureAsync);overlay.UpdateLayout();
        checks["visibleFailureWhileReading"]=HasVisibleText(overlay,app.Controller.Status)&&!overlay.AnswerContent.IsFollowing;
        app.Controller.History.Add(new AnswerRecord("status-old","Previous question","Previous answer",DateTimeOffset.UtcNow));app.Controller.History.Add(new AnswerRecord("status-new","Next question","Next answer",DateTimeOffset.UtcNow));app.Controller.BrowseHistory(-1);
        await app.Controller.Safe(app.Controller.CaptureAsync);overlay.UpdateLayout();checks["visibleFailureInHistory"]=HasVisibleText(overlay,app.Controller.Status)&&app.Controller.IsBrowsingHistory;
        app.Controller.History.Clear();app.Controller.ShowLatestAnswer();app.Controller.Screenshots.Clear();
        var foreground=GetForegroundWindow();var previousWidth=overlay.Width;var previousOpacity=app.Controller.Overlay.Opacity;
        await app.PerformShortcutAsync(ShortcutAction.ResizeRight);await app.PerformShortcutAsync(ShortcutAction.OpacityDown);await app.PerformShortcutAsync(ShortcutAction.PageDown);await app.PerformShortcutAsync(ShortcutAction.ScrollUp);
        checks["keyboardResize"]=overlay.Width>previousWidth;checks["opacityControl"]=app.Controller.Overlay.Opacity<previousOpacity;
        checks["keyboardNoActivate"]=GetForegroundWindow()==foreground&&WindowPrivacy.HasNoActivate(overlay);
        checks["readingPosition"]=!overlay.AnswerContent.IsFollowing;
        var bounds=WindowAppearance.PhysicalBounds(overlay);
        app.Controller.Overlay.Left=12345;
        overlay.ApplyPreferences();
        checks["physicalPlacement"]=Math.Abs(WindowAppearance.PhysicalBounds(overlay).Left-bounds.Left)<=2;
        var previousLeft=WindowAppearance.PhysicalBounds(overlay).Left;await app.PerformShortcutAsync(ShortcutAction.Left);checks["keyboardMove"]=WindowAppearance.PhysicalBounds(overlay).Left<previousLeft;
        app.Controller.Overlay.ShowCaptions=false;checks["captionVisibility"]=overlay.CaptionPanel.Visibility==Visibility.Collapsed;
        app.Controller.Overlay.ShowCaptions=true;var accept=typeof(ProctalController).GetMethod("AcceptTranscriptionEvent",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        accept.Invoke(app.Controller,new object[]{"{\"type\":\"conversation.item.input_audio_transcription.delta\",\"item_id\":\"preview\",\"delta\":\"How would you improve the nested scan?\"}",AudioSource.Interviewer});
        overlay.CaptionContent.FlushPendingRender();checks["liveCaptionBinding"]=overlay.CaptionContent.Text.Contains("Interviewer (live): How would you improve");
        await app.PerformShortcutAsync(ShortcutAction.CaptionTop);checks["captionPaging"]=!overlay.CaptionContent.IsFollowing&&GetForegroundWindow()==foreground;
        await app.PerformShortcutAsync(ShortcutAction.CaptionEnd);checks["captionFollow"]=overlay.CaptionContent.IsFollowing;
        app.Controller.Settings.AutoAnswer=false;overlay.UpdateLayout();checks["autoAnswerBinding"]=overlay.AutoAnswerControl.IsChecked==false;
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsListening))!.SetValue(app.Controller,true);overlay.UpdateLayout();checks["audioOptionsLocked"]=!overlay.MicrophoneControl.IsEnabled;typeof(ProctalController).GetProperty(nameof(ProctalController.IsListening))!.SetValue(app.Controller,false);
        app.Controller.Overlay.FontSize=20;app.Controller.Overlay.CodeWrap=true;app.Controller.Overlay.AutoFollow=false;app.Controller.Overlay.AlwaysOnTop=false;overlay.UpdateLayout();
        checks["answerPreferences"]=overlay.AnswerContent.FontSize==20&&overlay.AnswerContent.CodeWrap&&!overlay.AnswerContent.AutoFollow&&!overlay.Topmost;
        var brush=overlay.Resources["BackgroundBrush"];var stationary=WindowAppearance.PhysicalBounds(overlay);await app.PerformShortcutAsync(ShortcutAction.OpacityDown);
        checks["appearanceDoesNotRebuildLayout"]=ReferenceEquals(brush,overlay.Resources["BackgroundBrush"])&&WindowAppearance.PhysicalBounds(overlay).Left==stationary.Left;
        await app.PerformShortcutAsync(ShortcutAction.ScrollEnd);checks["endResumesFollowing"]=app.Controller.Overlay.AutoFollow&&overlay.AnswerContent.IsFollowing;
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsStarting))!.SetValue(app.Controller,true);overlay.UpdateLayout();checks["connectionCanBeCancelled"]=overlay.ListenControl.IsEnabled&&overlay.ListenControl.ToolTip.ToString()!.Contains("Cancel");typeof(ProctalController).GetProperty(nameof(ProctalController.IsStarting))!.SetValue(app.Controller,false);
        var speechShortcuts=new ShortcutPolicy();var speechForeground=GetForegroundWindow();
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsStarting))!.SetValue(app.Controller,true);
        await app.PerformShortcutAsync(speechShortcuts.Accept(0x77,true,false));
        checks["f8CancelsConnection"]=!app.Controller.IsStarting&&app.Controller.CanToggleListen&&GetForegroundWindow()==speechForeground;
        speechShortcuts.Accept(0x77,false,false);
        typeof(ProctalController).GetProperty(nameof(ProctalController.IsListening))!.SetValue(app.Controller,true);app.HideOverlay();
        await app.PerformShortcutAsync(speechShortcuts.Accept(0x77,true,false,overlayVisible:false));
        checks["f8PausesSpeechWithoutShowingOverlay"]=!app.Controller.IsListening&&!app.Controller.IsStarting&&!overlay.IsVisible&&GetForegroundWindow()==speechForeground;
        app.ShowOverlay();
        using var localHandler=new LocalAnswerHandler();using var localHttp=new HttpClient(localHandler);app.Controller.LunaFactory=s=>new LunaClient(s,"synthetic-test-key",localHttp);
        var solForeground=GetForegroundWindow();app.Controller.ManualText="Explain a depth-first search";
        await app.PerformShortcutAsync(new ShortcutPolicy().Accept(0x75,true,false));
        checks["f6UsesSolWithoutFocus"]=localHandler.Body.Contains("gpt-6.1-sol")&&app.Controller.Settings.AnswerDeployment=="gpt-6-luna"&&GetForegroundWindow()==solForeground;
        await app.Controller.SubmitAsync();checks["f11StillUsesLuna"]=localHandler.Body.Contains("gpt-6-luna");
        var frameReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var allowFrame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var originalFrame=app.CaptureFrame;
        var pendingImage=ScreenshotService.FromImage(testPixels,"pending.png");
        app.CaptureFrame=()=>{frameReady.TrySetResult();if(!allowFrame.Task.Wait(TimeSpan.FromSeconds(3)))throw new TimeoutException();return pendingImage;};
        var pendingCapture=app.Controller.CaptureAsync();
        var shortcutTime=1000L;var answerShortcuts=new ShortcutPolicy(()=>shortcutTime);
        try
        {
            await frameReady.Task.WaitAsync(TimeSpan.FromSeconds(2));await app.PerformShortcutAsync(answerShortcuts.Accept(0x7A,true,false)).WaitAsync(TimeSpan.FromSeconds(1));
            checks["speechShortcutIndependentOfCapture"]=!pendingCapture.IsCompleted&&!localHandler.Body.Contains("input_image")&&app.Controller.Answer=="ready";
            checks["overlayStaysHiddenDuringCapture"]=!overlay.IsVisible;
        }
        finally{allowFrame.TrySetResult();await pendingCapture;app.CaptureFrame=originalFrame;}
        checks["captureRestoresAfterTextAnswer"]=overlay.IsVisible&&app.Controller.Screenshots.Count==1;
        answerShortcuts.Accept(0x7A,false,false);shortcutTime=1200;
        var doubleF11=answerShortcuts.Accept(0x7A,true,false);
        await app.PerformShortcutAsync(doubleF11);checks["explicitImageShortcut"]=localHandler.Body.Contains("input_image")&&app.Controller.Screenshots.Count==0;
        checks["imageReceivesSessionContext"]=localHandler.Body.Contains("COMPLETED QUESTION/ANSWER PAIRS")&&localHandler.Body.Contains("Answer: ready");
        checks["doubleF11ImageShortcut"]=doubleF11==ShortcutAction.AnswerImages&&checks["explicitImageShortcut"]&&GetForegroundWindow()==foreground;
        var imageFrameReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var releaseImageFrame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        app.CaptureFrame=()=>{imageFrameReady.TrySetResult();if(!releaseImageFrame.Task.Wait(TimeSpan.FromSeconds(3)))throw new TimeoutException();return pendingImage;};
        var f9Capture=app.Controller.CaptureAsync();Task? f9Answer=null;
        try
        {
            await imageFrameReady.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var f9=answerShortcuts.Accept(0x78,true,false,overlayVisible:false);
            f9Answer=app.PerformShortcutAsync(f9);
            checks["f9WaitsForPendingImage"]=f9==ShortcutAction.AnswerImages&&!f9Answer.IsCompleted&&!overlay.IsVisible;
        }
        finally{releaseImageFrame.TrySetResult();await f9Capture;app.CaptureFrame=originalFrame;}
        if(f9Answer is not null)await f9Answer.WaitAsync(TimeSpan.FromSeconds(2));
        checks["f9ImageShortcut"]=localHandler.Body.Contains("input_image")&&app.Controller.Screenshots.Count==0&&overlay.IsVisible&&GetForegroundWindow()==foreground;
        app.Controller.ManualText="Solve a small recurrence";var deepForeground=GetForegroundWindow();var normalEffort=app.Controller.Settings.ReasoningEffort;
        await app.PerformShortcutAsync(answerShortcuts.Accept(0x76,true,false));
        using(var deepBody=JsonDocument.Parse(localHandler.Body))checks["f7DeepRequest"]=deepBody.RootElement.GetProperty("reasoning").GetProperty("effort").GetString()=="xhigh"&&!deepBody.RootElement.TryGetProperty("max_output_tokens",out _);
        checks["deepReceivesSessionContext"]=localHandler.Body.Contains("COMPLETED QUESTION/ANSWER PAIRS")&&localHandler.Body.Contains("Answer: ready");
        checks["f7NoActivateOrSettingsChange"]=GetForegroundWindow()==deepForeground&&app.Controller.Settings.ReasoningEffort==normalEffort;
        var deepFrameReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var releaseDeepFrame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var requestsBeforeDeep=localHandler.Requests;
        app.Controller.ManualText="Explain this diagram using C#";app.CaptureFrame=()=>{deepFrameReady.TrySetResult();if(!releaseDeepFrame.Task.Wait(TimeSpan.FromSeconds(3)))throw new TimeoutException();return pendingImage;};
        var deepCapture=app.Controller.CaptureAsync();Task? firstDeep=null;Task? secondDeep=null;
        try
        {
            await deepFrameReady.Task.WaitAsync(TimeSpan.FromSeconds(2));var repeatedDeep=new ShortcutPolicy();firstDeep=app.PerformShortcutAsync(repeatedDeep.Accept(0x76,true,false));repeatedDeep.Accept(0x76,false,false);secondDeep=app.PerformShortcutAsync(repeatedDeep.Accept(0x76,true,false));
            checks["repeatedF7WaitsForCapture"]=!firstDeep.IsCompleted&&!secondDeep.IsCompleted&&!overlay.IsVisible;
        }
        finally{releaseDeepFrame.TrySetResult();await deepCapture;app.CaptureFrame=originalFrame;}
        if(firstDeep is not null)await firstDeep;if(secondDeep is not null)await secondDeep;
        checks["repeatedF7KeepsPrompt"]=localHandler.Requests==requestsBeforeDeep+1&&app.Controller.Question=="Explain this diagram using C#"&&app.Controller.Screenshots.Count==0&&GetForegroundWindow()==deepForeground;
        app.Controller.AddImage(pendingImage);app.ShowDashboard();app.Dashboard.Workspace.SelectedIndex=0;app.Dashboard.UpdateLayout();
        app.Dashboard.LaunchOverlayControl.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        checks["setupLaunchPreservesImages"]=app.Controller.Screenshots.Count==1&&!app.Dashboard.IsVisible&&overlay.IsVisible;
        await app.PerformShortcutAsync(ShortcutAction.AnswerImages);checks["overlayImageAnswerAfterSetup"]=localHandler.Body.Contains("input_image")&&app.Controller.Screenshots.Count==0;
        overlay.AnswerContent.SetCurrentValue(MarkdownView.TextProperty,sample);overlay.AnswerContent.FlushPendingRender();
        app.Controller.Overlay.FontSize=16;app.Controller.Overlay.AutoFollow=true;app.Controller.Overlay.AlwaysOnTop=true;
        app.EditQuestion();var editorPrivacy=WindowPrivacy.Apply(app.Editor,true);checks["questionEditor"]=app.Editor.IsVisible&&!app.Dashboard.IsVisible&&editorPrivacy.Applied&&editorPrivacy.Affinity==0x11;
        checks["editorHoverPopupsBlocked"]=HoverHelpBlocked(app.Editor);
        app.HideOverlay();checks["editorHidesWithOverlay"]=!app.Editor.IsVisible&&!overlay.IsVisible;app.ShowOverlay();await app.PerformShortcutAsync(new ShortcutPolicy().Accept(0x48,true,false,ctrl:true,alt:true));
        checks["historyShortcut"]=app.Dashboard.IsVisible&&app.Dashboard.Workspace.SelectedIndex==3&&app.Dashboard.HistoryList.SelectedItem is AnswerRecord;
        app.Dashboard.Workspace.SelectedIndex=0;
        overlay.AnswerContent.Scroll(ShortcutAction.ScrollTop);
        app.Controller.ReportStatus("Preview answer · no cloud request was sent.");
        app.Controller.ManualText="Return two indexes whose values add up to the target.";
        app.Controller.History.Add(new AnswerRecord("preview",app.Controller.ManualText,sample,DateTimeOffset.UtcNow));
        app.Controller.Documents.Add(new ContextDocument("preview","Practice context","Backend engineer · Python, C#, APIs and distributed systems.\n\nPractice focus: coding questions and explaining tradeoffs."));
        app.Controller.Settings.Instructions="Use Python for coding answers. Explain the important variables, loops and conditions, show a small worked example, and finish with a short explanation I can say aloud.";
        ((System.Windows.Controls.TextBlock)overlay.QuestionScroller.Content).SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty,app.Controller.ManualText);
        app.Controller.Overlay.ExpandCaptions=true;Render(overlay,Path.Combine(directory,"overlay-captions-expanded.png"));app.Controller.Overlay.ExpandCaptions=false;
        Render(app.Dashboard,Path.Combine(directory,"dashboard.png"));Render(overlay,Path.Combine(directory,"overlay.png"));
        checks["primaryAnswerReady"]=overlay.TextAnswerControl.IsEnabled;
        RenderBackdrop(overlay,Path.Combine(directory,"overlay-light-backdrop.png"),"#EEF0F3");RenderBackdrop(overlay,Path.Combine(directory,"overlay-dark-backdrop.png"),"#121316");
        overlay.SetTune(true);Render(overlay,Path.Combine(directory,"overlay-more.png"));overlay.SetTune(false);
        overlay.AnswerContent.SetCurrentValue(MarkdownView.TextProperty,"## Intuitive progression\n\nStart by counting each pair: $\\binom{n}{2} = \\frac{n(n-1)}{2}$.\n\nA sum gives the same bound:\n\n$$\n\\sum_{i=1}^{n-1} i = \\frac{n(n-1)}{2}\n$$\n\n### Optimized code\n```python\ndef pair_count(n: int) -> int:\n    return n * (n - 1) // 2\n```\n\n### Matrix example\n```math\n\\begin{pmatrix}1 & 2 \\\\ 3 & 4\\end{pmatrix}\n```\n\n**Cost:** $O(1)$ time and space.");overlay.AnswerContent.FlushPendingRender();Render(overlay,Path.Combine(directory,"overlay-math.png"));
        checks["mathAndCodeTogether"]=EquationCount(overlay.AnswerContent)>=3&&overlay.AnswerContent.CodeBlocks.Count==1;
        overlay.AnswerContent.SetCurrentValue(MarkdownView.TextProperty,"## Probability and scale\n\n$ /text{sigmoid}(x) = /frac{1}{1+e^{-x}} $\n\n### Scientific notation\n$$6.02 \\times 10^{23}$$\n\nA small value: $2.5 \\times 10^{-3}$.\n\n### Fractions\n$\\dfrac{1}{2} + \\tfrac{3}{4}$\n\nBare fraction: \\frac{n(n+1)}{2}.\n\n```python\nprobability = 1 / (1 + math.exp(-x))\npattern = r'\\frac{a}{b}'\n```");overlay.AnswerContent.FlushPendingRender();
        checks["fractionAndScientificVariants"]=EquationCount(overlay.AnswerContent)==5&&overlay.AnswerContent.CodeBlocks.Count==1;
        Render(overlay,Path.Combine(directory,"overlay-math-variants.png"));
        checks["mathHoverPopupsBlocked"]=HoverHelpBlocked(overlay);
        overlay.AnswerContent.IsDark=false;Render(overlay,Path.Combine(directory,"overlay-math-light-text.png"));overlay.AnswerContent.IsDark=true;
        overlay.AnswerContent.SetCurrentValue(MarkdownView.TextProperty,sample);overlay.AnswerContent.FlushPendingRender();
        for(var tab=0;tab<4;tab++){app.Dashboard.Workspace.SelectedIndex=tab;app.Dashboard.UpdateLayout();Render(app.Dashboard,Path.Combine(directory,$"dashboard-tab-{tab}.png"));}
        app.Dashboard.Workspace.SelectedIndex=2;
        for(var page=0;page<3;page++){app.Dashboard.SettingsPages.SelectedIndex=page;Render(app.Dashboard,Path.Combine(directory,$"settings-{page}.png"));}
        app.Dashboard.SettingsPages.SelectedIndex=1;app.Dashboard.AnswerOptions.IsExpanded=true;Render(app.Dashboard,Path.Combine(directory,"settings-answer-options.png"));app.Dashboard.AnswerOptions.IsExpanded=false;
        app.Dashboard.SettingsPages.SelectedIndex=2;app.Dashboard.SharingOptions.IsExpanded=true;app.Dashboard.ShortcutOptions.IsExpanded=true;Render(app.Dashboard,Path.Combine(directory,"settings-appearance-options.png"));app.Dashboard.SharingOptions.IsExpanded=false;app.Dashboard.ShortcutOptions.IsExpanded=false;
        app.Dashboard.Workspace.SelectedIndex=3;app.Dashboard.HistoryList.SelectedIndex=app.Controller.History.Count-1;app.Dashboard.HistoryText.FlushPendingRender();Render(app.Dashboard,Path.Combine(directory,"history-populated.png"));
        app.Dashboard.Width=app.Dashboard.MinWidth;app.Dashboard.Height=app.Dashboard.MinHeight;
        for(var tab=0;tab<4;tab++){app.Dashboard.Workspace.SelectedIndex=tab;app.Dashboard.UpdateLayout();Render(app.Dashboard,Path.Combine(directory,$"dashboard-min-tab-{tab}.png"));}
        app.Dashboard.Workspace.SelectedIndex=2;
        for(var page=0;page<3;page++){app.Dashboard.SettingsPages.SelectedIndex=page;Render(app.Dashboard,Path.Combine(directory,$"settings-min-{page}.png"));}
        var launchOrigin=app.Dashboard.LaunchOverlayControl.TransformToAncestor(app.Dashboard).Transform(new Point());
        checks["setupMinimumLaunchVisible"]=app.Dashboard.LaunchOverlayControl.IsVisible&&app.Dashboard.LaunchOverlayControl.ActualWidth>=100&&launchOrigin.X>=0&&launchOrigin.Y>=0&&launchOrigin.X+app.Dashboard.LaunchOverlayControl.ActualWidth<=app.Dashboard.ActualWidth&&launchOrigin.Y+app.Dashboard.LaunchOverlayControl.ActualHeight<=app.Dashboard.ActualHeight;
        app.Dashboard.SettingsPages.SelectedIndex=1;app.Dashboard.UpdateLayout();var promptOrigin=app.Dashboard.AnswerInstructionsEditor.TransformToAncestor(app.Dashboard.AnswerSettingsScroll).Transform(new Point());checks["setupMinimumInitialPromptVisible"]=promptOrigin.Y>=0&&promptOrigin.Y+app.Dashboard.AnswerInstructionsEditor.ActualHeight<=app.Dashboard.AnswerSettingsScroll.ViewportHeight;app.Dashboard.AnswerOptions.IsExpanded=true;Render(app.Dashboard,Path.Combine(directory,"settings-answer-options-min.png"));app.Dashboard.AnswerOptions.IsExpanded=false;
        ((System.Windows.Controls.TextBlock)overlay.QuestionScroller.Content).SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty,"Return two indexes whose values add up to the target.");
        overlay.Width=overlay.MinWidth;overlay.Height=overlay.MinHeight;Render(overlay,Path.Combine(directory,"overlay-min.png"));
        checks["minimumAnswerViewport"]=overlay.AnswerContent.ActualHeight>=140;
        overlay.SetTune(true);Render(overlay,Path.Combine(directory,"overlay-tune-min.png"));checks["tuneAnswerViewport"]=overlay.AnswerContent.ActualHeight>=140&&overlay.Composer.Visibility==Visibility.Collapsed;overlay.SetTune(false);
        var savedCaptionHeight=app.Controller.Overlay.CaptionHeight;app.Controller.Overlay.CaptionHeight=180;app.Controller.Overlay.ExpandCaptions=true;overlay.SetTune(true);overlay.MinHeight=1;overlay.Height=600;overlay.UpdateLayout();
        checks["smallWindowAnswerViewport"]=overlay.AnswerContent.ActualHeight>=140;Render(overlay,Path.Combine(directory,"overlay-small-tune.png"));
        checks["opacitySurvivesResize"]=Math.Abs(RenderedAlpha(overlay)-(app.Controller.Overlay.Glass?204:255)*overlay.Opacity)<=3;
        app.Controller.Overlay.CaptionHeight=savedCaptionHeight;app.Controller.Overlay.ExpandCaptions=false;overlay.SetTune(false);overlay.ApplyPreferences();
        for(var i=0;i<3;i++)app.Controller.AddImage(ScreenshotService.FromImage(testPixels,$"sample-{i+1}.png"));Render(overlay,Path.Combine(directory,"overlay-images-min.png"));
        app.Controller.Overlay.Compact=true;overlay.Width=overlay.MinWidth;overlay.Height=overlay.MinHeight;Render(overlay,Path.Combine(directory,"overlay-compact.png"));checks["compactAnswerViewport"]=overlay.AnswerContent.ActualHeight>=140;checks["compactPrimaryAnswerAvailable"]=overlay.TextAnswerControl.IsVisible;app.Controller.Screenshots.Clear();
        checks["viewBindings"]=bindingErrors.Errors.Count==0;System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
        Write(args,new{passed=checks.Values.All(v=>v),checks,screenshotError,bindingErrors=bindingErrors.Errors,captureError=capture.Error,outputDevices=app.Controller.OutputDevices.Count,inputDevices=app.Controller.InputDevices.Count,cloud="not run",receiverSideSharing="not run"});
        await app.ExitAsync();
    }
    private static void Render(Window window,string path)
    {
        window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
    private static int EquationCount(DependencyObject parent)
    {
        var count=parent is System.Windows.Controls.Image {Source:DrawingImage}?1:0;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)count+=EquationCount(VisualTreeHelper.GetChild(parent,i));return count;
    }
    private static bool HasVisibleText(DependencyObject parent,string text)
    {
        if(parent is System.Windows.Controls.TextBlock block&&block.IsVisible&&block.Text==text)return true;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)if(HasVisibleText(VisualTreeHelper.GetChild(parent,i),text))return true;return false;
    }
    private static bool HoverHelpBlocked(DependencyObject parent)
    {
        if(parent is FrameworkElement element)
        {
            // Raise the actual direct service event; a window-level handler would miss it.
            var opening=(System.Windows.Controls.ToolTipEventArgs)Activator.CreateInstance(typeof(System.Windows.Controls.ToolTipEventArgs),System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,[true],null)!;
            element.RaiseEvent(opening);if(!opening.Handled)return false;
        }
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)if(!HoverHelpBlocked(VisualTreeHelper.GetChild(parent,i)))return false;
        return true;
    }
    private static byte RenderedAlpha(Window window)
    {
        window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
        var surface=window.FindName("AnswerSurface") as FrameworkElement;var sample=surface?.TransformToAncestor(window).Transform(new Point(8,Math.Min(100,surface.ActualHeight-8)))??new Point(10,100);
        var pixel=new byte[4];bitmap.CopyPixels(new Int32Rect((int)sample.X,(int)sample.Y,1,1),pixel,4,0);return pixel[3];
    }
    private static void RenderBackdrop(Window window,string path,string color)
    {
        window.UpdateLayout();var width=(int)Math.Ceiling(window.ActualWidth);var height=(int)Math.Ceiling(window.ActualHeight);var actual=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);actual.Render(window);
        var drawing=new DrawingVisual();using(var context=drawing.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),null,new Rect(0,0,width+48,height+48));
            var stroke=new Pen(new SolidColorBrush(Color.FromArgb(20,128,128,128)),1);for(var line=80;line<height+48;line+=40)context.DrawLine(stroke,new Point(0,line),new Point(width+48,line));
            context.DrawImage(actual,new Rect(24,24,width,height));
        }
        var bitmap=new RenderTargetBitmap(width+48,height+48,96,96,PixelFormats.Pbgra32);bitmap.Render(drawing);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
    public static async Task CheckAzureAsync(string[] args)
    {
        if(args.Contains("--sol"))
        {
            var solData=new AppDataStore();var solSettings=AzureSettings.Load(solData.SettingsPath);solSettings.AnswerDeployment=solSettings.SolDeployment;solSettings.ReasoningEffort="high";
            using var solCancellation=new CancellationTokenSource(TimeSpan.FromMinutes(5));var solResult=new StringBuilder();var clock=System.Diagnostics.Stopwatch.StartNew();double? firstText=null;
            var solClient=new LunaClient(solSettings,solData.Key());
            await foreach(var part in solClient.StreamAnswerAsync("Reply exactly: ready",[],"","Connection check",solCancellation.Token)){firstText??=clock.Elapsed.TotalSeconds;solResult.Append(part);}
            Write(args,new{passed=solResult.ToString().Trim()=="ready",answerDeployment=solSettings.AnswerDeployment,reasoning=solSettings.ReasoningEffort,firstTextSeconds=firstText,elapsedSeconds=clock.Elapsed.TotalSeconds,audioSent=false,privateContextSent=false});return;
        }
        if(args.Contains("--session"))
        {
            var sessionData=new AppDataStore();var sessionSettings=AzureSettings.Load(sessionData.SettingsPath);sessionSettings.ReasoningEffort="none";var key=sessionData.Key();
            var reportDirectory=Path.GetDirectoryName(ReportPath(args))!;var profile=Path.Combine(reportDirectory,"synthetic-session-"+Guid.NewGuid().ToString("N"));
            var controller=new ProctalController(System.Windows.Threading.Dispatcher.CurrentDispatcher,profile);controller.LunaFactory=_=>new LunaClient(sessionSettings,key);
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await controller.AnswerAsync("Synthetic problem A: sorted list [1,3,5,7,9], target sum 10. Reply exactly ACK; retain these facts for later.",false,"Connection check");
                for(var i=0;i<4;i++)await controller.AnswerAsync("Unrelated synthetic topic "+i+": name the SQL keyword for retrieving rows. Reply exactly SELECT.",false,"Connection check");
                await controller.AnswerAsync("Return the two pairs in original problem A whose sum is its original target, as a JSON array of arrays only, smaller number first in each pair and pairs sorted. Use original problem A rather than the unrelated topics.",false,"Connection check");
                var answer=System.Text.RegularExpressions.Regex.Replace(controller.Answer,@"\s","");var recalled=answer.Contains("[[1,9],[3,7]]",StringComparison.Ordinal);
                await controller.AnswerAsync("New independent problem B: list [2,4], target sum 6. Ignore problem A for this question. Return its one pair as a JSON array of arrays only.",false,"Connection check");
                var independent=System.Text.RegularExpressions.Regex.Replace(controller.Answer,@"\s","").Contains("[[2,4]]",StringComparison.Ordinal);
                using var sessionCancellation=new CancellationTokenSource(TimeSpan.FromSeconds(45));await LiveTranscriptionClient.CheckAsync(sessionSettings,key,sessionCancellation.Token);
                Write(args,new{passed=recalled&&independent,earlierProblemRecalled=recalled,independentProblemScoped=independent,completedAnswers=controller.History.Count,answerDeployment=sessionSettings.AnswerDeployment,transcriptionDeployment=sessionSettings.TranscriptionDeployment,reasoning="none",elapsedSeconds=clock.Elapsed.TotalSeconds,audioSent=false,privateContextSent=false,historySaved=false});
            }
            finally{await controller.ShutdownAsync();}
            return;
        }
        if(args.Contains("--deep"))
        {
            var deepData=new AppDataStore();var deepSettings=AzureSettings.Load(deepData.SettingsPath);deepSettings.ReasoningEffort="xhigh";
            using var deepCancellation=new CancellationTokenSource(TimeSpan.FromMinutes(5));var deepResult=new StringBuilder();var deepTiers=new List<string>();var clock=System.Diagnostics.Stopwatch.StartNew();double? firstText=null;
            var deepClient=new LunaClient(deepSettings,deepData.Key());deepClient.ProcessingTier+=deepTiers.Add;
            await foreach(var part in deepClient.StreamAnswerAsync("Reply exactly: ready",[],"","Connection check",deepCancellation.Token)){firstText??=clock.Elapsed.TotalSeconds;deepResult.Append(part);}
            Write(args,new{passed=deepResult.ToString().Trim()=="ready",answerDeployment=deepSettings.AnswerDeployment,reasoning="xhigh",outputLimit="Azure model default",answer=deepResult.ToString(),firstTextSeconds=firstText,elapsedSeconds=clock.Elapsed.TotalSeconds,processingTiers=deepTiers,audioSent=false,privateContextSent=false});return;
        }
        var data=new AppDataStore();var settings=AzureSettings.Load(data.SettingsPath);using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(60));var result=new StringBuilder();var tiers=new List<string>();var client=new LunaClient(settings,data.Key());client.ProcessingTier+=tiers.Add;
        var pixels=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255,255,0,0,255,255,0,0,255,255,0,0,255},8);
        var image=ScreenshotService.FromImage(pixels,"connection-check.png");
        await foreach(var part in client.StreamAnswerAsync("Confirm that the test image is blue in one short sentence.",[],"","Connection check",cancellation.Token,[image.Payload]))result.Append(part);
        await LiveTranscriptionClient.CheckAsync(settings,data.Key(),cancellation.Token);
        Write(args,new{passed=result.Length>0,answerDeployment=settings.AnswerDeployment,transcriptionDeployment=settings.TranscriptionDeployment,visionAnswer=result.ToString(),processingTiers=tiers,reasoning=settings.ReasoningEffort,audioSent=false});
    }
}
