using System.Text.Json;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private MeshDocument? _studioMesh;
    private LibraryModel? _studioLibraryModel;
    private IReadOnlyList<LibraryModel>? _modelLibrary;
    private string? _simulationId;
    private SimulationResult? _simulationResult;
    private IReadOnlyDictionary<string,double>? _simulationParameters;
    private TextBlock? _modelState;
    private bool _studioModelReady;
    private int _modelRevision;
    private ComboBox? _modelChoices;

    private void AddModelControls(StackPanel panel)
    {
        var controls=new StackPanel{Spacing=8};
        controls.Children.Add(Label(AstraOS.Core.UiText.T("MODELLI 3D / ANALISI E REPLAY"),17));
        controls.Children.Add(Label(AstraOS.Core.UiText.T("Affianca al grafico una mesh reale. Il replay applica i risultati del solutore scelto: traiettoria, assetto, spostamento, deformazione 1D o colore del campo. La sola mesh non determina inerzie, materiali o un calcolo CFD/FEM."),12));
        try{_modelLibrary??=ModelLibrary.Load();}catch(Exception ex){controls.Children.Add(Label(AstraOS.Core.UiText.T("Libreria: ")+ex.Message,12));}
        var relevant=SimulationLab.For(_preferences.StudyDomain,_preferences.EngineeringTrack).Select(t=>t.Id).ToHashSet();
        _modelChoices=new ComboBox{Header=AstraOS.Core.UiText.T("Modelli pertinenti al percorso"),ItemsSource=(_modelLibrary??Array.Empty<LibraryModel>()).Where(m=>m.Tools.Any(relevant.Contains)).ToArray(),DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch,SelectedIndex=0};controls.Children.Add(_modelChoices);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        buttons.Children.Add(Action(AstraOS.Core.UiText.T("Carica modello della libreria"),async(_,_)=>{if(_modelChoices.SelectedItem is LibraryModel m)await LoadLibraryModelAsync(m);}));
        buttons.Children.Add(Action(AstraOS.Core.UiText.T("Importa il mio modello…"),async(_,_)=>
        {
            try{var picker=new FileOpenPicker();foreach(var ext in new[]{".glb",".gltf",".stl",".obj"})picker.FileTypeFilter.Add(ext);WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is null||_windowClosed)return;var info=new FileInfo(file.Path);if(info.Length>MeshImport.MaxBytes)throw new InvalidDataException(AstraOS.Core.UiText.T("Modello superiore a 64 MB."));var mesh=await Task.Run(async()=>MeshImport.Parse(file.Name,await File.ReadAllBytesAsync(file.Path,_windowLifetime.Token)),_windowLifetime.Token);if(_windowClosed)return;_studioMesh=mesh;_studioLibraryModel=null;await ShowModelAsync();}
            catch(Exception ex){if(_modelState is not null)_modelState.Text=AstraOS.Core.UiText.T("Importazione modello: ")+ex.Message;}
        }));
        controls.Children.Add(buttons);
        if(_preferences.StudyDomain=="engineering"&&_preferences.EngineeringTrack=="aerospace")
        {
            controls.Children.Add(Label(AstraOS.Core.UiText.T("Ariane 6 / Ariane 64 · quattro booster P120C. Ricostruzione didattica ASTRA da dimensioni pubbliche ESA/ArianeGroup; non un CAD ufficiale."),12));
            controls.Children.Add(Action(AstraOS.Core.UiText.T("Esplora Ariane 64 (ESA)"),async(_,_)=>await ExploreArianeAsync()));
        }
        controls.Children.Add(Label(AstraOS.Core.UiText.T("GLB con materiali e texture incorporati, glTF incorporato, STL e OBJ. Per OBJ i file MTL esterni non vengono caricati. Usa i controlli della vista per allineare gli assi, amplificare deformazioni, sezionare e ispezionare il modello."),12));
        _modelState=Label(_studioMesh is null?AstraOS.Core.UiText.T("Nessun modello caricato."):_studioMesh.Name,12);controls.Children.Add(_modelState);
        controls.Children.Add(Action(AstraOS.Core.UiText.T("Crediti e licenze dei modelli"),async(_,_)=>
        {
            try{var text=await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Graphics3D","web","models","CREDITS.md"));var body=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,IsTextSelectionEnabled=true};await new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=AstraOS.Core.UiText.T("Crediti modelli 3D"),Content=new ScrollViewer{Content=body,MaxHeight=460},CloseButtonText=AstraOS.Core.UiText.T("Chiudi")}.ShowAsync();}catch(Exception ex){_modelState.Text=ex.Message;}
        }));
        panel.Children.Add(new Expander{Header=AstraOS.Core.UiText.T("Modelli 3D reali · importa, analizza e riproduci"),Content=controls,HorizontalAlignment=HorizontalAlignment.Stretch});
    }
    private async Task ExploreArianeAsync()
    {
        _modelLibrary??=ModelLibrary.Load();if(_modelLibrary.FirstOrDefault(m=>m.Id=="ariane64") is {} rocket){_simulationId=null;_simulationResult=null;_simulationParameters=null;_studioScene=new ScientificScene("Ariane 6 / Ariane 64","X","Y","Z",new[]{new SceneSeries("Origine","points",new[]{new ScenePoint(0,0,0)})});await LoadLibraryModelAsync(rocket);_studioHost?.StartBringIntoView();}
    }
    private async Task LoadLibraryModelAsync(LibraryModel entry)
    {
        int revision=++_modelRevision;
        try{if(_modelState is not null)_modelState.Text=AstraOS.Core.UiText.T("Caricamento modello…");var mesh=await ModelLibrary.ReadAsync(entry,_windowLifetime.Token);if(_windowClosed||revision!=_modelRevision)return;_studioMesh=mesh;_studioLibraryModel=entry;await ShowModelAsync();}
        catch(Exception ex){if(_modelState is not null)_modelState.Text=AstraOS.Core.UiText.T("Modello: ")+ex.Message;StartupDiagnostics.Record("Model.load.failed",ex.ToString());}
    }
    private async Task ShowModelAsync()
    {
        if(_studioMesh is null)return;
        _studioScene??=new ScientificScene("Modello 3D","X","Y","Z",new[]{new SceneSeries("Origine","points",new[]{new ScenePoint(0,0,0)})});
        if(_modelState is not null)_modelState.Text=AstraOS.Core.UiText.F("{0} · {1:N0} vertici · {2:N0} triangoli\n", _studioMesh.Name, _studioMesh.Vertices, _studioMesh.Triangles)+(_studioLibraryModel is {} m?$"{m.Author} · {m.License}":AstraOS.Core.UiText.T("Modello importato dall'utente"));
        if(_studioHost is not null)_studioHost.Height=570;
        await StartStudioAsync();
    }
    private void SendStudioModel()
    {
        if(!_studioPageLoaded||_studioMesh is null||_studioView?.CoreWebView2 is null)return;
        _studioModelReady=false;
        _studioView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type="astra-model",model=_studioMesh.ToWebData(),credit=_studioLibraryModel is {} m?$"{m.Name} — {m.Author} · {m.License}":"Modello importato dall'utente",simulation=_simulationResult is {} result?new{tool=_simulationId,columns=result.Data.Columns,rows=result.Data.Rows,parameters=_simulationParameters,summary=result.Summary}:null,trajectory=_simulationResult is null?_studioScene?.Series.FirstOrDefault(s=>s.Kind=="line")?.Points.Select(p=>new[]{p.X,p.Y,p.Z}).ToArray():null}));
    }
    private async Task SetSimulationModelAsync(string tool,SimulationResult result,IReadOnlyDictionary<string,double> parameters)
    {
        _simulationId=tool;_simulationResult=result;_simulationParameters=parameters;
        if(_studioMesh is null||(_studioLibraryModel is not null&&!_studioLibraryModel.Tools.Contains(tool)))
        {
            _modelLibrary??=ModelLibrary.Load();var candidate=_modelLibrary.FirstOrDefault(m=>m.Tools.Contains(tool));if(candidate is not null){await LoadLibraryModelAsync(candidate);return;}
        }
        await ShowModelAsync();
    }
}
