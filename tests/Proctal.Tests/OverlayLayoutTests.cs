using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Text.RegularExpressions;
using Proctal.Desktop;
using Xunit;
namespace Proctal.Tests;
public sealed class OverlayLayoutTests
{
    private static Window LoadOverlay(ProctalController controller)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,".proctal-project")))root=root.Parent;
        Assert.NotNull(root);var markup=File.ReadAllText(Path.Combine(root.FullName,"src","Proctal.Desktop","OverlayWindow.xaml"));
        markup=Regex.Replace(markup,@"\s+(x:Class|Click|MouseLeftButtonDown)=""[^""]+""","").Replace("clr-namespace:Proctal.Desktop\"","clr-namespace:Proctal.Desktop;assembly=Proctal\"");
        markup=markup.Replace("<Window.Resources>","<Window.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries><ResourceDictionary Source=\"pack://application:,,,/Proctal;component/UiTheme.xaml\"/></ResourceDictionary.MergedDictionaries>").Replace("</Window.Resources>","</ResourceDictionary></Window.Resources>");
        var window=(Window)XamlReader.Parse(markup);window.MinHeight=1;window.MinWidth=1;window.Width=480;window.Height=520;window.ShowActivated=false;window.DataContext=controller;return window;
    }
    [Fact] public Task DefaultOverlayKeepsAReadableAnswerOnSmallWindows()=>ControllerTests.OnDispatcher(controller=>
    {
        var window=LoadOverlay(controller);
        try{window.Show();typeof(OverlayWindow).GetMethod("FitContent",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{window,false,false,84d});window.UpdateLayout();Assert.True(((FrameworkElement)window.FindName("AnswerContent")).ActualHeight>=260,"The default 480x520 overlay must leave at least 260px for reading answers.");}
        finally{window.Close();}return Task.CompletedTask;
    });
    [Fact] public Task DefaultToolbarHasOneRowAndAdvancedControlsStayTuckedAway()=>ControllerTests.OnDispatcher(controller=>
    {
        var window=LoadOverlay(controller);
        try
        {
            window.Show();window.UpdateLayout();var toolbar=window.FindName("PrimaryToolbar") as FrameworkElement;Assert.NotNull(toolbar);Assert.True(toolbar.ActualHeight<=48);
            Assert.True(((FrameworkElement)window.FindName("TextAnswerControl")).IsVisible);Assert.True(((FrameworkElement)window.FindName("ListenControl")).IsVisible);
            Assert.False(((FrameworkElement)window.FindName("TunePanel")).IsVisible);Assert.False(((FrameworkElement)window.FindName("AutoAnswerControl")).IsVisible);Assert.False(((FrameworkElement)window.FindName("MicrophoneControl")).IsVisible);Assert.False(((FrameworkElement)window.FindName("FollowupActions")).IsVisible);
        }
        finally{window.Close();}return Task.CompletedTask;
    });
    [Fact] public Task PrimaryActionHasReadableDarkTextOnItsLightSurface()=>ControllerTests.OnDispatcher(controller=>
    {
        var window=LoadOverlay(controller);controller.ManualText="Explain a transaction";
        try
        {
            window.Show();window.UpdateLayout();var button=(Button)window.FindName("TextAnswerControl");Assert.True(button.IsEnabled);
            var text=Descendants(button).OfType<TextBlock>().First(t=>t.Text.Contains("Answer"));var brush=Assert.IsType<System.Windows.Media.SolidColorBrush>(text.Foreground);Assert.True(brush.Color.R<100&&brush.Color.G<100&&brush.Color.B<100,"Primary action text must contrast with its light background.");
        }
        finally{window.Close();}return Task.CompletedTask;
    });
    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(node,i);yield return child;foreach(var descendant in Descendants(child))yield return descendant;}
    }
    [Fact] public Task FiftyThumbnailsKeepTheAnswerReadable()=>ControllerTests.OnDispatcher(controller=>
    {
        var image=Proctal.Desktop.Platform.ScreenshotService.FromImage(System.Windows.Media.Imaging.BitmapSource.Create(2,2,96,96,System.Windows.Media.PixelFormats.Bgra32,null,new byte[16],8),"test.png");
        for(var i=0;i<50;i++)controller.AddImage(image);
        var window=LoadOverlay(controller);
        try
        {
            window.Show();typeof(OverlayWindow).GetMethod("FitContent",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{window,false,false,84d});window.UpdateLayout();
            Assert.True(((FrameworkElement)window.FindName("AnswerContent")).ActualHeight>=180,"Fifty thumbnails must leave an answer viewport.");
            var previews=Descendants((DependencyObject)window.FindName("Composer")).OfType<ScrollViewer>().Single();
            Assert.True(previews.ScrollableHeight>0);Assert.False(previews.Focusable);
        }
        finally{window.Close();}return Task.CompletedTask;
    });
    [Fact] public Task HoveredPrimaryActionKeepsAReadableLightSurface()=>ControllerTests.OnDispatcher(controller=>
    {
        var window=LoadOverlay(controller);controller.ManualText="Explain a transaction";
        try
        {
            window.Show();window.UpdateLayout();var button=(Button)window.FindName("TextAnswerControl");
            var key=typeof(UIElement).GetField("IsMouseOverPropertyKey",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null) as DependencyPropertyKey;Assert.NotNull(key);button.SetValue(key,true);window.UpdateLayout();
            var frame=Assert.IsType<Border>(button.Template.FindName("Frame",button));var background=Assert.IsType<System.Windows.Media.SolidColorBrush>(frame.Background);Assert.True(background.Color.R>180&&background.Color.G>180&&background.Color.B>180,"A hovered primary action must retain contrast with its dark text.");
        }
        finally{window.Close();}return Task.CompletedTask;
    });
    [Fact] public Task ShortScreensKeepAnAnswerViewportWithMaximumCaptionsAndTune()=>ControllerTests.OnDispatcher(controller=>
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root is not null&&!File.Exists(Path.Combine(root.FullName,".proctal-project")))root=root.Parent;
        Assert.NotNull(root);var markup=File.ReadAllText(Path.Combine(root.FullName,"src","Proctal.Desktop","OverlayWindow.xaml"));
        markup=Regex.Replace(markup,@"\s+(x:Class|Click|MouseLeftButtonDown)=""[^""]+""","").Replace("clr-namespace:Proctal.Desktop\"","clr-namespace:Proctal.Desktop;assembly=Proctal\"");
        markup=markup.Replace("<Window.Resources>","<Window.Resources><ResourceDictionary><ResourceDictionary.MergedDictionaries><ResourceDictionary Source=\"pack://application:,,,/Proctal;component/UiTheme.xaml\"/></ResourceDictionary.MergedDictionaries>").Replace("</Window.Resources>","</ResourceDictionary></Window.Resources>");
        var window=(Window)XamlReader.Parse(markup);window.MinHeight=1;window.MinWidth=1;window.Width=480;window.ShowActivated=false;window.DataContext=controller;
        controller.Overlay.CaptionHeight=180;controller.Overlay.ExpandCaptions=true;((FrameworkElement)window.FindName("TunePanel")).Visibility=Visibility.Visible;((FrameworkElement)window.FindName("Composer")).Visibility=Visibility.Collapsed;((FrameworkElement)window.FindName("FollowupActions")).Visibility=Visibility.Collapsed;
        var answer=(FrameworkElement)window.FindName("AnswerContent");
        try
        {
            window.Show();
            foreach(var height in new[]{600d,650d,700d,800d})
            {
                window.Height=height;window.UpdateLayout();
                typeof(OverlayWindow).GetMethod("FitContent",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,new object[]{window,false,true,180d});window.UpdateLayout();
                Assert.True(answer.ActualHeight>=140,$"At height {height}, answer viewport was {answer.ActualHeight}.");
            }
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });
}
