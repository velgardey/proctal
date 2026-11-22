using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
public sealed class ProctalController:INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly Dispatcher dispatcher;
    private readonly AppDataStore data;
    private readonly SessionStore store;
    private readonly AudioCaptureService capture=new();
    private readonly TranscriptBuffer transcript=new();
    private readonly QuestionAssembler questions=new();
    private readonly AnswerVersion answers=new();
    private readonly DispatcherTimer timer;
    private readonly DispatcherTimer appearanceTimer;
    private string historyQuery="",selectedHistoryId="";
    private bool isAnswering;
    private IReadOnlyDictionary<AudioSource,LiveTranscriptionClient> clients=new Dictionary<AudioSource,LiveTranscriptionClient>();
    private CancellationTokenSource? listenCancellation,answerCancellation;
    private long listenEpoch;
    private DateTimeOffset practiceAt;
    private DateTimeOffset speechSince=DateTimeOffset.MinValue;
    private string practiceQuestion="";
    private string status="Ready. Configure your context, then start listening.";
    private string question="Your next interview question will appear here.";
    private string answer="F11 answers stored transcription or typed text immediately. F10 attaches a screenshot; F9 or double F11 answers images. F12 shows or hides this window.\n\nUse Ctrl+Shift+I to enable clicking on the overlay.";
    private string transcriptText="",manualText="",tier="Fast mode · requested",privacy="Capture exclusion not checked yet",latency="";
    private bool listening,starting,interactive,pinned,stopping;
    private Task? stopTask;
    private Task? captureRequest;
    private Task? imageSubmission;
    private string imageSubmissionDraft="",imageSubmissionEffort="",imageSubmissionDeployment="";
    private long imageSubmissionGeneration,imageSubmissionVersion;
    private bool isCapturing;
    private long imageGeneration,settingsRevision,activeSettingsRevision;
    private string heardQuestion="",activeAnswerQuestion="",activeAnswerMode="",activeAnswerEffort="",activeAnswerReference="",activeAnswerDeployment="";
    private ScreenshotItem[] activeImages=[];
    private string[] heardIds=[];
    private readonly HashSet<string> manuallyAnsweredIds=[];
    private readonly HashSet<string> completedSessionIds=[];
    private double outputLevel,inputLevel;
    public AzureSettings Settings {get;}
    public OverlayPreferences Overlay {get;}
    public ObservableCollection<ContextDocument> Documents {get;}=[];
    public ObservableCollection<ScreenshotItem> Screenshots {get;}=[];
    public ObservableCollection<AnswerRecord> History {get;}=[];
    public ObservableCollection<AudioDevice> OutputDevices {get;}=[];
    public ObservableCollection<AudioDevice> InputDevices {get;}=[];
    private AudioDevice? selectedOutput,selectedInput;
    public AudioDevice? SelectedOutput {get=>selectedOutput;set=>Set(ref selectedOutput,value);}
    public AudioDevice? SelectedInput {get=>selectedInput;set=>Set(ref selectedInput,value);}
    public Func<Task<ScreenshotItem>>? CaptureScreenshot {get;set;}
    public bool DialogOpen {get;set;}
    public string Status {get=>status;private set=>Set(ref status,value);}
    public string Question {get=>question;private set{Set(ref question,value);Notify(nameof(DisplayQuestion));}}
    public string Answer {get=>answer;private set{Set(ref answer,value);Notify(nameof(DisplayAnswer));}}
    public string DisplayQuestion=>History.FirstOrDefault(h=>h.Id==selectedHistoryId)?.Question??Question;
    public string DisplayAnswer=>History.FirstOrDefault(h=>h.Id==selectedHistoryId)?.Answer??Answer;
    public bool IsBrowsingHistory=>selectedHistoryId.Length>0;
    public bool IsAnswering{get=>isAnswering;private set{Set(ref isAnswering,value);Notify(nameof(CanFollowUp));}}
    public bool CanFollowUp=>!IsAnswering&&History.Any(h=>h.Answer.Length>0);
    private bool hasSavedConfiguration;
    public bool HasSavedConfiguration{get=>hasSavedConfiguration;private set=>Set(ref hasSavedConfiguration,value);}
    public string HistoryQuery{get=>historyQuery;set{Set(ref historyQuery,value);Notify(nameof(VisibleHistory));}}
    public IEnumerable<AnswerRecord> VisibleHistory=>History.Where(h=>HistoryQuery.Length==0||h.Question.Contains(HistoryQuery,StringComparison.OrdinalIgnoreCase)||h.Answer.Contains(HistoryQuery,StringComparison.OrdinalIgnoreCase));
    public string Transcript {get=>transcriptText;private set=>Set(ref transcriptText,value);}
    public string ManualText {get=>manualText;set{Set(ref manualText,value);NotifyInput();}}
    public bool IsCapturing{get=>isCapturing;private set{Set(ref isCapturing,value);Notify(nameof(CanAnswerImages));Notify(nameof(CanSubmitDeep));}}
    public bool CanAnswerImages=>IsCapturing||Screenshots.Count>0;
    public bool CanSubmitText=>ManualText.Trim().Length>0||SpeechInput().Text.Length>0||practiceQuestion.Length>0||transcript.Snapshot().Any(t=>!string.IsNullOrWhiteSpace(t.Text));
    public bool CanSubmitDeep=>CanAnswerImages||CanSubmitText||History.Count>0;
    public string AnswerInputHint=>ManualText.Trim().Length>0?"Answer uses your typed question":SpeechInput().Text.Length>0?"Answer uses stored transcription":practiceQuestion.Length>0?"Answer uses the current practice question":CanSubmitText?"Answer uses stored transcription":"No stored transcription · Ctrl+Alt+Q to type";
    public string Tier {get=>tier;private set=>Set(ref tier,value);}
    public string Privacy {get=>privacy;private set=>Set(ref privacy,value);}
    public string Latency {get=>latency;private set=>Set(ref latency,value);}
    public bool IsListening {get=>listening;private set{Set(ref listening,value);Notify(nameof(ListenButtonText));Notify(nameof(CanEditAudio));Notify(nameof(AudioStatus));}}
    public bool IsStarting {get=>starting;private set{Set(ref starting,value);Notify(nameof(CanToggleListen));Notify(nameof(CanEditAudio));Notify(nameof(ListenButtonText));Notify(nameof(AudioStatus));}}
    public bool CanToggleListen=>!stopping;
    public bool CanEditAudio=>!IsListening&&!IsStarting&&!stopping;
    public string AudioStatus=>stopping?"Pausing…":IsStarting?"Connecting…":IsListening?"Listening":"Paused";
    public string ListenButtonText=>(IsStarting?stopping?"Pausing…":"Cancel connection":IsListening?"Pause listening":"Start listening")+" · F8";
    public bool Interactive {get=>interactive;set{Set(ref interactive,value);Notify(nameof(InteractionLabel));}}
    public string InteractionLabel=>Interactive?"Mouse controls available · focus stays elsewhere":"Keyboard mode · focus stays in your editor";
    public bool Pinned {get=>pinned;set=>Set(ref pinned,value);}
    public double OutputLevel {get=>outputLevel;private set=>Set(ref outputLevel,value);}
    public double InputLevel {get=>inputLevel;private set=>Set(ref inputLevel,value);}
    public string DataPath=>data.Data;
    internal Func<AzureSettings,LunaClient> LunaFactory {get;set;}
    public void ReportStatus(string message)=>Status=message;
    public void CancelAnswer(){if(!IsAnswering)return;answerCancellation?.Cancel();Status="Cancelling the answer…";}
    public ProctalController(Dispatcher dispatcher,string? dataRoot=null)
    {
        this.dispatcher=dispatcher;data=new AppDataStore(dataRoot);store=new SessionStore(data.Data);LunaFactory=settings=>new LunaClient(settings,data.Key());
        try{Settings=AzureSettings.Load(data.SettingsPath);Settings.Endpoint=AzureSettings.NormalizeEndpoint(Settings.Endpoint);HasSavedConfiguration=File.Exists(data.SettingsPath)&&!string.IsNullOrWhiteSpace(Settings.AnswerDeployment)&&!string.IsNullOrWhiteSpace(Settings.TranscriptionDeployment);}catch(Exception){Settings=new AzureSettings();Status="Saved settings could not be read. Re-enter them in Setup; the existing file is retained.";}
        var appearancePath=Path.Combine(data.Data,"overlay.json");try{Overlay=OverlayPreferences.Load(appearancePath);if(!File.Exists(appearancePath)&&Settings.Opacity!=.96)Overlay.Opacity=Settings.Opacity;}catch(Exception){Overlay=new();Status="Overlay preferences could not be read; defaults restored.";}
        appearanceTimer=new DispatcherTimer(TimeSpan.FromMilliseconds(500),DispatcherPriority.Background,(_,_)=>SaveAppearance(),dispatcher);appearanceTimer.Stop();
        Overlay.PropertyChanged+=(_,_)=>{appearanceTimer.Stop();appearanceTimer.Start();};
        Settings.PropertyChanged+=(_,e)=>
        {
            settingsRevision++;
            if(e.PropertyName is nameof(AzureSettings.AutoAnswer) or nameof(AzureSettings.IncludeMicrophone))PersistDailyOptions();
        };
        History.CollectionChanged+=(_,_)=>{completedSessionIds.IntersectWith(History.Select(h=>h.Id));if(selectedHistoryId.Length>0&&!History.Any(h=>h.Id==selectedHistoryId)){selectedHistoryId="";Notify(nameof(IsBrowsingHistory));}Notify(nameof(VisibleHistory));Notify(nameof(DisplayQuestion));Notify(nameof(DisplayAnswer));Notify(nameof(CanFollowUp));Notify(nameof(CanSubmitDeep));};
        Screenshots.CollectionChanged+=(_,_)=>{Notify(nameof(CanAnswerImages));Notify(nameof(CanSubmitDeep));};
        try{foreach(var doc in data.LoadDocuments())Documents.Add(doc);}catch(Exception){Status="Saved context could not be read. Import it again; the existing file is retained.";}
        capture.Frame+=(source,pcm)=>{var active=Volatile.Read(ref clients);if(active.TryGetValue(source,out var client))client.Enqueue(pcm);};
        capture.Level+=(source,level)=>dispatcher.BeginInvoke(()=>{if(source==AudioSource.Interviewer)OutputLevel=Math.Min(100,level*500);else InputLevel=Math.Min(100,level*500);},DispatcherPriority.Background);
        capture.Fault+=message=>dispatcher.BeginInvoke(async()=>{await StopListeningAsync();Status=message;});
        timer=new DispatcherTimer(TimeSpan.FromMilliseconds(150),DispatcherPriority.Background,(_,_)=>CheckQuestion(),dispatcher);timer.Start();
        RefreshDevices();
    }
    private void Notify([CallerMemberName]string name="")=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
    private void NotifyInput(){Notify(nameof(CanSubmitText));Notify(nameof(AnswerInputHint));Notify(nameof(CanSubmitDeep));}
    private (string Text,string[] Ids) SpeechInput()
    {
        var pending=questions.Snapshot();var snapshot=transcript.Snapshot().Where(t=>t.Source==AudioSource.Interviewer&&t.At>=speechSince).ToArray();
        var ready=pending.Count>0?pending.Select(t=>t.ItemId).ToHashSet():heardIds.ToHashSet();
        // Item order is stable across partial/final updates. Earlier orphan partials must not override a ready question.
        var boundary=Array.FindIndex(snapshot,t=>ready.Contains(t.ItemId));
        var live=snapshot.Skip(Math.Max(0,boundary)).Where(t=>!t.IsFinal&&!manuallyAnsweredIds.Contains(t.ItemId)).ToArray();
        TranscriptTurn[] turns;
        if(pending.Count>0)turns=pending.Concat(live.Where(t=>t.At>=pending[0].At)).OrderBy(t=>t.At).ToArray();
        else if(boundary>=0&&live.Length>0&&!heardIds.All(manuallyAnsweredIds.Contains))turns=snapshot.Skip(boundary).Where(t=>ready.Contains(t.ItemId)||live.Any(l=>l.ItemId==t.ItemId)).ToArray();
        else turns=live.TakeLast(1).ToArray();
        return turns.Length>0?(string.Join(" ",turns.Select(t=>t.Text.Trim())),turns.Select(t=>t.ItemId).ToArray()):(heardQuestion,heardIds);
    }
    private void SetHeard(IEnumerable<TranscriptTurn> turns)
    {
        var group=turns.ToArray();heardQuestion=string.Join(" ",group.Select(t=>t.Text.Trim()));heardIds=group.Select(t=>t.ItemId).ToArray();
    }
    private void SaveAppearance(){appearanceTimer.Stop();try{Overlay.Save(Path.Combine(data.Data,"overlay.json"));}catch(Exception){Status="Overlay changes are active, but could not be saved locally.";}}
    private void PersistDailyOptions()
    {
        if(!HasSavedConfiguration)return;
        try{var saved=AzureSettings.Load(data.SettingsPath);saved.AutoAnswer=Settings.AutoAnswer;saved.IncludeMicrophone=Settings.IncludeMicrophone;saved.Save(data.SettingsPath);}
        catch(Exception){Status="Audio options are active, but could not be saved locally.";}
    }
    public void ShowLatestAnswer(){selectedHistoryId="";Notify(nameof(DisplayQuestion));Notify(nameof(DisplayAnswer));Notify(nameof(IsBrowsingHistory));}
    public void BrowseHistory(int direction)
    {
        if(History.Count==0)return;var current=History.ToList().FindIndex(h=>h.Id==selectedHistoryId);
        var next=direction<0?Math.Min(History.Count-1,current<0?Math.Min(1,History.Count-1):current+1):current-1;
        if(next<0){ShowLatestAnswer();return;}selectedHistoryId=History[next].Id;Notify(nameof(DisplayQuestion));Notify(nameof(DisplayAnswer));Notify(nameof(IsBrowsingHistory));
    }
    public async Task FollowUpAsync(string action)
    {
        if(IsAnswering)throw new ArgumentException("Wait for this answer to finish, or cancel it before a follow-up.");
        if(!History.Any(h=>h.Answer.Length>0))throw new ArgumentException("Get an answer first, then choose a follow-up.");
        var referenceId=History.FirstOrDefault(h=>h.Question==DisplayQuestion&&h.Answer==DisplayAnswer)?.Id;
        var reference="\nPREVIOUS QUESTION\n"+DisplayQuestion+"\nPREVIOUS ANSWER (reference only)\n"+DisplayAnswer+TranscriptReference(transcript.Snapshot());
        var prompt=action switch{"explain"=>"Explain the approach and its tradeoffs concisely.","edges"=>"Show important edge cases and a few concrete tests for the previous solution.","optimize"=>"Optimize the previous solution if worthwhile. Show improved code first, then complexity and tradeoffs.",_=>throw new ArgumentException("Choose Explain, Edge cases, or Optimize.")};
        ShowLatestAnswer();await AnswerAsync(prompt,false,"Technical",reference,referenceAnswerId:referenceId);
    }
    private void Set<T>(ref T field,T value,[CallerMemberName]string name=""){if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;Notify(name);}
    public async Task Safe(Func<Task> operation)
    {
        try{await operation();}
        catch(OperationCanceledException){Status="The operation was cancelled or timed out. Retry when ready.";}
        catch(CloudServiceException e){Status=e.Message;}
        catch(ArgumentException e){Status=e.Message;}
        catch(InvalidDataException e){Status=e.Message;}
        catch(IOException){Status="Local files could not be read or saved. Check folder access and available disk space, then retry.";}
        catch(UnauthorizedAccessException){Status="Proctal cannot access its local files. Check the project folder permissions, then retry.";}
        catch(System.ComponentModel.Win32Exception){Status="Windows could not capture the screen. Unlock the desktop and run Proctal in your normal Windows session.";}
        catch(System.Security.Cryptography.CryptographicException){Status="The saved key cannot be opened by this Windows user. Enter it again in Setup.";}
        catch(Exception){Status="The operation could not finish. Check the selected devices and connection, then retry.";}
    }
    public async Task LoadHistoryAsync(){await Safe(async()=>{foreach(var item in await store.ReadAsync())History.Add(item);});if(store.RecoveryBackupPath is not null)Status="Unreadable history preserved as "+Path.GetFileName(store.RecoveryBackupPath)+". New history can be saved.";}
    public void RefreshDevices()
    {
        if(!CanEditAudio){Status="Pause listening before refreshing audio devices.";return;}
        var output=SelectedOutput?.Id??Settings.OutputDeviceId;var input=SelectedInput?.Id??Settings.InputDeviceId;
        try
        {
            OutputDevices.Clear();foreach(var d in AudioCaptureService.Devices(false))OutputDevices.Add(d);
            InputDevices.Clear();foreach(var d in AudioCaptureService.Devices(true))InputDevices.Add(d);
            SelectedOutput=OutputDevices.FirstOrDefault(d=>d.Id==output)??(string.IsNullOrWhiteSpace(output)?OutputDevices.FirstOrDefault():null);
            SelectedInput=InputDevices.FirstOrDefault(d=>d.Id==input)??(string.IsNullOrWhiteSpace(input)?InputDevices.FirstOrDefault():null);Notify(nameof(SelectedOutput));Notify(nameof(SelectedInput));
            if((!string.IsNullOrWhiteSpace(output)&&SelectedOutput is null)||(Settings.IncludeMicrophone&&!string.IsNullOrWhiteSpace(input)&&SelectedInput is null))Status="A saved audio device is unavailable. Select your current output and microphone before listening.";
        }
        catch(Exception){Status="Audio devices could not be listed. Typed questions and image attachments are still available.";}
    }
    public void SaveSettings(string newKey)
    {
        Settings.OutputDeviceId=SelectedOutput?.Id??Settings.OutputDeviceId;Settings.InputDeviceId=SelectedInput?.Id??Settings.InputDeviceId;
        Settings.Save(data.SettingsPath);data.SaveKey(newKey);
        HasSavedConfiguration=true;
        Tier=Settings.FastProcessing?"Fast requested · reasoning "+Settings.ReasoningEffort:"Standard processing · reasoning "+Settings.ReasoningEffort;
        Status=IsListening?"Saved. Restart listening to apply device or transcription changes.":"Settings saved locally.";
    }
    public void DeleteStoredKey(){data.DeleteStoredKey();Status="Stored key removed. An existing environment key, if present, can still be used.";}
    public async Task CheckConnectionAsync()
    {
        Status="Checking Luna and live transcription…";using var ct=new CancellationTokenSource(TimeSpan.FromSeconds(45));var key=data.Key();
        var client=new LunaClient(Settings,key);client.ProcessingTier+=t=>dispatcher.BeginInvoke(()=>Tier=DisplayTier(t));
        var output=new StringBuilder();await foreach(var part in client.StreamAnswerAsync("Reply with exactly: Proctal connected.",[],"","Connection check",ct.Token))output.Append(part);
        await LiveTranscriptionClient.CheckAsync(Settings,key,ct.Token);Status="Luna and live transcription connected. No microphone audio was sent by this check.";
    }
    public async Task CheckSolConnectionAsync()
    {
        if(string.IsNullOrWhiteSpace(Settings.SolDeployment))throw new ArgumentException("Enter the Sol deployment in Setup → Connection first.");
        var settings=Settings.Snapshot();settings.AnswerDeployment=Settings.SolDeployment.Trim();settings.ReasoningEffort="high";
        Status="Checking Sol · high reasoning…";using var ct=new CancellationTokenSource(TimeSpan.FromMinutes(5));var output=new StringBuilder();
        await foreach(var part in LunaFactory(settings).StreamAnswerAsync("Reply exactly: ready",[],"","Connection check",ct.Token))output.Append(part);
        if(string.IsNullOrWhiteSpace(output.ToString()))throw new CloudServiceException("Sol returned no text. Check the deployment and retry.");
        Status="Sol connected. F6 is ready; F11 keeps using Luna.";
    }
    public async Task ToggleListeningAsync(){if(IsListening||IsStarting)await StopListeningAsync();else await StartListeningAsync();}
    private IEnumerable<string> Vocabulary()=>TranscriptionProtocol.Vocabulary(new[]{Settings.Role,Settings.JobDescription}.Concat(Documents.Select(d=>d.Text)));
    public async Task StartListeningAsync()
    {
        if(IsListening||IsStarting||stopping)return;
        if(SelectedOutput is null)throw new InvalidDataException("Select the device playing your interview audio.");
        if(Settings.IncludeMicrophone&&SelectedInput is null)throw new InvalidDataException("Select your microphone, or turn off microphone capture before listening.");
        SaveSettings("");
        IsStarting=true;speechSince=DateTimeOffset.UtcNow;var epoch=++listenEpoch;var cancellation=new CancellationTokenSource();listenCancellation=cancellation;var token=cancellation.Token;
        var local=new Dictionary<AudioSource,LiveTranscriptionClient>();
        try
        {
            Status="Connecting live transcription…";var key=data.Key();var sources=Settings.IncludeMicrophone?new[]{AudioSource.Interviewer,AudioSource.Candidate}:new[]{AudioSource.Interviewer};
            foreach(var source in sources)
            {
                var client=new LiveTranscriptionClient();local[source]=client;
                client.Event+=json=>dispatcher.BeginInvoke(()=>{if(epoch==listenEpoch)AcceptTranscriptionEvent(json,source);});
                client.Fault+=message=>dispatcher.BeginInvoke(async()=>{if(epoch!=listenEpoch)return;await StopListeningAsync();Status=message;});
                await client.ConnectAsync(Settings,key,Vocabulary(),token);
            }
            token.ThrowIfCancellationRequested();if(epoch!=listenEpoch)return;
            Volatile.Write(ref clients,local);capture.Start(Settings.OutputDeviceId,Settings.IncludeMicrophone?Settings.InputDeviceId:null);IsListening=true;Status="Listening to your selected interview output. Answers appear as text.";
        }
        catch
        {
            foreach(var client in local.Values)await client.StopAsync();if(epoch==listenEpoch){capture.Stop();IsListening=false;Volatile.Write(ref clients,new Dictionary<AudioSource,LiveTranscriptionClient>());}throw;
        }
        finally{if(epoch==listenEpoch&&!stopping)IsStarting=false;}
    }
    public Task StopListeningAsync(){if(stopTask is {IsCompleted:false})return stopTask;return stopTask=StopListeningCoreAsync();}
    private void AcceptTranscriptionEvent(string json,AudioSource source)
    {
        var turn=transcript.Accept(json,source);if(turn is null)return;
        Transcript=string.Join("\n\n",transcript.Snapshot().TakeLast(14).Select(t=>$"{(t.Source==AudioSource.Interviewer?"Interviewer":"You")}{(t.IsFinal?"":" (live)")}: {t.Text}"));
        if(source==AudioSource.Interviewer&&heardIds.Contains(turn.ItemId))SetHeard(transcript.Snapshot().Where(t=>t.Source==AudioSource.Interviewer&&heardIds.Contains(t.ItemId)));
        if(turn.IsFinal&&source==AudioSource.Interviewer)
        {
            if((IsListening||stopping)&&!manuallyAnsweredIds.Contains(turn.ItemId))questions.Accept(turn);
            if(!heardIds.Contains(turn.ItemId)&&!IsListening&&!stopping)SetHeard([turn]);
            if(!IsAnswering&&History.Count==0)Question=turn.Text;
        }
        NotifyInput();
    }
    private async Task StopListeningCoreAsync()
    {
        stopping=true;IsListening=false;IsStarting=true;
        try
        {
            var cancellation=listenCancellation;listenCancellation=null;if(cancellation is not null)await cancellation.CancelAsync();
            var previous=Volatile.Read(ref clients);Volatile.Write(ref clients,new Dictionary<AudioSource,LiveTranscriptionClient>());capture.Stop();
            var drained=await Task.WhenAll(previous.Values.Select(client=>client.StopAsync()));
            await dispatcher.InvokeAsync(()=>{},DispatcherPriority.Background);
            var pending=questions.Snapshot();if(pending.Count>0)SetHeard(pending);
            ++listenEpoch;cancellation?.Dispose();questions.Clear();NotifyInput();OutputLevel=0;InputLevel=0;Status=drained.All(value=>value)?"Listening paused. F12 changes visibility only.":"Listening paused before all final captions arrived. The last spoken words may be missing; check captions and retry.";
        }
        finally{stopping=false;IsStarting=false;stopTask=null;}
    }
    private void CheckQuestion()
    {
        if(!IsListening||IsAnswering||capture.IsInterviewerSpeaking||Volatile.Read(ref clients).TryGetValue(AudioSource.Interviewer,out var client)&&client.HasPendingAudio)return;var turns=questions.Snapshot();var completed=questions.Flush(DateTimeOffset.UtcNow);if(completed is null)return;
        SetHeard(turns);NotifyInput();if(History.Count==0)Question=completed;
        if(Settings.AutoAnswer&&QuestionAssembler.LooksLikeQuestion(completed)&&!Pinned&&Screenshots.Count==0&&!IsStarting&&!heardIds.All(manuallyAnsweredIds.Contains))_=Safe(()=>AnswerAsync(completed,false));
        else Status="Question ready. F11 to answer.";
    }
    public Task SubmitAsync()=>SubmitTextAsync(null);
    public Task SubmitDeepAsync()=>SubmitAlternateAsync("xhigh",null);
    public Task SubmitSolAsync()
    {
        if(string.IsNullOrWhiteSpace(Settings.SolDeployment))throw new ArgumentException("Enter the Sol deployment in Setup → Connection first.");
        return SubmitAlternateAsync("high",Settings.SolDeployment.Trim());
    }
    private async Task SubmitAlternateAsync(string? reasoningEffort,string? deployment)
    {
        if(CanAnswerImages){await SubmitImagesWithEffortAsync(reasoningEffort,deployment);return;}
        var input=SpeechInput();var fresh=input.Text.Length>0&&input.Ids.Any(id=>!manuallyAnsweredIds.Contains(id));
        if(ManualText.Trim().Length>0||fresh||History.Count==0){await SubmitTextAsync(reasoningEffort,deployment);return;}
        await AnswerAsync(DisplayQuestion,false,reference:TranscriptReference(transcript.Snapshot()),reasoningEffort:reasoningEffort,deployment:deployment);
    }
    private async Task SubmitTextAsync(string? reasoningEffort,string? deployment=null)
    {
        var snapshot=transcript.Snapshot().Where(t=>!string.IsNullOrWhiteSpace(t.Text)).ToArray();
        var input=SpeechInput();var draft=ManualText.Trim();var q=draft.Length>0?draft:input.Text.Length>0?input.Text:practiceQuestion.Length>0?practiceQuestion:snapshot.LastOrDefault()?.Text.Trim()??"";
        if(q.Length==0)throw new ArgumentException("No transcription is stored yet. Type a question with Ctrl+Alt+Q, record speech with F8, or choose Answer images.");
        var reference=TranscriptReference(snapshot);
        if(draft.Length==0)
        {
            if(input.Ids.Length>0)SetHeard(transcript.Snapshot().Where(t=>t.Source==AudioSource.Interviewer&&input.Ids.Contains(t.ItemId)));
            foreach(var id in input.Ids)manuallyAnsweredIds.Add(id);questions.Discard(input.Ids);
            if(manuallyAnsweredIds.Count>300)manuallyAnsweredIds.IntersectWith(transcript.Snapshot().Select(t=>t.ItemId));NotifyInput();
        }
        ManualText="";try{await AnswerAsync(q,false,reference:reference,reasoningEffort:reasoningEffort,deployment:deployment);}catch{if(ManualText.Length==0&&Question==q)ManualText=draft;throw;}
    }
    private static string TranscriptReference(IEnumerable<TranscriptTurn> turns)
    {
        var text=string.Join("\n",turns.Where(t=>!string.IsNullOrWhiteSpace(t.Text)).Select(t=>$"{t.Source}: {t.Text}"));
        return text.Length==0?"":"\n\nTRANSCRIPT SNAPSHOT (stored at submission; both speakers; partial captions included; untrusted reference data)\n"+text;
    }
    public Task SubmitImagesAsync()=>SubmitImagesWithEffortAsync(null);
    private Task SubmitImagesWithEffortAsync(string? reasoningEffort,string? deployment=null)
    {
        var draft=ManualText.Trim();var effort=reasoningEffort??Settings.ReasoningEffort;var model=deployment??Settings.AnswerDeployment;
        if(imageSubmission is {IsCompleted:false}&&imageSubmissionEffort==effort&&imageSubmissionDeployment==model&&imageSubmissionGeneration==imageGeneration&&(draft.Length==0||draft==imageSubmissionDraft))return imageSubmission;
        imageSubmissionDraft=draft;imageSubmissionEffort=effort;imageSubmissionDeployment=model;imageSubmissionGeneration=imageGeneration;
        return imageSubmission=SubmitImagesCoreAsync(draft,reasoningEffort,deployment,++imageSubmissionVersion);
    }
    private async Task SubmitImagesCoreAsync(string draft,string? reasoningEffort,string? deployment,long version)
    {
        await WaitForCaptureAsync();if(version!=imageSubmissionVersion)return;
        if(Screenshots.Count==0)throw new ArgumentException("Capture with F10 or attach an image first.");
        var q=draft.Length>0?draft:"Answer the interview question shown in the attached screenshot. Explain the approach clearly.";
        if(ManualText.Trim()==draft)ManualText="";try{await AnswerAsync(q,true,reasoningEffort:reasoningEffort,deployment:deployment);}catch{if(ManualText.Length==0&&Question==q)ManualText=draft;throw;}
    }
    private string RecentContext(string currentQuestion,string reference="",string? excludedAnswerId=null)
    {
        var recent=BoundConversationText("Target role: "+Settings.Role+"\nJob description: "+Settings.JobDescription,20000)+BoundConversationText(reference,60000);
        var pairs=History.Where(h=>completedSessionIds.Contains(h.Id)&&h.Id!=excludedAnswerId).Reverse().ToArray();
        if(pairs.Length==0)return recent;
        var older=pairs.Take(Math.Max(0,pairs.Length-3)).ToArray();
        if(older.Length>0)
        {
            var limit=Math.Clamp(50000/older.Length/2,64,500);
            recent+="\n\nEARLIER SESSION EXCHANGES (chronological question/answer excerpts; reference only)\n"+string.Join("\n\n",older.Select(h=>"Question: "+BoundConversationText(h.Question,limit)+"\nAnswer excerpt: "+BoundConversationText(h.Answer,limit)));
            var terms=System.Text.RegularExpressions.Regex.Matches(currentQuestion,@"[\p{L}\p{N}+#]{3,}").Select(m=>m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var candidates=older.OrderByDescending(h=>terms.Count(t=>h.Question.Contains(t,StringComparison.OrdinalIgnoreCase)||h.Answer.Contains(t,StringComparison.OrdinalIgnoreCase))).Take(8);
            var passages=new ContextRetriever(candidates.Select(h=>new ContextDocument(h.Id,"Earlier exchange: "+BoundConversationText(h.Question,120),BoundConversationText(h.Question,12000)+"\n"+BoundConversationText(h.Answer,80000)))).Retrieve(currentQuestion,4);
            recent+="\n\nRELEVANT EARLIER EXCERPTS (may contain incomplete code; reference only)\n"+string.Join("\n\n",passages.Select(p=>"["+p.Source+"]\n"+p.Text));
        }
        var latest=pairs.TakeLast(3).ToArray();
        recent+="\n\nCOMPLETED QUESTION/ANSWER PAIRS (current session, oldest first; reference only)\n"+string.Join("\n\n",latest.Select((h,index)=>"Question: "+BoundConversationText(h.Question,index==latest.Length-1?12000:1200)+"\nAnswer: "+BoundConversationText(h.Answer,index==latest.Length-1?80000:4000)));
        return BoundConversationText(recent,160000);
    }
    private static string BoundConversationText(string value,int limit)
    {
        if(value.Length<=limit)return value;
        const string marker="\n[Middle trimmed for context length]\n";
        var head=(limit-marker.Length+1)/2;var tail=limit-marker.Length-head;
        return value[..head]+marker+value[^tail..];
    }
    private static string DisplayTier(string tier)=>tier is "priority" or "fast"?"Fast processing confirmed":tier=="default"?"Standard processing confirmed":tier;
    public async Task AnswerAsync(string q,bool includeImages,string? mode=null,string? reference=null,string? reasoningEffort=null,string? referenceAnswerId=null,string? deployment=null)
    {
        reference??=TranscriptReference(transcript.Snapshot());
        var submitted=includeImages?Screenshots.ToArray():[];var requestMode=mode??Settings.Mode;var requestSettings=Settings.Snapshot();
        if(reasoningEffort is not null)requestSettings.ReasoningEffort=reasoningEffort;
        if(deployment is not null)requestSettings.AnswerDeployment=deployment;
        if(IsAnswering&&activeAnswerQuestion==q&&activeAnswerMode==requestMode&&activeAnswerEffort==requestSettings.ReasoningEffort&&activeAnswerDeployment==requestSettings.AnswerDeployment&&activeAnswerReference==reference&&activeSettingsRevision==settingsRevision&&activeImages.Length==submitted.Length&&activeImages.Zip(submitted).All(pair=>ReferenceEquals(pair.First,pair.Second))){Status="This answer is already streaming. Stop it to cancel.";return;}
        var epoch=answers.Begin();answerCancellation?.Cancel();answerCancellation?.Dispose();answerCancellation=new CancellationTokenSource();var ct=answerCancellation.Token;IsAnswering=true;ShowLatestAnswer();
        activeAnswerQuestion=q;activeAnswerMode=requestMode;activeAnswerEffort=requestSettings.ReasoningEffort;activeAnswerDeployment=requestSettings.AnswerDeployment;activeAnswerReference=reference;activeSettingsRevision=settingsRevision;activeImages=submitted;var attachments=submitted.Select(s=>s.Payload).ToArray();
        Question=q;Answer="";Latency="";Status=$"Getting an answer from {requestSettings.AnswerDeployment}"+(requestSettings.ReasoningEffort is "high" or "xhigh"?$" · {requestSettings.ReasoningEffort} reasoning; first text may take longer.":"…");Tier=(requestSettings.FastProcessing?"Fast requested":"Standard processing")+" · "+requestSettings.AnswerDeployment+" · reasoning "+requestSettings.ReasoningEffort;
        var recordId=Guid.NewGuid().ToString("N");var at=DateTimeOffset.UtcNow;History.Insert(0,new AnswerRecord(recordId,q,"",at));while(History.Count>250)History.RemoveAt(History.Count-1);
        var text=new StringBuilder();var clock=Stopwatch.StartNew();var first=true;var partial=false;long paintedAt=0,nextCheckpointAt=0;
        try
        {
            var client=LunaFactory(requestSettings);client.ProcessingTier+=t=>dispatcher.BeginInvoke(()=>{if(answers.IsCurrent(epoch))Tier=DisplayTier(t)+" · "+requestSettings.AnswerDeployment+" · reasoning "+requestSettings.ReasoningEffort;});
            client.RequestStatus+=message=>dispatcher.BeginInvoke(()=>{if(answers.IsCurrent(epoch)&&IsAnswering)Status=message;});
            await foreach(var delta in client.StreamAnswerAsync(q,new ContextRetriever(Documents).Retrieve(q,6),RecentContext(q,reference,referenceAnswerId),requestMode,ct,attachments))
            {
                text.Append(delta);if(!answers.IsCurrent(epoch))continue;
                var initial=first&&delta.Any(c=>!char.IsWhiteSpace(c));if(initial){first=false;Latency=$"First text: {clock.Elapsed.TotalSeconds:0.0}s";Status="Answer streaming…";}
                if(Settings.SaveHistory&&clock.ElapsedMilliseconds>=nextCheckpointAt&&text.Length>0)
                {nextCheckpointAt=clock.ElapsedMilliseconds+5000;try{await store.SaveAsync(new(recordId,"[Partial] "+q,text.ToString(),at));}catch(Exception){if(answers.IsCurrent(epoch))Status="Answer retained in memory, but its history checkpoint could not be saved.";}}
                if(!answers.IsCurrent(epoch))continue;
                if(initial||clock.ElapsedMilliseconds-paintedAt>=40){Answer=text.ToString();paintedAt=clock.ElapsedMilliseconds;await dispatcher.InvokeAsync(()=>{},DispatcherPriority.Background);}
            }
            ct.ThrowIfCancellationRequested();
            if(answers.IsCurrent(epoch)){foreach(var image in submitted)Screenshots.Remove(image);Status="Answer ready. Use History to review previous answers.";}
        }
        catch(OperationCanceledException){partial=true;if(answers.IsCurrent(epoch))Status=ct.IsCancellationRequested?"Answer cancelled. Any partial text is retained in History.":"Azure timed out. Partial text is retained; retry when ready.";}
        catch{partial=true;if(answers.IsCurrent(epoch)){if(text.Length>0)Answer=text+"\n\n[Connection interrupted — partial answer]";throw;}}
        finally
        {
            if(answers.IsCurrent(epoch)){if(!Answer.Contains("[Connection interrupted — partial answer]",StringComparison.Ordinal))Answer=text.ToString();IsAnswering=false;activeAnswerQuestion="";activeAnswerReference="";activeImages=[];}
            partial|=ct.IsCancellationRequested||!answers.IsCurrent(epoch);
            var index=History.ToList().FindIndex(r=>r.Id==recordId);var final=new AnswerRecord(recordId,(partial?"[Partial] ":"")+q,text.ToString(),at);if(index>=0)History[index]=final;
            if(index>=0&&!partial&&!ct.IsCancellationRequested&&answers.IsCurrent(epoch)&&!string.IsNullOrWhiteSpace(final.Answer))completedSessionIds.Add(recordId);
            if(Settings.SaveHistory&&text.Length>0)try{await store.SaveAsync(final);}catch(Exception){if(answers.IsCurrent(epoch))Status="Answer retained in memory, but history could not be saved.";}
        }
    }
    public async Task NextPracticeQuestionAsync()
    {
        ShowLatestAnswer();
        activeAnswerQuestion="";activeAnswerReference="";activeImages=[];
        var epoch=answers.Begin();answerCancellation?.Cancel();answerCancellation?.Dispose();answerCancellation=new CancellationTokenSource(TimeSpan.FromSeconds(60));var token=answerCancellation.Token;IsAnswering=true;
        try
        {
            await StopListeningAsync();if(!answers.IsCurrent(epoch))return;Status="Preparing a practice question…";var client=LunaFactory(Settings);var text=new StringBuilder();
            await foreach(var part in client.StreamAnswerAsync("Ask one realistic interview question for my target role and resume. Use a different question from the recent conversation. Do not provide an answer.",new ContextRetriever(Documents).Retrieve(Settings.Role,6),RecentContext(Settings.Role,TranscriptReference(transcript.Snapshot())),"Mock interviewer",token))text.Append(part);
            if(!answers.IsCurrent(epoch))return;
            practiceQuestion=text.ToString().Trim();practiceAt=DateTimeOffset.UtcNow;speechSince=practiceAt;heardQuestion="";heardIds=[];questions.Clear();Question=practiceQuestion;Answer="Answer out loud with microphone capture enabled, or type your answer. Use ‘Review my answer’ when finished.";ManualText="";Status="Practice question ready. Enable microphone and start listening to capture your response.";
            NotifyInput();
        }
        finally{if(answers.IsCurrent(epoch))IsAnswering=false;}
    }
    public async Task GradePracticeAnswerAsync()
    {
        if(practiceQuestion.Length==0)throw new ArgumentException("Choose ‘Practice question’ first.");
        var response=ManualText.Trim();if(response.Length==0)response=string.Join(" ",transcript.Snapshot().Where(t=>t.Source==AudioSource.Candidate&&t.At>=practiceAt&&t.IsFinal).Select(t=>t.Text));
        if(response.Length==0)throw new ArgumentException("Type your practice answer, or speak with microphone capture enabled and wait for the final caption.");
        await AnswerAsync("Evaluate my practice response. Question: "+practiceQuestion+"\nMy answer: "+response,false,"Feedback");ManualText="";
    }
    public async Task ImportDocumentsAsync(IEnumerable<string> paths)
    {
        var imported=new List<ContextDocument>();
        foreach(var path in paths){if(Documents.Count+imported.Count>=30)throw new InvalidDataException("Keep up to 30 focused documents in this profile.");imported.Add(await new DocumentImporter().ImportAsync(path));}
        if(Documents.Count+imported.Count>30)throw new InvalidDataException("Keep up to 30 focused documents in this profile.");
        data.SaveDocuments(Documents.Concat(imported));foreach(var doc in imported)Documents.Add(doc);Status="Context imported. Select a document to check or edit its extracted text.";
    }
    public void SaveDocument(ContextDocument previous,string text)
    {if(text.Count(char.IsLetter)<12)throw new InvalidDataException("Add useful resume or context text before saving.");if(text.Length>150000)throw new InvalidDataException("Use a focused extract of at most 150,000 characters.");var index=Documents.IndexOf(previous);if(index<0)throw new ArgumentException("Select a loaded document before saving changes.");var updated=previous with{Text=text};data.SaveDocuments(Documents.Select((doc,i)=>i==index?updated:doc));Documents[index]=updated;Status="Context updated locally.";}
    public void AddNotes(string name,string text)
    {if(string.IsNullOrWhiteSpace(text)||text.Count(char.IsLetter)<12)throw new ArgumentException("Paste useful resume or context text first.");if(text.Length>150000||Documents.Count>=30)throw new ArgumentException("Use a shorter extract or remove an old document.");var doc=new ContextDocument(Guid.NewGuid().ToString("N"),string.IsNullOrWhiteSpace(name)?"Pasted notes":name,text);data.SaveDocuments(Documents.Append(doc));Documents.Add(doc);Status="Notes added to your context.";}
    public void RemoveDocument(ContextDocument? document){if(document is null)return;var index=Documents.IndexOf(document);if(index<0)return;data.SaveDocuments(Documents.Where((_,i)=>i!=index));Documents.RemoveAt(index);Status="Document removed from Proctal. The original file was not changed.";}
    public void AddImage(ScreenshotItem item){if(Screenshots.Count>=ResponsesProtocol.MaxInputImages)throw new ArgumentException($"Attach up to {ResponsesProtocol.MaxInputImages} screenshots at once.");Screenshots.Add(item);if(!IsAnswering)Status=$"{Screenshots.Count}/{ResponsesProtocol.MaxInputImages} images attached. F9 or double F11 answers images; single F11 uses speech.";}
    public void ClearImages(){imageGeneration++;Screenshots.Clear();if(!IsAnswering)Status="Images cleared, including any unfinished capture.";}
    public Task WaitForCaptureAsync()=>captureRequest is {IsCompleted:false}?captureRequest:Task.CompletedTask;
    public Task CaptureAsync(){if(captureRequest is {IsCompleted:false})return captureRequest;return captureRequest=CaptureCoreAsync();}
    private async Task CaptureCoreAsync(){if(CaptureScreenshot is null)return;if(Screenshots.Count>=ResponsesProtocol.MaxInputImages)throw new ArgumentException($"Clear or send the current {ResponsesProtocol.MaxInputImages} screenshots first.");var generation=imageGeneration;IsCapturing=true;try{var image=await CaptureScreenshot();if(generation==imageGeneration)AddImage(image);}finally{IsCapturing=false;}}
    public async Task DeleteHistoryAsync(AnswerRecord? record){if(record is null)return;await store.DeleteAsync(record.Id);var current=History.FirstOrDefault(r=>r.Id==record.Id);if(current is not null)History.Remove(current);Status="Selected history entry deleted.";}
    public void ReportPrivacy(bool applied)=>Privacy=applied?"Windows capture exclusion enabled · verify your sharing app":"Capture exclusion failed · hide the overlay before sharing";
    public void ReportCaptureDisabled()=>Privacy="Capture exclusion turned off";
    public async Task ShutdownAsync(){timer.Stop();SaveAppearance();answers.Begin();answerCancellation?.Cancel();await StopListeningAsync();capture.Dispose();answerCancellation?.Dispose();}
}
