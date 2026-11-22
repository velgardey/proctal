using Proctal.Desktop;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;
namespace Proctal.Tests;
public sealed class MathRenderingTests
{
    [Theory]
    [InlineData(@"$\text{Probability} = \frac{1}{1+e^{-x}}$")]
    [InlineData(@"$\dfrac{1}{2} + \tfrac{3}{4}$")]
    [InlineData(@"$ 6.02 \times 10^{23} $")]
    [InlineData(@"$$\frac{a}{b} = 2.5 \times 10^{-3}$$")]
    [InlineData(@"$ /text{Probability} = /frac{1}{2} $")]
    [InlineData(@"\[ /frac{1}{2} \times 10^{-3} \]")]
    [InlineData(@"The ratio is \frac{n+1}{2}.")]
    [InlineData(@"The ratio is \frac{n+1}{2}x.")]
    [InlineData(@"A \frac{a}{b}2 value.")]
    public Task FractionAndScientificVariantsRenderEquations(string source)=>ControllerTests.OnDispatcher(_=>
    {
        var view=new MarkdownView{Text=source};view.FlushPendingRender();
        var doc=((FlowDocumentScrollViewer)view.Content).Document;
        var inline=doc.Blocks.OfType<Paragraph>().SelectMany(p=>p.Inlines.OfType<InlineUIContainer>()).Count(i=>i.Child is Image {Source:DrawingImage});
        var display=doc.Blocks.OfType<BlockUIContainer>().Count(b=>b.Child is ScrollViewer {Content:Image {Source:DrawingImage}});
        Assert.Equal(1,inline+display);return Task.CompletedTask;
    });
    [Fact] public void NormalizationPreservesProgrammingAndOrdinarySlashText()
    {
        const string source="Use /text and /frac paths. `$/frac{1}{2}$`\n\n```python\nexpr = r'$\\dfrac{1}{2}$'\npath = '/text/data'\n```";
        Assert.Equal(source,MathMarkdown.Normalize(source));
    }
    [Fact] public void FormulaRepairCorrectsSlashCommandsAndFractionAliasesOnly()
    {
        var method=typeof(MathMarkdown).GetMethod("NormalizeFormula");Assert.NotNull(method);
        Assert.Equal(@"\text{P} = \frac{1}{2} + \frac{3}{4}",method.Invoke(null,[@"/text{P} = /frac{1}{2} + \dfrac{3}{4}"]));
    }
    [Fact] public Task QuotedProgrammingFencesAndIndentedCodePreserveTexStrings()=>ControllerTests.OnDispatcher(_=>
    {
        foreach(var source in new[]{"> ~~~python\n> pattern = r\"\\(a\\)\"\n> ~~~",">     pattern = r\"\\[a\\]\""})
        {
            var view=new MarkdownView{Text=source};view.FlushPendingRender();Assert.Contains("\\",Assert.Single(view.CodeBlocks));Assert.Equal(source,MathMarkdown.Normalize(source));
        }
        return Task.CompletedTask;
    });
    [Fact] public Task ProseMathCannotPairWithADelimiterInsideProgrammingCode()=>ControllerTests.OnDispatcher(_=>
    {
        const string source="An unfinished delimiter: \\[\n\n~~~python\npattern = r\"\\]\"\n~~~";
        var view=new MarkdownView{Text=source};view.FlushPendingRender();Assert.Equal("pattern = r\"\\]\"",Assert.Single(view.CodeBlocks));Assert.Equal(source,MathMarkdown.Normalize(source));return Task.CompletedTask;
    });
    [Fact] public Task StreamedFenceWithTrailingWhitespaceDoesNotCrash()=>ControllerTests.OnDispatcher(_=>
    {
        var view=new MarkdownView{Text="```python\nprint(1)\n    "};view.FlushPendingRender();Assert.Contains("print(1)",Assert.Single(view.CodeBlocks));return Task.CompletedTask;
    });
    [Fact] public Task DollarAndTexDelimitersRenderNativeEquations()=>ControllerTests.OnDispatcher(_=>
    {
        var view=new MarkdownView{Text="Cost $n^2$ and \\(\\frac{a}{b}\\).\n\n\\[\\sum_{i=1}^{n} i = \\frac{n(n+1)}{2}\\]"};view.FlushPendingRender();
        var doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Equal(2,doc.Blocks.OfType<Paragraph>().SelectMany(p=>p.Inlines.OfType<InlineUIContainer>()).Count(i=>i.Child is Image {Source:DrawingImage}));
        Assert.Contains(doc.Blocks.OfType<BlockUIContainer>(),b=>b.Child is ScrollViewer {Content:Image {Source:DrawingImage}});
        Assert.Empty(view.CodeBlocks);return Task.CompletedTask;
    });
    [Fact] public Task MathFencesRenderMatricesAndCodeKeepsEveryBackslash()=>ControllerTests.OnDispatcher(_=>
    {
        const string code="path = r\"C:\\new\\test\"\npattern = r\"\\(a\\)\"";
        var view=new MarkdownView{Text="~~~python\n"+code+"\n~~~\n\n```latex\n\\begin{pmatrix}a & b \\\\ c & d\\end{pmatrix}\n```"};view.FlushPendingRender();
        Assert.Equal(code,Assert.Single(view.CodeBlocks));var doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Contains(doc.Blocks.OfType<BlockUIContainer>(),b=>b.Child is ScrollViewer {Content:Image {Source:DrawingImage}});return Task.CompletedTask;
    });
    [Fact] public Task IncompleteAndUnsupportedMathRemainReadableAndCanFinishStreaming()=>ControllerTests.OnDispatcher(_=>
    {
        var view=new MarkdownView{Text="$$\n\\frac{n}{"};view.FlushPendingRender();var doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Contains("\\frac{n}{",new TextRange(doc.ContentStart,doc.ContentEnd).Text);
        view.Text="$$\n\\frac{n}{2}\n$$\n\n$\\unsupportedcommand{x}$";view.FlushPendingRender();doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Contains(doc.Blocks.OfType<BlockUIContainer>(),b=>b.Child is ScrollViewer {Content:Image {Source:DrawingImage}});
        Assert.Contains("\\unsupportedcommand{x}",new TextRange(doc.ContentStart,doc.ContentEnd).Text);return Task.CompletedTask;
    });
    [Fact] public Task InlineCodeAndIndentedCodeDoNotBecomeEquations()=>ControllerTests.OnDispatcher(_=>
    {
        const string code="\\[x^2\\]";var view=new MarkdownView{Text="Use `\\(x\\)` and `$n$`.\n\n    "+code};view.FlushPendingRender();
        Assert.Equal(code,Assert.Single(view.CodeBlocks));var doc=((FlowDocumentScrollViewer)view.Content).Document;
        Assert.Empty(doc.Blocks.OfType<Paragraph>().SelectMany(p=>p.Inlines.OfType<InlineUIContainer>()));
        Assert.Contains("\\(x\\)",new TextRange(doc.ContentStart,doc.ContentEnd).Text);return Task.CompletedTask;
    });
}
