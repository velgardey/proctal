using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Proctal.Core;
using Xunit;
namespace Proctal.Tests;
public sealed class SettingsReliabilityTests
{
    [Fact] public Task DailyAudioTogglesPersistWithoutSavingIncompleteSetupDrafts()=>ControllerTests.OnDispatcher(controller=>
    {
        controller.SaveSettings("");controller.Settings.Endpoint="incomplete draft";
        controller.Settings.AutoAnswer=false;controller.Settings.IncludeMicrophone=true;
        var saved=AzureSettings.Load(Path.Combine(controller.DataPath,"settings.json"));
        Assert.False(saved.AutoAnswer);Assert.True(saved.IncludeMicrophone);Assert.StartsWith("https://",saved.Endpoint);
        return Task.CompletedTask;
    });
    [Fact] public void EverySettingNotifiesOpenViewsAndSnapshotsDoNotShareSubscribers()
    {
        var settings=new AzureSettings();var observable=Assert.IsAssignableFrom<INotifyPropertyChanged>(settings);
        var changed=new List<string>();observable.PropertyChanged+=(_,e)=>changed.Add(e.PropertyName!);
        foreach(var property in typeof(AzureSettings).GetProperties().Where(p=>p.CanWrite))
        {
            var previous=property.GetValue(settings);
            object value=property.PropertyType==typeof(bool)?!(bool)previous!:property.PropertyType==typeof(int)?2800:property.PropertyType==typeof(double)?.73:
                property.Name=="ResponseStructure"?"Problem → brute force → optimize":property.Name=="Endpoint"?"https://practice.openai.azure.com/":property.Name=="ReasoningEffort"?"low":property.Name=="Mode"?"Technical":"Test "+property.Name;
            changed.Clear();property.SetValue(settings,value);Assert.Contains(property.Name,changed);
        }
        var snapshot=settings.Snapshot();changed.Clear();snapshot.Instructions="Snapshot only";Assert.Empty(changed);Assert.NotEqual(settings.Instructions,snapshot.Instructions);
    }
    [Fact] public void AllOptionsRoundTripAndOldCodeFirstDefaultsRemainCompatible()
    {
        var settings=new AzureSettings{Endpoint="https://practice.openai.azure.com/",AnswerDeployment="answer",TranscriptionDeployment="transcribe",Language="en",OutputDeviceId="output",InputDeviceId="input",IncludeMicrophone=true,AutoAnswer=false,SaveHistory=true,CaptureExclusion=false,FastProcessing=false,ReasoningEffort="low",Opacity=.73,Role="Engineer",JobDescription="Backend systems",Instructions="Use concise headings",Mode="Technical",CodeFirst=false,ResponseStructure="Problem → brute force → optimize",CodingLanguage="C#"};
        var folder=Path.Combine(Path.GetTempPath(),"proctal-all-settings-"+Guid.NewGuid().ToString("N"));var path=Path.Combine(folder,"settings.json");
        try{settings.Save(path);Assert.Equal(JsonSerializer.Serialize(settings),JsonSerializer.Serialize(AzureSettings.Load(path)));}
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Assert.Equal("Insight → solution",JsonSerializer.Deserialize<AzureSettings>("{\"CodeFirst\":false}")!.ResponseStructure);
    }
    [Fact] public Task SelectedAudioDevicesNotifyEveryView()=>ControllerTests.OnDispatcher(controller=>
    {
        controller.SelectedOutput=new AudioDevice("test-output","Synthetic output");controller.SelectedInput=new AudioDevice("test-input","Synthetic input");
        var changed=new List<string>();controller.PropertyChanged+=(_,e)=>changed.Add(e.PropertyName!);
        controller.SelectedOutput=null;controller.SelectedInput=null;
        Assert.Contains(nameof(controller.SelectedOutput),changed);Assert.Contains(nameof(controller.SelectedInput),changed);
        changed.Clear();controller.SelectedOutput=null;controller.SelectedInput=null;Assert.Empty(changed);
        return Task.CompletedTask;
    });
}
