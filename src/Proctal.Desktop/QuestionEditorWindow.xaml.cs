using System.Windows;
using System.Windows.Input;
using Proctal.Core;
using Proctal.Desktop.Platform;
namespace Proctal.Desktop;
public partial class QuestionEditorWindow:Window
{
    private App Host=>(App)Application.Current;
    private readonly ProctalController controller;
    public QuestionEditorWindow(ProctalController controller)
    {
        this.controller=controller;InitializeComponent();DataContext=controller;
        SourceInitialized+=(_,_)=>{WindowAppearance.Apply(this,true);Host.ApplyPrivacy();};
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){Hide();e.Handled=true;}};
        Closing+=(_,e)=>{if(!Host.IsExiting){e.Cancel=true;Hide();}};
    }
    public void ShowEditor(){Show();Activate();Editor.Focus();Keyboard.Focus(Editor);}
    private async void Submit(object sender,RoutedEventArgs e){Hide();Host.ShowOverlay();await controller.Safe(controller.SubmitAsync);}
    private async void Review(object sender,RoutedEventArgs e){Hide();Host.ShowOverlay();await controller.Safe(controller.GradePracticeAnswerAsync);}
    private void CloseEditor(object sender,RoutedEventArgs e)=>Hide();
}
