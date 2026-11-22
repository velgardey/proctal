using System;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Extensions.Mathematics;
namespace Proctal.Desktop;
internal static class MathMarkdown
{
    private static readonly MarkdownPipeline CodePipeline=new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();
    private static readonly MarkdownPipeline MathPipeline=new MarkdownPipelineBuilder().UsePreciseSourceLocation().UseMathematics().Build();
    private static readonly Regex BareFraction=new(@"(?<!\\)\\(?:frac|dfrac|tfrac)\s*(?=\{)",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
    public static string NormalizeFormula(string source)
    {
        // Repair model typos only inside equations; programming strings stay untouched.
        source=Regex.Replace(source,@"(?<![\\\p{L}])/(text|frac|dfrac|tfrac|sqrt|times|cdot)(?![A-Za-z])",m=>"\\"+m.Groups[1].Value);
        return Regex.Replace(source,@"(?<!\\)\\(?:dfrac|tfrac)(?![A-Za-z])",@"\frac");
    }
    // CommonMark source spans protect code in lists/quotes and constrain delimiter pairing.
    // JSON has already been decoded by the protocol; never unescape raw answer text here.
    public static string Normalize(string text)
    {
        if(!text.Contains("\\(",StringComparison.Ordinal)&&!text.Contains("\\[",StringComparison.Ordinal))return WrapBareFractions(text);
        var protectedCode=new bool[text.Length];
        foreach(var item in Markdown.Parse(text,CodePipeline).Descendants())
        {
            if(item is not (CodeBlock or CodeInline))continue;
            var start=Math.Clamp(item.Span.Start,0,text.Length);var end=Math.Clamp(item.Span.End+1,start,text.Length);
            Array.Fill(protectedCode,true,start,end-start);
        }
        var escaped=new bool[text.Length];var slashRun=0;
        for(var i=0;i<text.Length;i++){escaped[i]=slashRun%2!=0;slashRun=text[i]=='\\'?slashRun+1:0;}
        // Linear-time searches also bound unfinished streamed delimiter handling.
        var nextParen=new int[text.Length+1];var nextBracket=new int[text.Length+1];var nextCode=new int[text.Length+1];
        var paren=text.Length;var bracket=text.Length;var code=text.Length;
        nextParen[^1]=nextBracket[^1]=nextCode[^1]=text.Length;
        for(var i=text.Length-1;i>=0;i--)
        {
            if(protectedCode[i])code=i;
            else if(text[i]=='\\'&&!escaped[i]&&i+1<text.Length){if(text[i+1]==')')paren=i;else if(text[i+1]==']')bracket=i;}
            nextParen[i]=paren;nextBracket[i]=bracket;nextCode[i]=code;
        }
        var output=new StringBuilder(text.Length);
        for(var i=0;i<text.Length;)
        {
            if(!protectedCode[i]&&text[i]=='\\'&&!escaped[i]&&i+1<text.Length&&text[i+1] is '(' or '[')
            {
                var display=text[i+1]=='[';var end=(display?nextBracket:nextParen)[i+2];
                if(end<text.Length&&end<nextCode[i+2])
                {
                    if(display){if(output.Length>0&&output[^1]!='\n')output.Append('\n');output.Append("$$\n");}
                    else output.Append('$');
                    output.Append(text,i+2,end-i-2);output.Append(display?"\n$$\n":"$");i=end+2;continue;
                }
            }
            output.Append(text[i++]);
        }
        return WrapBareFractions(output.ToString());
    }
    private static string WrapBareFractions(string text)
    {
        if(!text.Contains("frac",StringComparison.Ordinal))return text;
        var protectedContent=new bool[text.Length];
        foreach(var item in Markdown.Parse(text,MathPipeline).Descendants())
        {
            if(item is not (CodeBlock or CodeInline or MathBlock or MathInline))continue;
            var start=Math.Clamp(item.Span.Start,0,text.Length);var end=Math.Clamp(item.Span.End+1,start,text.Length);
            Array.Fill(protectedContent,true,start,end-start);
        }
        var output=new StringBuilder(text.Length);var cursor=0;
        foreach(Match match in BareFraction.Matches(text))
        {
            if(match.Index<cursor||protectedContent[match.Index])continue;
            var end=match.Index+match.Length;var valid=true;
            for(var argument=0;argument<2;argument++)
            {
                while(end<text.Length&&char.IsWhiteSpace(text[end]))end++;
                if(end>=text.Length||text[end]!='{'){valid=false;break;}
                var depth=0;
                do
                {
                    if(end>=text.Length||end-match.Index>=4096||protectedContent[end]||depth>64){valid=false;break;}
                    var c=text[end++];if(c=='\\'&&end<text.Length){end++;continue;}if(c=='{')depth++;else if(c=='}')depth--;
                }while(depth>0);
                if(!valid)break;
            }
            if(!valid)continue;
            // Keep adjacent factors inside math; Markdig rejects a closing dollar before a letter or digit.
            while(end<text.Length&&end-match.Index<4096&&!protectedContent[end]&&char.IsLetterOrDigit(text[end]))end++;
            output.Append(text,cursor,match.Index-cursor).Append('$').Append(text,match.Index,end-match.Index).Append('$');cursor=end;
        }
        if(cursor==0)return text;return output.Append(text,cursor,text.Length-cursor).ToString();
    }
}
