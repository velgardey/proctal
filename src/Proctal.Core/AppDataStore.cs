using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Proctal.Core;
public sealed class AppDataStore
{
    public string Root {get;}
    public string Data {get;}
    public string SettingsPath=>Path.Combine(Data,"settings.json");
    private string KeyPath=>Path.Combine(Data,"azure-key.bin");
    public AppDataStore(string? root=null)
    {
        if(root is null){var current=new DirectoryInfo(AppContext.BaseDirectory);while(current is not null&&!File.Exists(Path.Combine(current.FullName,".proctal-project")))current=current.Parent;root=current?.FullName??AppContext.BaseDirectory;}
        Root=Path.GetFullPath(root);Data=Path.Combine(Root,".data");Directory.CreateDirectory(Data);
    }
    public string Key()
    {
        if(File.Exists(KeyPath))return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(KeyPath),Encoding.UTF8.GetBytes("Proctal.Azure.Key.v1"),DataProtectionScope.CurrentUser));
        return Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")??"";
    }
    public void SaveKey(string key)
    {
        if(string.IsNullOrWhiteSpace(key))return;
        File.WriteAllBytes(KeyPath,ProtectedData.Protect(Encoding.UTF8.GetBytes(key.Trim()),Encoding.UTF8.GetBytes("Proctal.Azure.Key.v1"),DataProtectionScope.CurrentUser));
    }
    public void DeleteStoredKey(){if(File.Exists(KeyPath))File.Delete(KeyPath);}
    public List<ContextDocument> LoadDocuments()=>File.Exists(Path.Combine(Data,"documents.json"))?JsonSerializer.Deserialize<List<ContextDocument>>(File.ReadAllText(Path.Combine(Data,"documents.json")))??[]:[];
    public void SaveDocuments(IEnumerable<ContextDocument> docs)
    {var path=Path.Combine(Data,"documents.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(docs));File.Move(path+".tmp",path,true);}
}
