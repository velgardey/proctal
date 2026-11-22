using System.Windows;
using System.Windows.Controls;
using Proctal.Core;
using Proctal.Desktop;
using Xunit;
namespace Proctal.Tests;
public sealed class CaptionViewTests
{
    [Fact] public Task ReadingKeepsTheSameCaptionWhenOldTurnsRollOff()=>ControllerTests.OnDispatcher(controller=>
    {
        string Turns(int first)=>string.Join("\n\n",Enumerable.Range(first,14).Select(i=>$"Interviewer: turn {i:00} about constraints."));
        var view=new LiveCaptionView{Text=Turns(1).Replace("Interviewer: turn 06","Interviewer (live): turn 06")};var window=new Window{Content=view,Width=480,Height=180,ShowActivated=false};
        try
        {
            window.Show();view.FlushPendingRender();window.UpdateLayout();view.Scroll(ShortcutAction.CaptionTop);
            var editor=(TextBox)view.Content;var index=editor.Text.IndexOf("Interviewer (live): turn 06",StringComparison.Ordinal);var rect=editor.GetRectFromCharacterIndex(index);
            editor.ScrollToVerticalOffset(editor.VerticalOffset+rect.Top);window.UpdateLayout();
            Assert.Contains("turn 06",editor.GetLineText(editor.GetFirstVisibleLineIndex()));
            view.Text=Turns(2).Replace("Interviewer: turn 06","Interviewer (live): turn 06");view.FlushPendingRender();window.UpdateLayout();
            Assert.Contains("turn 06",editor.GetLineText(editor.GetFirstVisibleLineIndex()));Assert.False(view.IsFollowing);
            view.Text=Turns(2);view.FlushPendingRender();window.UpdateLayout();Assert.Contains("turn 06",editor.GetLineText(editor.GetFirstVisibleLineIndex()));
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });
    [Fact] public Task CaptionsCatchUpWhenTheOverlayBecomesVisible()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new LiveCaptionView{Text="Before hiding"};var window=new Window{Content=view,Width=400,Height=200,ShowActivated=false};
        try{window.Show();view.FlushPendingRender();window.Hide();view.Text="While hidden";window.Show();window.UpdateLayout();Assert.Equal("While hidden",((TextBox)view.Content).Text);}
        finally{window.Close();}
        return Task.CompletedTask;
    });
    [Fact] public Task LiveCaptionReadingPositionSurvivesStreamingUpdates()=>ControllerTests.OnDispatcher(controller=>
    {
        var type=typeof(MarkdownView).Assembly.GetType("Proctal.Desktop.LiveCaptionView");Assert.NotNull(type);
        var view=(UserControl)Activator.CreateInstance(type)!;var text=type.GetProperty("Text")!;var flush=type.GetMethod("FlushPendingRender")!;
        var content=string.Join("\n\n",Enumerable.Range(0,50).Select(i=>$"Interviewer: question {i} about algorithms and data structures."));text.SetValue(view,content);
        var window=new Window{Content=view,Width=460,Height=160,ShowActivated=false};
        try
        {
            window.Show();flush.Invoke(view,null);window.UpdateLayout();
            type.GetMethod("Scroll")!.Invoke(view,new object[]{Enum.Parse<ShortcutAction>("CaptionPageUp")});window.UpdateLayout();
            var editor=(TextBox)view.Content;var position=editor.VerticalOffset;Assert.True(position>0);Assert.False((bool)type.GetProperty("IsFollowing")!.GetValue(view)!);
            text.SetValue(view,content+"\n\nInterviewer (live): One more constraint");flush.Invoke(view,null);window.UpdateLayout();Assert.InRange(editor.VerticalOffset,position-3,position+3);
            type.GetMethod("Scroll")!.Invoke(view,new object[]{Enum.Parse<ShortcutAction>("CaptionEnd")});Assert.True((bool)type.GetProperty("IsFollowing")!.GetValue(view)!);
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });
    [Fact] public void CaptionAndDailyWorkflowShortcutsAreScopedToVisibleOverlay()
    {
        foreach(uint key in new uint[]{0x54,0x50,0x4E,0x52,0x08,0x1B})
        {
            Assert.NotEqual(ShortcutAction.None,new ShortcutPolicy().Accept(key,true,false,ctrl:true,alt:true));
            Assert.Equal(ShortcutAction.None,new ShortcutPolicy().Accept(key,true,false,ctrl:true,alt:true,overlayVisible:false));
        }
        Assert.NotEqual(ShortcutAction.None,new ShortcutPolicy().Accept(0x21,true,false,ctrl:true,alt:true,shift:true));
    }
}
