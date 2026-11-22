using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
public partial class DashboardWindow:Window
{
    private readonly ProctalController controller;
    private App Host=>(App)Application.Current;
    public DashboardWindow(ProctalController controller)
    {
        this.controller=controller;InitializeComponent();DataContext=controller;
        Height=Math.Min(Height,SystemParameters.WorkArea.Height-40);
        SourceInitialized+=(_,_)=>{WindowAppearance.Apply(this,true);Host.ApplyPrivacy();};
        Closing+=(_,e)=>{if(!Host.IsExiting){e.Cancel=true;Hide();controller.ReportStatus("Setup hidden. Use the Proctal tray icon to reopen it.");}};
    }
    private async void ShowOverlay(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{controller.SaveSettings(ApiKey.Password);ApiKey.Clear();Host.LaunchOverlay();return Task.CompletedTask;});
    private void MinimizeWindow(object sender,RoutedEventArgs e)=>SystemCommands.MinimizeWindow(this);
    private void MaximizeWindow(object sender,RoutedEventArgs e){if(WindowState==WindowState.Maximized)SystemCommands.RestoreWindow(this);else SystemCommands.MaximizeWindow(this);}
    private void HideWorkspace(object sender,RoutedEventArgs e)=>Close();
    private void GoConnection(object sender,RoutedEventArgs e){Workspace.SelectedIndex=2;SettingsPages.SelectedIndex=0;}
    private void GoContext(object sender,RoutedEventArgs e)=>Workspace.SelectedIndex=1;
    private void GoAnswers(object sender,RoutedEventArgs e){Workspace.SelectedIndex=2;SettingsPages.SelectedIndex=1;}
    private void CompactPreset(object sender,RoutedEventArgs e)=>Host.Overlay.SetPreset("Compact");
    private void StandardPreset(object sender,RoutedEventArgs e)=>Host.Overlay.SetPreset("Standard");
    private void ReadingPreset(object sender,RoutedEventArgs e)=>Host.Overlay.SetPreset("Reading");
    private async void ExitApp(object sender,RoutedEventArgs e)=>await Host.ExitAsync();
    private async void ToggleListening(object sender,RoutedEventArgs e)=>await controller.Safe(controller.ToggleListeningAsync);
    private void RefreshDevices(object sender,RoutedEventArgs e)=>controller.RefreshDevices();
    private async void PracticeQuestion(object sender,RoutedEventArgs e){await controller.Safe(controller.NextPracticeQuestionAsync);Host.ShowOverlay();}
    private async void GradeAnswer(object sender,RoutedEventArgs e){await controller.Safe(controller.GradePracticeAnswerAsync);Host.ShowOverlay();}
    private async void ImportDocuments(object sender,RoutedEventArgs e)
    {
        controller.DialogOpen=true;
        try{var dialog=new OpenFileDialog{Title="Add résumé or context",Filter="Supported documents|*.pdf;*.docx;*.txt;*.md",Multiselect=true};if(dialog.ShowDialog(this)==true)await controller.Safe(()=>controller.ImportDocumentsAsync(dialog.FileNames));}
        finally{controller.DialogOpen=false;}
    }
    private void SelectDocument(object sender,SelectionChangedEventArgs e){if(DocumentList.SelectedItem is ContextDocument doc){DocumentName.Text=doc.Name;DocumentText.Text=doc.Text;}}
    private async void SaveDocument(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{if(DocumentList.SelectedItem is ContextDocument doc)controller.SaveDocument(doc,DocumentText.Text);else throw new ArgumentException("Select a document first, or add the text as new notes.");return Task.CompletedTask;});
    private async void AddNotes(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{controller.AddNotes(DocumentName.Text,DocumentText.Text);return Task.CompletedTask;});
    private async void RemoveDocument(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{controller.RemoveDocument(DocumentList.SelectedItem as ContextDocument);DocumentText.Clear();return Task.CompletedTask;});
    private async void SaveSettings(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{controller.SaveSettings(ApiKey.Password);ApiKey.Clear();Host.ApplyPrivacy();Host.ApplyOpacity();return Task.CompletedTask;});
    private async void CheckConnection(object sender,RoutedEventArgs e)
    {
        if(!TestConnectionControl.IsEnabled)return;
        TestConnectionControl.IsEnabled=TestSolConnectionControl.IsEnabled=ConnectionFields.IsEnabled=RemoveKeyControl.IsEnabled=SaveSettingsControl.IsEnabled=LaunchOverlayControl.IsEnabled=false;
        var sol=ReferenceEquals(sender,TestSolConnectionControl);var button=sol?TestSolConnectionControl:TestConnectionControl;button.Content="Testing…";
        try{await controller.Safe(async()=>{controller.SaveSettings(ApiKey.Password);ApiKey.Clear();Host.ApplyPrivacy();if(sol)await controller.CheckSolConnectionAsync();else await controller.CheckConnectionAsync();});}
        finally
        {
            button.Content=sol?"Test Sol":"Test connection";
            TestConnectionControl.IsEnabled=TestSolConnectionControl.IsEnabled=ConnectionFields.IsEnabled=RemoveKeyControl.IsEnabled=SaveSettingsControl.IsEnabled=LaunchOverlayControl.IsEnabled=true;
        }
    }
    private async void RemoveKey(object sender,RoutedEventArgs e)=>await controller.Safe(()=>{controller.DeleteStoredKey();ApiKey.Clear();return Task.CompletedTask;});
    private void SelectHistory(object sender,SelectionChangedEventArgs e){if(HistoryList.SelectedItem is AnswerRecord record)HistoryText.Text="## "+record.Question+"\n\n"+record.Answer;}
    private async void DeleteHistory(object sender,RoutedEventArgs e)=>await controller.Safe(()=>controller.DeleteHistoryAsync(HistoryList.SelectedItem as AnswerRecord));
}
