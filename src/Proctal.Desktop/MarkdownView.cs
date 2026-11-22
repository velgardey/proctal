using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Mathematics;
using WpfMath.Parsers;
using WpfMath.Rendering;
using XamlMath;
using Proctal.Core;
using MdBlock=Markdig.Syntax.Block;
using MdTable=Markdig.Extensions.Tables.Table;
using MdRow=Markdig.Extensions.Tables.TableRow;
using MdCell=Markdig.Extensions.Tables.TableCell;
using NativeBlock=System.Windows.Documents.Block;
using NativeInline=System.Windows.Documents.Inline;
namespace Proctal.Desktop;
public sealed class MarkdownView:UserControl
{
    private static readonly MarkdownPipeline Pipeline=new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().UseMathematics().DisableHtml().Build();
    private static readonly object MathParserLock=new();
    private readonly Dictionary<(string Source,double Size,bool Dark,bool Display),DrawingImage?> mathCache=[];
    public static readonly DependencyProperty TextProperty=DependencyProperty.Register(nameof(Text),typeof(string),typeof(MarkdownView),new PropertyMetadata("",Changed));
    public static readonly DependencyProperty CodeWrapProperty=DependencyProperty.Register(nameof(CodeWrap),typeof(bool),typeof(MarkdownView),new PropertyMetadata(false,Changed));
    public static readonly DependencyProperty AutoFollowProperty=DependencyProperty.Register(nameof(AutoFollow),typeof(bool),typeof(MarkdownView),new PropertyMetadata(true,Changed));
    public static readonly DependencyProperty IsDarkProperty=DependencyProperty.Register(nameof(IsDark),typeof(bool),typeof(MarkdownView),new PropertyMetadata(true,Changed));
    private readonly FlowDocumentScrollViewer viewer=new(){Focusable=false,IsSelectionEnabled=true,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Background=Brushes.Transparent};
    private readonly DispatcherTimer renderTimer;
    private readonly List<string> codeBlocks=[];
    private string rendered="";
    private bool following=true,rendering;
    private int mathRenderCount;
    private ScrollViewer? watchedScroll;
    public string Text{get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value??"");}
    public bool CodeWrap{get=>(bool)GetValue(CodeWrapProperty);set=>SetValue(CodeWrapProperty,value);}
    public bool AutoFollow{get=>(bool)GetValue(AutoFollowProperty);set=>SetValue(AutoFollowProperty,value);}
    public bool IsDark{get=>(bool)GetValue(IsDarkProperty);set=>SetValue(IsDarkProperty,value);}
    public bool IsFollowing=>following&&AutoFollow;
    public IReadOnlyList<string> CodeBlocks=>codeBlocks;
    public event Action? ReadingPositionChanged;
    public event Action? CodeCopied;
    public MarkdownView()
    {
        Content=viewer;Focusable=false;FontFamily=new("pack://application:,,,/Proctal;component/Assets/Fonts/#IBM Plex Sans");FontSize=16;
        renderTimer=new DispatcherTimer(TimeSpan.FromMilliseconds(100),DispatcherPriority.Background,(_,_)=>FlushPendingRender(),Dispatcher);renderTimer.Stop();
        Loaded+=(_,_)=>FlushPendingRender();Unloaded+=(_,_)=>renderTimer.Stop();SizeChanged+=(_,_)=>QueueRender();
        IsVisibleChanged+=(_,_)=>{if(IsVisible)FlushPendingRender();else renderTimer.Stop();};
        PreviewMouseWheel+=(_,_)=>{following=false;ReadingPositionChanged?.Invoke();};
    }
    private static void Changed(DependencyObject d,DependencyPropertyChangedEventArgs e){var view=(MarkdownView)d;if(e.Property==AutoFollowProperty)view.following=(bool)e.NewValue;view.QueueRender();}
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e){base.OnPropertyChanged(e);if(e.Property==FontSizeProperty&&renderTimer is not null)QueueRender();}
    private void QueueRender(){if(IsVisible&&!renderTimer.IsEnabled)renderTimer.Start();}
    private Brush Color(string dark,string light)=>new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(IsDark?dark:light));
    public void FlushPendingRender()
    {
        renderTimer.Stop();rendering=true;var scroll=FindScroll(viewer);var offset=scroll?.VerticalOffset??0;var append=Text.StartsWith(rendered,StringComparison.Ordinal)&&rendered.Length>0;
        var document=new FlowDocument{FontFamily=FontFamily,FontSize=FontSize,Foreground=Color("#ECEFF3","#252A34"),Background=Brushes.Transparent,PagePadding=new Thickness(2,0,12,0),ColumnWidth=999999};
        codeBlocks.Clear();mathRenderCount=0;foreach(var block in Markdown.Parse(MathMarkdown.Normalize(Text),Pipeline)){var native=RenderBlock(block);if(native is not null)document.Blocks.Add(native);}
        viewer.Document=document;rendered=Text;viewer.UpdateLayout();
        var target=FindScroll(viewer);if(target is not null){if(watchedScroll!=target){if(watchedScroll is not null)watchedScroll.ScrollChanged-=ScrollChanged;watchedScroll=target;watchedScroll.ScrollChanged+=ScrollChanged;}if(append&&IsFollowing)target.ScrollToEnd();else target.ScrollToVerticalOffset(append?offset:0);}
        rendering=false;
        ReadingPositionChanged?.Invoke();
    }
    private void ScrollChanged(object sender,ScrollChangedEventArgs e){if(!rendering&&e.ExtentHeightChange==0&&e.VerticalChange<0){following=false;ReadingPositionChanged?.Invoke();}}
    public void Scroll(ShortcutAction action)
    {
        var scroll=FindScroll(viewer);if(scroll is null)return;following=action==ShortcutAction.ScrollEnd;var page=Math.Max(80,scroll.ViewportHeight-40);
        switch(action){case ShortcutAction.ScrollUp:scroll.ScrollToVerticalOffset(scroll.VerticalOffset-48);break;case ShortcutAction.ScrollDown:scroll.ScrollToVerticalOffset(scroll.VerticalOffset+48);break;case ShortcutAction.PageUp:scroll.ScrollToVerticalOffset(scroll.VerticalOffset-page);break;case ShortcutAction.PageDown:scroll.ScrollToVerticalOffset(scroll.VerticalOffset+page);break;case ShortcutAction.ScrollTop:scroll.ScrollToTop();break;case ShortcutAction.ScrollEnd:scroll.ScrollToEnd();break;}
        ReadingPositionChanged?.Invoke();
    }
    public void ResumeFollowing(){following=true;FindScroll(viewer)?.ScrollToEnd();ReadingPositionChanged?.Invoke();}
    public bool CopyFirstCode(){FlushPendingRender();if(codeBlocks.Count==0)return false;Clipboard.SetText(codeBlocks[0]);CodeCopied?.Invoke();return true;}
    private static ScrollViewer? FindScroll(DependencyObject parent){if(parent is ScrollViewer s)return s;for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var result=FindScroll(VisualTreeHelper.GetChild(parent,i));if(result is not null)return result;}return null;}
    private Paragraph Paragraph(ContainerInline? content){var p=new Paragraph{Margin=new Thickness(0,0,0,12),LineHeight=FontSize*1.55};AddInlines(p.Inlines,content);return p;}
    private NativeBlock? RenderBlock(MdBlock block)
    {
        if(block is HeadingBlock heading){var p=Paragraph(heading.Inline);p.FontSize=FontSize+(heading.Level==1?8:heading.Level==2?4:1);p.FontWeight=FontWeights.SemiBold;p.Margin=new Thickness(0,10,0,10);return p;}
        if(block is ParagraphBlock paragraph)return Paragraph(paragraph.Inline);
        if(block is MathBlock math)return MathPanel(math.Lines.ToString().Trim());
        if(block is FencedCodeBlock formula&&formula.Info?.Trim().ToLowerInvariant() is "math" or "latex" or "tex")return MathPanel(formula.Lines.ToString().Trim());
        if(block is CodeBlock code)return CodePanel(code);
        if(block is MdTable table)
        {
            var native=new System.Windows.Documents.Table{CellSpacing=0,Margin=new Thickness(0,6,0,16),FontSize=FontSize-1};var group=new TableRowGroup();native.RowGroups.Add(group);
            foreach(var row in table.OfType<MdRow>()){var r=new System.Windows.Documents.TableRow();group.Rows.Add(r);foreach(var cell in row.OfType<MdCell>()){var c=new System.Windows.Documents.TableCell{Padding=new Thickness(10,8,10,8),BorderBrush=Color("#536176","#C9D0DB"),BorderThickness=new Thickness(0,0,0,1),Background=row.IsHeader?Color("#303237","#EAF0F7"):Brushes.Transparent,FontWeight=row.IsHeader?FontWeights.SemiBold:FontWeights.Normal};foreach(var child in cell){var b=RenderBlock(child);if(b is not null)c.Blocks.Add(b);}r.Cells.Add(c);}}
            return native;
        }
        if(block is ListBlock list){var native=new System.Windows.Documents.List{MarkerStyle=list.IsOrdered?TextMarkerStyle.Decimal:TextMarkerStyle.Disc,Margin=new Thickness(0,0,0,12),Padding=new Thickness(22,0,0,0)};foreach(var item in list.OfType<ListItemBlock>()){var n=new System.Windows.Documents.ListItem();foreach(var child in item){var b=RenderBlock(child);if(b is not null)n.Blocks.Add(b);}native.ListItems.Add(n);}return native;}
        if(block is QuoteBlock quote){var section=new Section{BorderBrush=Color("#9CB8D8","#6383A4"),BorderThickness=new Thickness(3,0,0,0),Padding=new Thickness(14,8,8,0),Margin=new Thickness(0,4,0,14)};foreach(var child in quote){var b=RenderBlock(child);if(b is not null)section.Blocks.Add(b);}return section;}
        if(block is ThematicBreakBlock)return new Paragraph{BorderThickness=new Thickness(0,0,0,1),BorderBrush=Color("#4B5C70","#C9D0DB"),Margin=new Thickness(0,8,0,16)};
        return null;
    }
    private NativeBlock CodePanel(CodeBlock block)
    {
        var text=block.Lines.ToString().TrimEnd('\r','\n');var language=block is FencedCodeBlock fence?fence.Info?.Split(' ',StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"code":"code";codeBlocks.Add(text);
        var mono=new FontFamily("pack://application:,,,/Proctal;component/Assets/Fonts/#IBM Plex Mono");var p=new Paragraph{Margin=new Thickness(0),LineHeight=FontSize*1.5};
        foreach(var token in CodeColorizer.Tokenize(text,language))p.Inlines.Add(new Run(token.Text){Foreground=token.Kind switch{CodeTokenKind.Keyword=>Color("#C7A5F4","#6743A0"),CodeTokenKind.String=>Color("#A6D9B1","#246842"),CodeTokenKind.Comment=>Color("#91A0B4","#5F6C7D"),CodeTokenKind.Number=>Color("#F2CCA0","#8B5823"),_=>Color("#E5EBF4","#263447")}});
        var width=Math.Max(260,(ActualWidth>0?ActualWidth:Width>0&&!double.IsNaN(Width)?Width:560)-36);var lines=text.Split('\n');var doc=new FlowDocument(p){FontFamily=mono,FontSize=Math.Max(12,FontSize-1),PagePadding=new Thickness(12),ColumnWidth=999999,PageWidth=CodeWrap?double.NaN:Math.Max(width,lines.Max(l=>l.Length)*FontSize*.66+32)};
        // Let WPF measure the document with the actual available width, font and tab stops.
        var code=new RichTextBox{Document=doc,IsReadOnly=true,IsUndoEnabled=false,Focusable=false,IsTabStop=false,Background=Brushes.Transparent,BorderThickness=new Thickness(0),MinHeight=50,HorizontalScrollBarVisibility=CodeWrap?ScrollBarVisibility.Disabled:ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};
        System.Windows.Automation.AutomationProperties.SetName(code,language+" code");
        var header=new Grid{Margin=new Thickness(12,8,6,0)};header.Children.Add(new TextBlock{Text=language,Foreground=Color("#A9B9CE","#64778E"),FontFamily=mono,FontSize=11,VerticalAlignment=VerticalAlignment.Center});
        var copy=new Button{Content="Copy code",Focusable=false,IsTabStop=false,FontSize=11,Padding=new Thickness(10,5,10,5),HorizontalAlignment=HorizontalAlignment.Right};copy.Click+=(_,_)=>{Clipboard.SetText(text);CodeCopied?.Invoke();};header.Children.Add(copy);
        var panel=new StackPanel();panel.Children.Add(header);panel.Children.Add(code);
        return new BlockUIContainer(new Border{Child=panel,Background=Color("#80202125","#F5F7FA"),BorderBrush=Color("#38FFFFFF","#CDD6E2"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(9),Margin=new Thickness(0,4,0,14)}){Margin=new Thickness(0)};
    }
    private void AddInlines(InlineCollection target,ContainerInline? container)
    {
        if(container is null)return;for(var item=container.FirstChild;item is not null;item=item.NextSibling)
        {
            if(item is MathInline math){var source=math.Content.ToString();var image=MathImage(source,false);target.Add(image is null?new Run(source){FontFamily=new FontFamily("Consolas")}:new InlineUIContainer(image){BaselineAlignment=BaselineAlignment.Center});}
            else if(item is LiteralInline literal)target.Add(new Run(literal.Content.ToString()));
            else if(item is CodeInline code)target.Add(new Run(code.Content){FontFamily=new FontFamily("pack://application:,,,/Proctal;component/Assets/Fonts/#IBM Plex Mono"),FontSize=FontSize-1,Background=Color("#503F4249","#E9EFF7"),Foreground=Color("#DBE9FB","#234263")});
            else if(item is EmphasisInline emphasis){var span=new Span();if(emphasis.DelimiterChar=='~')span.TextDecorations=TextDecorations.Strikethrough;else if(emphasis.DelimiterCount>=2)span.FontWeight=FontWeights.SemiBold;else span.FontStyle=FontStyles.Italic;AddInlines(span.Inlines,emphasis);target.Add(span);}
            else if(item is LineBreakInline br)target.Add(br.IsHard?new LineBreak():new Run(" "));
            else if(item is LinkInline link){if(link.IsImage){var alt=new Span();AddInlines(alt.Inlines,link);target.Add(alt);continue;}var span=new Hyperlink{Foreground=Color("#9BC8F7","#235F9A")};AddInlines(span.Inlines,link);if(Uri.TryCreate(link.Url,UriKind.Absolute,out var uri)&&uri.Scheme is "https" or "http"&&uri.UserInfo.Length==0){span.NavigateUri=uri;span.RequestNavigate+=OpenLink;}target.Add(span);}
            else if(item is AutolinkInline auto)target.Add(new Run(auto.Url));
            else if(item is HtmlEntityInline entity)target.Add(new Run(entity.Transcoded.ToString()));
            else if(item is ContainerInline nested){var span=new Span();AddInlines(span.Inlines,nested);target.Add(span);}
        }
    }
    private NativeBlock MathPanel(string source)
    {
        var image=MathImage(source,true);
        if(image is null)return new Paragraph(new Run(source)){FontFamily=new FontFamily("Consolas"),Foreground=Color("#BEC9D8","#405065"),Margin=new Thickness(0,6,0,14)};
        return new BlockUIContainer(new ScrollViewer{Content=image,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,Focusable=false,IsTabStop=false,Background=Brushes.Transparent,Margin=new Thickness(0,8,0,16)}){Margin=new Thickness(0)};
    }
    private Image? MathImage(string source,bool display)
    {
        if(++mathRenderCount>128)return null;
        var key=(source,FontSize,IsDark,display);
        if(!mathCache.TryGetValue(key,out var drawing))
        {
            drawing=null;
            if(SafeMath(source))
            {
                try
                {
                    lock(MathParserLock)
                    {
                        var size=FontSize*(display?1.16:1);var formula=WpfTeXFormulaParser.Instance.Parse(MathMarkdown.NormalizeFormula(source));
                        var environment=WpfTeXEnvironment.Create(display?TexStyle.Display:TexStyle.Text,size,"Arial",Color("#ECEFF3","#252A34"));
                        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())formula.RenderTo(dc,environment,size);
                        var bounds=visual.ContentBounds;
                        if(!bounds.IsEmpty&&bounds.Width is >0 and <=12000&&bounds.Height is >0 and <=2000)
                        {
                            var group=new DrawingGroup{Transform=new TranslateTransform(-bounds.Left+2,-bounds.Top+2)};group.Children.Add(visual.Drawing);
                            group.Freeze();drawing=new DrawingImage(group);drawing.Freeze();
                        }
                    }
                }
                catch(Exception error) when(error is not OutOfMemoryException){drawing=null;}
            }
            if(mathCache.Count>=64)mathCache.Clear();mathCache[key]=drawing;
        }
        if(drawing is null)return null;
        var image=new Image{Source=drawing,Width=drawing.Width,Height=drawing.Height,Stretch=Stretch.None,HorizontalAlignment=HorizontalAlignment.Left,Focusable=false,Margin=new Thickness(2,0,2,0)};
        System.Windows.Automation.AutomationProperties.SetName(image,"Equation: "+source);return image;
    }
    private static bool SafeMath(string source)
    {
        if(source.Length is 0 or >4096)return false;var depth=0;
        for(var i=0;i<source.Length;i++){if(source[i]=='\\'){i++;continue;}if(source[i]=='{'&&++depth>64)return false;if(source[i]=='}'&&--depth<0)return false;}
        return depth==0;
    }
    private static void OpenLink(object sender,RequestNavigateEventArgs e){if(e.Uri.Scheme is "https" or "http")Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri){UseShellExecute=true});e.Handled=true;}
}
