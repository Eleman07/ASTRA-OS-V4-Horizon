using AstraOS.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly WindowWorkspace _windowWorkspace=new();
    private readonly WorkspaceSessionStore _sessionStore=new();
    private List<WorkspaceSession> _sessions=new();
    private ListView? _windowList,_sessionApps;
    private ComboBox? _windowLayout,_sessionChoose;
    private TextBox? _sessionName;
    private TextBlock? _multitaskStatus;
    private readonly List<string> _sessionFolders=new();
    private bool _restoreFullscreen;
    private void InitializeMultitasking()
    {
        try{_sessions=_sessionStore.Load().ToList();}catch(Exception ex){StatusText.Text=ex.Message;}
        var p=new StackPanel{Spacing=10};p.Children.Add(Label(AstraOS.Core.UiText.T("MULTITASKING / FINESTRE E SESSIONI"),21));p.Children.Add(Label(AstraOS.Core.UiText.T("Seleziona le finestre da affiancare e un layout. Puoi ripristinare la disposizione precedente. Le sessioni salvano profilo, percorso STEM, app e cartelle di progetto."),12));
        _windowLayout=new ComboBox{Header=AstraOS.Core.UiText.T("Layout sul monitor di ASTRA"),ItemsSource=new[]{"Due colonne","Tre colonne","Griglia"},SelectedIndex=0};p.Children.Add(_windowLayout);_windowList=new ListView{SelectionMode=ListViewSelectionMode.Multiple,DisplayMemberPath="Display",MaxHeight=320};p.Children.Add(_windowList);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};actions.Children.Add(Action(AstraOS.Core.UiText.T("Aggiorna finestre aperte"),(_,_)=>RefreshWindowList()));actions.Children.Add(Action(AstraOS.Core.UiText.T("Affianca selezionate"),(_,_)=>TileSelectedWindows()));actions.Children.Add(Action(AstraOS.Core.UiText.T("Ripristina disposizione"),(_,_)=>{_windowWorkspace.Restore();if(_restoreFullscreen)ApplyFullScreen(true);_restoreFullscreen=false;}));actions.Children.Add(Action(AstraOS.Core.UiText.T("Attiva finestra"),(_,_)=>{if(_windowList.SelectedItem is WorkspaceWindow w)WindowWorkspace.Activate(w);}));p.Children.Add(actions);
        _multitaskStatus=Label(AstraOS.Core.UiText.T("Seleziona da una a otto finestre. La barra Windows resta disponibile durante il multitasking."),12);p.Children.Add(_multitaskStatus);MultitaskPanel.Children.Add(Card(p));
        var sessions=new StackPanel{Spacing=10};sessions.Children.Add(Label(AstraOS.Core.UiText.T("LE TUE SESSIONI DI PROFILO"),18));_sessionChoose=new ComboBox{Header=AstraOS.Core.UiText.T("Sessione salvata"),DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};sessions.Children.Add(_sessionChoose);
        sessions.Children.Add(Action(AstraOS.Core.UiText.T("Riprendi sessione"),async(_,_)=>{if(_sessionChoose.SelectedItem is WorkspaceSession session)await ResumeSessionAsync(session);}));
        sessions.Children.Add(Action(AstraOS.Core.UiText.T("Rimuovi sessione selezionata"),(_,_)=>{if(_sessionChoose.SelectedItem is WorkspaceSession s){_sessions.RemoveAll(x=>x.Id==s.Id);_sessionStore.Save(_sessions);UpdateSessionChoices();}}));
        _sessionName=new TextBox{Header=AstraOS.Core.UiText.T("Nome della nuova sessione"),MaxLength=80,PlaceholderText=AstraOS.Core.UiText.T("Es. laboratorio, esame, progetto, montaggio…")};sessions.Children.Add(_sessionName);
        sessions.Children.Add(Label(AstraOS.Core.UiText.T("La nuova sessione usa il profilo e il percorso STEM selezionati ora. Scegli fino a sei app installate e sei cartelle."),12));
        sessions.Children.Add(Action(AstraOS.Core.UiText.T("Rileva app per la sessione"),async(_,_)=>{await EnsureAppsAsync(true);_sessionApps!.ItemsSource=_installedApps;}));_sessionApps=new ListView{SelectionMode=ListViewSelectionMode.Multiple,DisplayMemberPath="Name",MaxHeight=230};sessions.Children.Add(_sessionApps);
        var folders=Label(AstraOS.Core.UiText.T("Nessuna cartella selezionata."),12);sessions.Children.Add(Action(AstraOS.Core.UiText.T("Aggiungi cartella di progetto…"),async(_,_)=>{var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var folder=await picker.PickSingleFolderAsync();if(folder is not null&&_sessionFolders.Count<6&&!_sessionFolders.Contains(folder.Path,StringComparer.OrdinalIgnoreCase)){_sessionFolders.Add(folder.Path);folders.Text=string.Join("\n",_sessionFolders);}}));sessions.Children.Add(folders);
        sessions.Children.Add(Action(AstraOS.Core.UiText.T("Salva sessione del profilo"),(_,_)=>
        {
            try{var session=new WorkspaceSession(Guid.NewGuid().ToString("N"),_sessionName.Text.Trim(),_workspace,_preferences.StudyDomain,_preferences.EngineeringTrack,new[]{"two","three","grid"}[_windowLayout.SelectedIndex],_sessionApps.SelectedItems.OfType<InstalledApplication>().Select(a=>a.Id).ToArray(),_sessionFolders.ToArray());WorkspaceSessionStore.Validate(_sessions.Append(session).ToArray());_sessions.Add(session);_sessionStore.Save(_sessions);UpdateSessionChoices();_multitaskStatus.Text=AstraOS.Core.UiText.T("Sessione salvata.");}catch(Exception ex){_multitaskStatus.Text=ex.Message;}
        }));MultitaskPanel.Children.Add(Card(sessions));UpdateSessionChoices();
    }
    private void RefreshWindowList(){if(_windowList is not null)_windowList.ItemsSource=WindowWorkspace.List();}
    private void UpdateSessionChoices(){if(_sessionChoose is null)return;_sessionChoose.ItemsSource=_sessions.ToArray();_sessionChoose.SelectedIndex=0;}
    private void TileSelectedWindows()
    {
        try
        {
            var selected=_windowList!.SelectedItems.OfType<WorkspaceWindow>().ToArray();if(selected.Length is <1 or >8)throw new InvalidOperationException(AstraOS.Core.UiText.T("Seleziona da una a otto finestre."));
            _restoreFullscreen=AppWindow.Presenter.Kind==AppWindowPresenterKind.FullScreen;if(_restoreFullscreen)ApplyFullScreen(false);var display=DisplayArea.GetFromWindowId(AppWindow.Id,DisplayAreaFallback.Primary).WorkArea;
            _windowWorkspace.Tile(selected,MultitaskLayouts.Arrange(selected.Length,display.X,display.Y,display.Width,display.Height,new[]{"two","three","grid"}[_windowLayout!.SelectedIndex]));_multitaskStatus!.Text=AstraOS.Core.UiText.T("Finestre affiancate. Ripristina disposizione annulla il layout.");
        }catch(Exception ex){_multitaskStatus!.Text=ex.Message;}
    }
    private async Task ResumeSessionAsync(WorkspaceSession session)
    {
        _windowLayout!.SelectedIndex=Array.IndexOf(new[]{"two","three","grid"},session.Layout);_preferences.StudyDomain=session.StudyDomain;_preferences.EngineeringTrack=session.EngineeringTrack;_preferenceStore.Save(_preferences);_studyDomainBox!.SelectedValue=session.StudyDomain;_studyTrackBox!.SelectedValue=session.EngineeringTrack;
        await EnsureAppsAsync();int launched=0;foreach(var id in session.Apps){var app=_installedApps.FirstOrDefault(a=>a.Id==id);if(app is not null){LaunchInstalledApp(app);launched++;}}
        foreach(var folder in session.Folders.Where(Directory.Exists))OpenWindowsExplorer(folder);
        Navigate(session.Workspace);StatusText.Text=AstraOS.Core.UiText.F("Sessione {0}: {1} app richieste. Usa Multitasking per affiancare le finestre già aperte.", session.Name, launched);
    }
    private void MultitaskNav_Click(object sender,RoutedEventArgs e)=>Navigate("multitasking");
}
