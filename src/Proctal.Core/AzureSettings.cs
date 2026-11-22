using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace Proctal.Core;
public sealed class AzureSettings:INotifyPropertyChanged
{
    private string endpoint="https://your-resource.services.ai.azure.com/openai/v1/";
    public string Endpoint {get=>endpoint;set=>Set(ref endpoint,value);}
    private string answerDeployment="gpt-6-luna";
    public string AnswerDeployment {get=>answerDeployment;set=>Set(ref answerDeployment,value);}
    private string solDeployment="gpt-6.1-sol";
    public string SolDeployment {get=>solDeployment;set=>Set(ref solDeployment,value);}
    private string transcriptionDeployment="gpt-live-transcribe";
    public string TranscriptionDeployment {get=>transcriptionDeployment;set=>Set(ref transcriptionDeployment,value);}
    private string language="en";
    public string Language {get=>language;set=>Set(ref language,value);}
    private string outputDeviceId="";
    public string OutputDeviceId {get=>outputDeviceId;set=>Set(ref outputDeviceId,value);}
    private string inputDeviceId="";
    public string InputDeviceId {get=>inputDeviceId;set=>Set(ref inputDeviceId,value);}
    private bool includeMicrophone;
    public bool IncludeMicrophone {get=>includeMicrophone;set=>Set(ref includeMicrophone,value);}
    private bool autoAnswer=true;
    public bool AutoAnswer {get=>autoAnswer;set=>Set(ref autoAnswer,value);}
    private bool saveHistory;
    public bool SaveHistory {get=>saveHistory;set=>Set(ref saveHistory,value);}
    private bool captureExclusion=true;
    public bool CaptureExclusion {get=>captureExclusion;set=>Set(ref captureExclusion,value);}
    private bool fastProcessing=true;
    public bool FastProcessing {get=>fastProcessing;set=>Set(ref fastProcessing,value);}
    private string reasoningEffort="none";
    public string ReasoningEffort {get=>reasoningEffort;set=>Set(ref reasoningEffort,value);}
    private double opacity=.96;
    public double Opacity {get=>opacity;set=>Set(ref opacity,value);}
    private string role="";
    public string Role {get=>role;set=>Set(ref role,value);}
    private string jobDescription="";
    public string JobDescription {get=>jobDescription;set=>Set(ref jobDescription,value);}
    private string instructions="";
    public string Instructions {get=>instructions;set=>Set(ref instructions,value);}
    private string mode="General";
    public string Mode {get=>mode;set=>Set(ref mode,value);}
    private bool codeFirst=true;
    public bool CodeFirst {get=>codeFirst;set=>Set(ref codeFirst,value);}
    private bool launchOverlayOnStartup=true;
    public bool LaunchOverlayOnStartup {get=>launchOverlayOnStartup;set=>Set(ref launchOverlayOnStartup,value);}
    private string responseStructure="";
    public string ResponseStructure {get=>responseStructure.Length>0?responseStructure:CodeFirst?"Problem → brute force → optimize":"Insight → solution";set=>Set(ref responseStructure,value??"");}
    private string codingLanguage="Python";
    public string CodingLanguage {get=>codingLanguage;set=>Set(ref codingLanguage,value);}
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field,T value,[CallerMemberName]string name="")
    {
        if(EqualityComparer<T>.Default.Equals(field,value))return;
        field=value;PropertyChanged?.Invoke(this,new(name));
        if(name==nameof(CodeFirst)&&responseStructure.Length==0)PropertyChanged?.Invoke(this,new(nameof(ResponseStructure)));
    }
    public AzureSettings Snapshot(){var copy=(AzureSettings)MemberwiseClone();copy.PropertyChanged=null;return copy;}
    public static string NormalizeEndpoint(string endpoint)
    {
        if(!Uri.TryCreate(endpoint.Trim(),UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.UserInfo!=""||!uri.IsDefaultPort||uri.Query!=""||uri.Fragment!=""||
            !(uri.Host.EndsWith(".openai.azure.com",StringComparison.OrdinalIgnoreCase)||uri.Host.EndsWith(".services.ai.azure.com",StringComparison.OrdinalIgnoreCase))||
            !new[]{"","/","/openai/v1","/openai/v1/"}.Contains(uri.AbsolutePath))
            throw new ArgumentException("Enter your Azure HTTPS resource URL or its /openai/v1 endpoint.");
        return $"https://{uri.Host}/openai/v1/";
    }
    public static AzureSettings Load(string path)=>File.Exists(path)?JsonSerializer.Deserialize<AzureSettings>(File.ReadAllText(path))??new():new();
    public void Save(string path)
    {
        Endpoint=NormalizeEndpoint(Endpoint);
        if(string.IsNullOrWhiteSpace(AnswerDeployment)||string.IsNullOrWhiteSpace(TranscriptionDeployment))throw new ArgumentException("Deployment names are required.");
        if(ResponseStructure is not ("Code first" or "Insight → solution" or "Problem → brute force → optimize"))throw new ArgumentException("Choose a response structure from the available presets.");
        if(Instructions.Length>4000)throw new ArgumentException("Keep custom response instructions within 4000 characters.");
        CodingLanguage=string.IsNullOrWhiteSpace(CodingLanguage)?"Python":CodingLanguage.Trim()[..Math.Min(CodingLanguage.Trim().Length,40)];
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);
    }
}
