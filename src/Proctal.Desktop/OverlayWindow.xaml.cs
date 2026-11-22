using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
public partial class OverlayWindow:Window
{
    private readonly ProctalController controller;
    private bool applying;
    private App Host=>(App)Application.Current;
    public OverlayWindow(ProctalController controller)
    {
        this.controller=controller;InitializeComponent();DataContext=controller;
        Left=Math.Max(SystemParameters.WorkArea.Left,SystemParameters.WorkArea.Right-Width-24);Top=SystemParameters.WorkArea.Top+24;
        SourceInitialized+=(_,_)=>{WindowAppearance.KeepNonactivating(this);ApplyPreferences();ApplyInteraction();Host.ApplyPrivacy();};
        controller.Overlay.PropertyChanged+=PreferenceChanged;
        controller.Screenshots.CollectionChanged+=(_,_)=>{if(IsLoaded)FitContent(this,controller.Overlay.Compact,TunePanel.Visibility==Visibility.Visible,controller.Overlay.CaptionHeight);};
        controller.PropertyChanged+=(_,e)=>
        {
            if(e.PropertyName==nameof(ProctalController.IsAnswering)){if(!controller.IsAnswering)AnswerContent.FlushPendingRender();else if(controller.Overlay.AutoFollow)AnswerContent.ResumeFollowing();}
            if(e.PropertyName==nameof(ProctalController.IsCapturing)&&IsLoaded)FitContent(this,controller.Overlay.Compact,TunePanel.Visibility==Visibility.Visible,controller.Overlay.CaptionHeight);
            if(e.PropertyName is nameof(ProctalController.Status) or nameof(ProctalController.IsAnswering))UpdateReadingState();
        };
        CaptionContent.ReadingPositionChanged+=()=>CaptionExpandControl.ToolTip=CaptionContent.IsFollowing?"Expand or collapse captions · following live":"Reading captions · Ctrl+Alt+Shift+End to follow";
        Loaded+=(_,_)=>ApplyPreferences();
        SizeChanged+=(_,_)=>{SaveGeometry();if(IsLoaded&&!applying)FitContent(this,controller.Overlay.Compact,TunePanel.Visibility==Visibility.Visible,controller.Overlay.CaptionHeight);};LocationChanged+=(_,_)=>SaveGeometry();
        AnswerContent.ReadingPositionChanged+=UpdateReadingState;
        AnswerContent.CodeCopied+=()=>controller.ReportStatus("Code copied without Markdown fences.");
        Closing+=(_,e)=>{if(!Host.IsExiting){e.Cancel=true;Hide();}};
        Closed+=(_,_)=>controller.Overlay.PropertyChanged-=PreferenceChanged;
    }
    private void PreferenceChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(applying||!IsLoaded)return;
        switch(e.PropertyName)
        {
            case nameof(OverlayPreferences.Opacity):WindowAppearance.ApplyOpacity(this,controller.Overlay.Opacity);break;
            case nameof(OverlayPreferences.Glass):ApplySurface();break;
            case nameof(OverlayPreferences.AlwaysOnTop):Topmost=controller.Overlay.AlwaysOnTop;ApplyInteraction();break;
            case nameof(OverlayPreferences.AutoFollow):if(controller.Overlay.AutoFollow)AnswerContent.ResumeFollowing();break;
            case nameof(OverlayPreferences.FontSize):case nameof(OverlayPreferences.CodeWrap):break;
            default:ApplyPreferences();break;
        }
    }
    public void ApplyPreferences()
    {
        if(applying)return;applying=true;
        try
        {
            var p=controller.Overlay;var monitor=WindowAppearance.MonitorArea(this,p.PhysicalLeft,p.PhysicalTop);var work=monitor.Work;
            var tuning=TunePanel.Visibility==Visibility.Visible;
            MinWidth=Math.Min(480,work.Width);MinHeight=Math.Min(430,work.Height);
            var layout=new OverlayPlacement(Math.Max(p.Width,MinWidth),Math.Max(p.Height,MinHeight),p.PhysicalLeft/monitor.Scale??p.Left??Left,p.PhysicalTop/monitor.Scale??p.Top??Top).Fit(work);
            Width=layout.Width;Height=layout.Height;WindowAppearance.Place(this,layout,monitor.Scale);Topmost=p.AlwaysOnTop;
            QuestionScroller.MaxHeight=p.Compact?36:52;
            CaptionPanel.Visibility=p.ShowCaptions?Visibility.Visible:Visibility.Collapsed;
            CaptionExpandControl.Content=p.ExpandCaptions?"⌃":"⌄";
            ApplySurface();
            WindowAppearance.ApplyOpacity(this,p.Opacity);
            GlassShell.ToolTip=p.Glass?"Translucent glass surface · Ctrl+Alt+[ / ] window opacity":"Solid surface · Ctrl+Alt+[ / ] window opacity";
            ApplyInteraction();
            if(IsLoaded)FitContent(this,p.Compact,tuning,p.CaptionHeight);
        }
        finally{applying=false;}
    }
    public void ApplyInteraction()=>WindowPrivacy.ReadOnly(this,!controller.Interactive);
    private void ApplySurface()
    {
        var p=controller.Overlay;WindowAppearance.Apply(this,p.Glass);
        Resources["BackgroundBrush"]=new SolidColorBrush(System.Windows.Media.Color.FromArgb(p.Glass?(byte)204:(byte)255,32,33,36));
        Resources["PanelBrush"]=new SolidColorBrush(System.Windows.Media.Color.FromArgb(p.Glass?(byte)220:(byte)255,32,33,36));
        Resources["FieldBrush"]=new SolidColorBrush(System.Windows.Media.Color.FromArgb(p.Glass?(byte)80:(byte)255,23,25,29));
        Resources["ButtonBrush"]=new SolidColorBrush(System.Windows.Media.Color.FromArgb(p.Glass?(byte)48:(byte)255,47,49,55));
    }
    internal static void FitContent(Window window,bool compact,bool tuning,double requestedCaptionHeight)
    {
        var answer=(FrameworkElement)window.FindName("AnswerContent");var captions=(LiveCaptionView)window.FindName("CaptionContent");
        var captionPanel=(Border)window.FindName("CaptionPanel");var tune=(ScrollViewer)((Border)window.FindName("TunePanel")).Child;
        var composer=(FrameworkElement)window.FindName("Composer");var state=window.DataContext as ProctalController;
        composer.Visibility=state?.CanAnswerImages==true?Visibility.Visible:Visibility.Collapsed;
        captions.SetCurrentValue(HeightProperty,state?.Overlay.ExpandCaptions==true?requestedCaptionHeight:30);tune.MaxHeight=Math.Max(100,Math.Min(220,window.ActualHeight-190));
        // Supplementary regions give space back before the answer becomes unreadable.
        for(var pass=0;pass<6;pass++)
        {
            window.UpdateLayout();var deficit=180-answer.ActualHeight;if(deficit<=0)break;
            if(captionPanel.Visibility==Visibility.Visible&&captions.Height>30)captions.SetCurrentValue(HeightProperty,Math.Max(30,captions.Height-deficit));
            else break;
        }
        window.UpdateLayout();captionPanel.ToolTip=captions.Height<requestedCaptionHeight?"Caption height fits the available window space. Enlarge the overlay to use the full saved height.":"Ctrl+Alt+Shift+PageUp / PageDown to read captions";
    }
    private void SaveGeometry(){if(!IsLoaded||applying)return;applying=true;try{var bounds=WindowAppearance.PhysicalBounds(this);controller.Overlay.Width=ActualWidth;controller.Overlay.Height=ActualHeight;controller.Overlay.Left=Left;controller.Overlay.Top=Top;controller.Overlay.PhysicalLeft=bounds.Left;controller.Overlay.PhysicalTop=bounds.Top;}finally{applying=false;}}
    public void ResizeBy(double width,double height){controller.Overlay.Width=Width+width;controller.Overlay.Height=Height+height;ApplyPreferences();SaveGeometry();}
    public void MoveBy(double x,double y){var bounds=WindowAppearance.PhysicalBounds(this);var scale=VisualTreeHelper.GetDpi(this).DpiScaleX;applying=true;try{controller.Overlay.PhysicalLeft=bounds.Left+x*scale;controller.Overlay.PhysicalTop=bounds.Top+y*scale;}finally{applying=false;}ApplyPreferences();SaveGeometry();}
    public void ResetPosition(){applying=true;try{var monitor=WindowAppearance.MonitorArea(this);var work=monitor.Work;controller.Overlay.Width=620;controller.Overlay.Height=620;controller.Overlay.PhysicalLeft=(work.Left+work.Width-Math.Min(620,work.Width)-20)*monitor.Scale;controller.Overlay.PhysicalTop=(work.Top+20)*monitor.Scale;}finally{applying=false;}ApplyPreferences();SaveGeometry();}
    public void SetPreset(string name){controller.Overlay.Compact=name=="Compact";controller.Overlay.Width=name=="Reading"?780:name=="Compact"?500:620;controller.Overlay.Height=name=="Reading"?860:name=="Compact"?480:620;ApplyPreferences();SaveGeometry();}
    private void DragHeader(object sender,MouseButtonEventArgs e){if(controller.Interactive&&e.LeftButton==MouseButtonState.Pressed&&e.OriginalSource is not Button)DragMove();}
    private void OpenSetup(object sender,RoutedEventArgs e)=>Host.ShowDashboard();
    private void OpenHistory(object sender,RoutedEventArgs e)=>Host.ShowHistory();
    private async void DeepAnswer(object sender,RoutedEventArgs e)=>await controller.Safe(controller.SubmitDeepAsync);
    private async void SolAnswer(object sender,RoutedEventArgs e)=>await controller.Safe(controller.SubmitSolAsync);
    private void HideOverlay(object sender,RoutedEventArgs e)=>Host.HideOverlay();
    internal void SetTune(bool value){TunePanel.Visibility=value?Visibility.Visible:Visibility.Collapsed;MoreControl.ToolTip=value?"Close More · Ctrl+Alt+V":"More · Ctrl+Alt+V";if(IsLoaded)FitContent(this,controller.Overlay.Compact,value,controller.Overlay.CaptionHeight);}
    private void ToggleTune(object sender,RoutedEventArgs e)=>SetTune(TunePanel.Visibility!=Visibility.Visible);
    private void ToggleMouse(object sender,RoutedEventArgs e)=>controller.Interactive=!controller.Interactive;
    private void CompactPreset(object sender,RoutedEventArgs e)=>SetPreset("Compact");
    private void StandardPreset(object sender,RoutedEventArgs e)=>SetPreset("Standard");
    private void ReadingPreset(object sender,RoutedEventArgs e)=>SetPreset("Reading");
    private async void ToggleListening(object sender,RoutedEventArgs e)=>await controller.Safe(controller.ToggleListeningAsync);
    private async void PracticeQuestion(object sender,RoutedEventArgs e)=>await controller.Safe(controller.NextPracticeQuestionAsync);
    private void ReviewAnswer(object sender,RoutedEventArgs e)=>Host.EditQuestion();
    private void ZoomOut(object sender,RoutedEventArgs e)=>controller.Overlay.FontSize--;
    private void ZoomIn(object sender,RoutedEventArgs e)=>controller.Overlay.FontSize++;
    private void ResetLayout(object sender,RoutedEventArgs e)=>ResetPosition();
    private void EditQuestion(object sender,RoutedEventArgs e)=>Host.EditQuestion();
    private async void CaptureImage(object sender,RoutedEventArgs e)=>await controller.Safe(controller.CaptureAsync);
    private async void AttachImage(object sender,RoutedEventArgs e)=>await Host.AttachImagesAsync(this);
    private async void GetAnswer(object sender,RoutedEventArgs e){if(controller.IsAnswering){controller.CancelAnswer();return;}await controller.Safe(controller.SubmitAsync);}
    private async void AnswerImages(object sender,RoutedEventArgs e)=>await controller.Safe(controller.SubmitImagesAsync);
    private void ClearImages(object sender,RoutedEventArgs e)=>controller.ClearImages();
    private void CancelAnswer(object sender,RoutedEventArgs e)=>controller.CancelAnswer();
    private void CopyCode(object sender,RoutedEventArgs e){if(!AnswerContent.CopyFirstCode())controller.ReportStatus("This answer has no code block to copy.");}
    private void ShowLatest(object sender,RoutedEventArgs e){controller.ShowLatestAnswer();controller.Overlay.AutoFollow=true;AnswerContent.ResumeFollowing();}
    private void PreviousAnswer(object sender,RoutedEventArgs e)=>controller.BrowseHistory(-1);
    private void NextAnswer(object sender,RoutedEventArgs e)=>controller.BrowseHistory(1);
    private void ExpandCaptions(object sender,RoutedEventArgs e)=>controller.Overlay.ExpandCaptions=!controller.Overlay.ExpandCaptions;
    private void UpdateReadingState()=>ReadingState.Text=controller.IsBrowsingHistory?"History":!AnswerContent.IsFollowing?"Reading":"";
    private async void Explain(object sender,RoutedEventArgs e)=>await controller.Safe(()=>controller.FollowUpAsync("explain"));
    private async void Edges(object sender,RoutedEventArgs e)=>await controller.Safe(()=>controller.FollowUpAsync("edges"));
    private async void Optimize(object sender,RoutedEventArgs e)=>await controller.Safe(()=>controller.FollowUpAsync("optimize"));
    private async void ExportAnswer(object sender,RoutedEventArgs e)=>await Host.ExportAnswerAsync();
    private void RemoveImage(object sender,RoutedEventArgs e){if(sender is Button{Tag:ScreenshotItem image})controller.Screenshots.Remove(image);}
}
