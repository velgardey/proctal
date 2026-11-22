using Proctal.Core;
using Proctal.Desktop;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;
using Xunit;
namespace Proctal.Tests;
public sealed class MarkdownTests
{
    [Fact] public Task LongAnswersKeepTheirFinalExplanationAndCode()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new MarkdownView{Text=new string('x',120001)+"\n\n## Final explanation\n\nThis example completes the answer.\n\n```python\nreturn 42\n```"};
        view.FlushPendingRender();
        Assert.Equal("return 42",Assert.Single(view.CodeBlocks));
        var document=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Contains("This example completes the answer.",new TextRange(document.ContentStart,document.ContentEnd).Text);
        return Task.CompletedTask;
    });
    [Fact] public Task ReopeningShowsTheLatestPartialAnswer()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new MarkdownView{Text="Before hiding"};var window=new Window{Content=view,Width=460,Height=240,ShowActivated=false};
        try{window.Show();view.FlushPendingRender();window.Hide();view.Text="```python\nreturn 42\n```";window.Show();window.UpdateLayout();Assert.Equal("return 42",Assert.Single(view.CodeBlocks));}
        finally{window.Close();}
        return Task.CompletedTask;
    });
    [Fact] public Task WrappedTabsAndWideCharactersRemainReachable()=>ControllerTests.OnDispatcher(controller=>
    {
        foreach(var code in new[]{string.Join("\n",Enumerable.Repeat(new string('\t',15)+"return 42",8)),new string('界',200)})
        {
            var view=new MarkdownView{CodeWrap=true,Text="```python\n"+code+"\n```"};
            var window=new Window{Content=view,Width=440,Height=360,ShowActivated=false};
            try
            {
                window.Show();view.FlushPendingRender();window.UpdateLayout();
                var container=((FlowDocumentScrollViewer)view.Content).Document.Blocks.OfType<BlockUIContainer>().Single();
                var panel=(StackPanel)((Border)container.Child).Child;
                var editor=panel.Children.OfType<RichTextBox>().Single();editor.UpdateLayout();
                Assert.True(editor.ExtentHeight<=editor.ViewportHeight+2,$"Code clipped: extent {editor.ExtentHeight}, viewport {editor.ViewportHeight}");
            }
            finally{window.Close();}
        }
        return Task.CompletedTask;
    });
    [Fact] public Task ReadingPositionSurvivesStreamingAppend()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new MarkdownView{AutoFollow=true,Text=string.Join("\n\n",Enumerable.Range(0,60).Select(i=>$"Paragraph {i}: A sufficiently long explanation of the approach, constraints and tradeoffs."))};
        var window=new Window{Content=view,Width=560,Height=360,ShowActivated=false};
        try
        {
            window.Show();view.FlushPendingRender();window.UpdateLayout();view.Scroll(ShortcutAction.PageDown);window.UpdateLayout();
            var scroll=Scroll(view);Assert.NotNull(scroll);Assert.True(scroll.VerticalOffset>0);var offset=scroll.VerticalOffset;
            view.Text+="\n\nAnother final paragraph.";view.FlushPendingRender();window.UpdateLayout();
            Assert.False(view.IsFollowing);Assert.InRange(scroll.VerticalOffset,offset-3,offset+3);
        }
        finally{window.Close();}
        return Task.CompletedTask;
    });
    private static ScrollViewer? Scroll(DependencyObject parent){if(parent is ScrollViewer s)return s;for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var found=Scroll(VisualTreeHelper.GetChild(parent,i));if(found is not null)return found;}return null;}
    [Fact] public void SyntaxColorsPreserveCodeAndDoNotMistakeStringsForComments()
    {
        const string code="const url = \"https://example.com\"; // comment\nreturn 42;";
        var tokens=CodeColorizer.Tokenize(code,"typescript");
        Assert.Equal(code,string.Concat(tokens.Select(t=>t.Text)));
        Assert.Contains(tokens,t=>t.Kind==CodeTokenKind.String&&t.Text.Contains("https://"));
        Assert.Single(tokens,t=>t.Kind==CodeTokenKind.Comment);
    }
    [Fact] public Task NativeMarkdownShowsTablesAndUnclosedStreamedCode()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new MarkdownView{Width=600,Text="# Solution\n\n**Fast** with `lookup`.\n\n| Cost | Value |\n| --- | --- |\n| Time | O(n) |\n\n```python\ndef solve(x):\n    return x + 1"};
        view.FlushPendingRender();var doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Contains(doc.Blocks.Cast<Block>(),b=>b is Table);
        Assert.Equal("def solve(x):\n    return x + 1",view.CodeBlocks.Single());
        Assert.Contains(doc.Blocks.Cast<Block>(),b=>b is BlockUIContainer);
        return Task.CompletedTask;
    });
    [Fact] public Task HtmlAndRemoteImagesDoNotBecomeExecutableControls()=>ControllerTests.OnDispatcher(controller=>
    {
        var view=new MarkdownView{Text="<script>alert('x')</script>\n\n![diagram](https://example.com/private.png)"};view.FlushPendingRender();
        Assert.Empty(view.CodeBlocks);Assert.DoesNotContain(((FlowDocumentScrollViewer)view.Content).Document.Blocks.Cast<Block>(),b=>b is BlockUIContainer);
        return Task.CompletedTask;
    });
}
