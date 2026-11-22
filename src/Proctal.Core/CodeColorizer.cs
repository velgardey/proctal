using System.Text.RegularExpressions;
namespace Proctal.Core;
public enum CodeTokenKind{Plain,Keyword,String,Comment,Number}
public readonly record struct CodeToken(string Text,CodeTokenKind Kind);
public static class CodeColorizer
{
    private static readonly HashSet<string> Supported=new(StringComparer.OrdinalIgnoreCase){"python","py","javascript","js","typescript","ts","csharp","cs","c#","java","cpp","c++","c","go","rust","rs","sql","json","bash","sh","powershell","ps1"};
    private static readonly HashSet<string> Keywords=new(StringComparer.Ordinal){"def","class","return","if","else","elif","for","while","in","not","and","or","True","False","None","import","from","as","try","except","raise","with","yield","lambda","pass","break","continue","const","let","var","function","async","await","new","null","true","false","export","interface","type","public","private","protected","static","void","int","string","bool","double","using","namespace","this","switch","case","default","throw","catch","finally","package","func","defer","range","nil","struct","fn","mut","impl","match","self","use","SELECT","FROM","WHERE","JOIN","ON","AS","GROUP","BY","ORDER","LIMIT","INSERT","UPDATE","DELETE","NULL","COUNT","DISTINCT"};
    private static readonly Regex Lex=new("(?<str>\"\"\"[\\s\\S]*?(?:\"\"\"|$)|'''[\\s\\S]*?(?:'''|$)|@\"(?:\"\"|[^\"])*(?:\"|$)|\"(?:\\\\.|[^\"\\\\])*(?:\"|$)|'(?:\\\\.|[^'\\\\])*(?:'|$)|`(?:\\\\.|[^`\\\\])*(?:`|$))|(?<comment>//[^\\r\\n]*|/\\*[\\s\\S]*?(?:\\*/|$)|\\#[^\\r\\n]*|--[^\\r\\n]*)|(?<number>\\b(?:0[xX][0-9a-fA-F]+|\\d+(?:\\.\\d+)?)\\b)|(?<word>\\b[A-Za-z_][A-Za-z_0-9]*\\b)",RegexOptions.Compiled,TimeSpan.FromMilliseconds(80));
    public static IReadOnlyList<CodeToken> Tokenize(string code,string language)
    {
        if(!Supported.Contains(language)||code.Length>60000)return [new(code,CodeTokenKind.Plain)];
        var tokens=new List<CodeToken>();var offset=0;
        try{foreach(Match m in Lex.Matches(code)){if(m.Index>offset)tokens.Add(new(code[offset..m.Index],CodeTokenKind.Plain));var kind=m.Groups["str"].Success?CodeTokenKind.String:m.Groups["comment"].Success?CodeTokenKind.Comment:m.Groups["number"].Success?CodeTokenKind.Number:Keywords.Contains(m.Value)?CodeTokenKind.Keyword:CodeTokenKind.Plain;tokens.Add(new(m.Value,kind));offset=m.Index+m.Length;}}
        catch(RegexMatchTimeoutException){return [new(code,CodeTokenKind.Plain)];}
        if(offset<code.Length)tokens.Add(new(code[offset..],CodeTokenKind.Plain));return tokens;
    }
}
