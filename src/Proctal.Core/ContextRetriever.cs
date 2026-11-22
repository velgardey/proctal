using System.Text.RegularExpressions;
namespace Proctal.Core;
public sealed class ContextRetriever
{
    private readonly List<(ContextPassage Passage,Dictionary<string,int> Words)> chunks=[];
    public ContextRetriever(IEnumerable<ContextDocument> docs)
    {
        foreach(var doc in docs)for(var offset=0;offset<doc.Text.Length;offset+=800)
        {
            var text=doc.Text.Substring(offset,Math.Min(1000,doc.Text.Length-offset));
            var words=Tokenize(text).GroupBy(w=>w).ToDictionary(g=>g.Key,g=>g.Count());
            chunks.Add((new ContextPassage(doc.Name,text),words));
        }
    }
    private static IEnumerable<string> Tokenize(string text)=>Regex.Matches(text.ToLowerInvariant(),@"[\p{L}\p{N}+#.]{2,}").Select(m=>m.Value.Trim('.')).Where(s=>!new[]{"the","and","what","with","this","that","how","for","your","you","did"}.Contains(s));
    public IReadOnlyList<ContextPassage> Retrieve(string question,int limit)
    {
        var query=Tokenize(question).Distinct().ToArray();
        var weights=query.ToDictionary(q=>q,q=>Math.Log(1+(chunks.Count+.5)/(chunks.Count(x=>x.Words.ContainsKey(q))+.5)));
        var ranked=chunks.Select(c=>new{c.Passage,Score=query.Sum(q=>c.Words.TryGetValue(q,out var tf)?weights[q]*tf/(tf+1.2):0)}).OrderByDescending(c=>c.Score).ToArray();
        return ranked.Take(Math.Clamp(limit,0,8)).Select(c=>c.Passage).ToArray();
    }
}
