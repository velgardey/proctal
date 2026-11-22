using System.Text.Json;
namespace Proctal.Core;
public sealed class SessionStore
{
    private readonly string path;
    private readonly SemaphoreSlim gate=new(1,1);
    private readonly HashSet<string> deleted=[];
    public string? RecoveryBackupPath {get;private set;}
    public SessionStore(string dir){Directory.CreateDirectory(dir);path=Path.Combine(dir,"answers.json");}
    public async Task<IReadOnlyList<AnswerRecord>> ReadAsync()
    {
        await gate.WaitAsync();try{return await ReadUnlocked();}finally{gate.Release();}
    }
    private async Task<List<AnswerRecord>> ReadUnlocked()
    {
        if(!File.Exists(path))return [];
        var json=await File.ReadAllTextAsync(path);
        try
        {
            var records=JsonSerializer.Deserialize<List<AnswerRecord>>(json)??[];
            if(records.Any(r=>r is null||r.Id is null||r.Question is null||r.Answer is null))throw new JsonException("History has invalid entries.");
            return records;
        }
        catch(JsonException)
        {
            var backup=Path.Combine(Path.GetDirectoryName(path)!,$"answers.corrupt-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
            File.Move(path,backup);RecoveryBackupPath=backup;return [];
        }
    }
    public async Task SaveAsync(AnswerRecord r)
    {
        await gate.WaitAsync();try{if(deleted.Contains(r.Id))return;var records=await ReadUnlocked();records.RemoveAll(x=>x.Id==r.Id);records.Insert(0,r);await Write(records.Take(250).ToList());}finally{gate.Release();}
    }
    public async Task DeleteAsync(string id)
    {
        await gate.WaitAsync();try{var records=await ReadUnlocked();records.RemoveAll(x=>x.Id==id);await Write(records);deleted.Add(id);}finally{gate.Release();}
    }
    private async Task Write(List<AnswerRecord> records){var temp=path+".tmp";await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(records));File.Move(temp,path,true);}
}
