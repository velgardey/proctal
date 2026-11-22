using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using Proctal.Desktop.Platform;
using Xunit;

namespace Proctal.Tests;

[CollectionDefinition("Native hover privacy",DisableParallelization=true)]
public sealed class NativeHoverPrivacyCollection;

[Collection("Native hover privacy")]
public sealed class HoverPopupTests
{
    private static ToolTipEventArgs Opening()=>(ToolTipEventArgs)Activator.CreateInstance(typeof(ToolTipEventArgs),BindingFlags.Instance|BindingFlags.NonPublic,null,[true],null)!;

    [Fact]
    public Task CaptureExcludedWindowPreventsButtonAndInlineHoverPopups()=>ControllerTests.OnDispatcher(c=>
    {
        var button=new Button{ToolTip="Shortcut help"};
        var inline=new Run("code"){ToolTip="Inline help"};
        var text=new TextBlock(inline);
        var panel=new StackPanel();panel.Children.Add(button);panel.Children.Add(text);
        var window=new Window{Content=panel};
        try
        {
            WindowPrivacy.Apply(window,true);
            var buttonOpening=Opening();button.RaiseEvent(buttonOpening);
            var inlineOpening=Opening();inline.RaiseEvent(inlineOpening);
            Assert.True(buttonOpening.Handled,"A hover tooltip must not create an unprotected popup window.");
            Assert.True(inlineOpening.Handled,"Inline Markdown help must follow the same capture-exclusion policy.");
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });

    [Fact]
    public Task NormalWindowKeepsHoverHelpWhenExclusionIsDisabled()=>ControllerTests.OnDispatcher(c=>
    {
        var button=new Button{ToolTip="Shortcut help"};var window=new Window{Content=button};
        try
        {
            WindowPrivacy.Apply(window,true);
            WindowPrivacy.Apply(window,false);
            var opening=Opening();button.RaiseEvent(opening);
            Assert.False(opening.Handled);
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });

    [Fact]
    public Task EnablingExclusionClosesAnAlreadyOpenFloatingTooltip()=>ControllerTests.OnDispatcher(c=>
    {
        var button=new Button{Content="Test help",ToolTip="Shortcut help"};
        var window=new Window{Content=button,ShowActivated=false,Width=200,Height=100};
        var tip=new ToolTip{PlacementTarget=button,Content=button.ToolTip};
        try
        {
            WindowPrivacy.Apply(window,false);window.Show();tip.IsOpen=true;
            Assert.True(tip.IsOpen);
            var source=PresentationSource.FromVisual(tip) as HwndSource;
            Assert.NotNull(source);
            Assert.NotEqual(new WindowInteropHelper(window).Handle,source.Handle);
            WindowPrivacy.Apply(window,true);
            Assert.False(tip.IsOpen);
            var opening=Opening();button.RaiseEvent(opening);
            Assert.True(opening.Handled);
        }
        finally{tip.IsOpen=false;window.Close();}
        return Task.CompletedTask;
    });
}
