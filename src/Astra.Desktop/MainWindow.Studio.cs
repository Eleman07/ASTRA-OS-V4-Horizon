using System.Text.Json;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private ScientificScene? _studioScene;
    private Grid? _studioHost;
    private TextBlock? _studioState;
    private WebView2? _studioView;
    private int _studioGeneration;
    private bool _studioReady,_studioLoading,_studioPageLoaded;
    private string? _studioFile;
    private DateTime _studioFileTime;
    private readonly DispatcherTimer _studioWatch=new(){Interval=TimeSpan.FromSeconds(2)};
    private bool _studioWatching,_studioReading;
    private void InitializeStudio()
    {
        _studioWatch.Tick+=async(_,_)=>{if(!_studioWatching||_studioReading||!_windowActive||_v4LastPage!="stem"||_studioFile is null)return;try{if(File.GetLastWriteTimeUtc(_studioFile)!=_studioFileTime)await ReadStudioFileAsync(_studioFile);}catch(Exception ex){if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.T("Aggiornamento: ")+ex.Message;}};
        Closed+=(_,_)=>{_studioWatch.Stop();StopStudio();};
    }
    private void AddStudioPanel()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(AstraOS.Core.UiText.T("STUDIO STEM / PYTHON → 3D"),20));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Esplora traiettorie, nuvole di punti e superfici dei tuoi progetti. Trascina per ruotare, Maiusc+trascina per spostare, rotellina per zoom. Nessun ambiente Python viene eseguito automaticamente."),12));
        _studioState=Label(_studioScene?.Title??AstraOS.Core.UiText.T("Importa una scena oppure esegui una simulazione. Il renderer si attiva su richiesta."),12);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        actions.Children.Add(Action(AstraOS.Core.UiText.T("Importa scena JSON…"),async(_,_)=>{try{var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".json");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is not null&&!_windowClosed){await ReadStudioFileAsync(file.Path);await StartStudioAsync();}}catch(Exception ex){_studioState.Text=AstraOS.Core.UiText.T("Scena: ")+ex.Message;}}));
        actions.Children.Add(Action(AstraOS.Core.UiText.T("Attiva / aggiorna 3D"),async(_,_)=>await StartStudioAsync()));actions.Children.Add(Action(AstraOS.Core.UiText.T("Arresta 3D"),(_,_)=>{StopStudio();_studioState.Text=AstraOS.Core.UiText.T("Renderer rilasciato. I dati restano in memoria.");}));panel.Children.Add(actions);
        var toolkit=new StackPanel{Spacing=8};
        var samples=new ComboBox{Header=AstraOS.Core.UiText.T("Esempi calcolati · nessuna misura sperimentale"),ItemsSource=new[]{"Superficie matematica","Traiettorie balistiche ideali","Doppia elica illustrativa"},SelectedIndex=0};toolkit.Children.Add(samples);
        actions.Children.Add(Action(AstraOS.Core.UiText.T("Carica esempio"),async(_,_)=>{try{await ReadStudioFileAsync(Path.Combine(AppContext.BaseDirectory,"python-sdk","examples-data",new[]{"mathematics.json","physics.json","biology.json"}[samples.SelectedIndex]));await StartStudioAsync();}catch(Exception ex){_studioState.Text=ex.Message;}}));
        var watch=new ToggleSwitch{Header=AstraOS.Core.UiText.T("Aggiorna automaticamente il JSON importato"),IsOn=_studioWatching};watch.Toggled+=(_,_)=>{_studioWatching=watch.IsOn;if(watch.IsOn)_studioWatch.Start();else _studioWatch.Stop();};toolkit.Children.Add(watch);
        toolkit.Children.Add(Label(AstraOS.Core.UiText.T("Dal tuo script: from astra_scene import Scene; Scene('Progetto').line('Percorso', punti_xyz).save('scene.json'). Copia il toolkit nella cartella del progetto, importa scene.json qui e attiva l'aggiornamento per visualizzare i nuovi risultati."),12));
        toolkit.Children.Add(Action(AstraOS.Core.UiText.T("Copia toolkit Python in una cartella…"),async(_,_)=>
        {
            try{var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var dir=await picker.PickSingleFolderAsync();if(dir is null||_windowClosed)return;string target=Path.Combine(dir.Path,"astra-python-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(target);foreach(var name in new[]{"astra_scene.py","examples.py","README.md"})File.Copy(Path.Combine(AppContext.BaseDirectory,"python-sdk",name),Path.Combine(target,name),false);_studioState.Text=AstraOS.Core.UiText.T("Toolkit copiato: ")+target+AstraOS.Core.UiText.T(". Usa Python nel tuo editor o terminale.");}catch(Exception ex){_studioState.Text=AstraOS.Core.UiText.T("Toolkit: ")+ex.Message;}
        }));
        panel.Children.Add(new Expander{Header=AstraOS.Core.UiText.T("Progetti Python · esempi e aggiornamento"),Content=toolkit,HorizontalAlignment=HorizontalAlignment.Stretch});
        var map=new StackPanel{Spacing=8};var x=new ComboBox{Header=AstraOS.Core.UiText.T("Asse X")};var y=new ComboBox{Header=AstraOS.Core.UiText.T("Asse Y")};var z=new ComboBox{Header=AstraOS.Core.UiText.T("Asse Z")};var kind=new ComboBox{Header=AstraOS.Core.UiText.T("Geometria"),ItemsSource=new[]{"Punti","Linea"},SelectedIndex=0};
        void UpdateMap(){var cols=_dataset?.Columns??Array.Empty<string>();x.ItemsSource=new[]{"Indice riga"}.Concat(cols).ToArray();y.ItemsSource=cols;z.ItemsSource=new[]{"Piano Z=0"}.Concat(cols).ToArray();x.SelectedIndex=0;y.SelectedIndex=0;z.SelectedIndex=0;}
        map.Children.Add(Label(AstraOS.Core.UiText.T("Visualizza i dati del Data Lab / dell'ultima simulazione"),14));map.Children.Add(Action(AstraOS.Core.UiText.T("Aggiorna colonne del dataset"),(_,_)=>UpdateMap()));map.Children.Add(x);map.Children.Add(y);map.Children.Add(z);map.Children.Add(kind);map.Children.Add(Action(AstraOS.Core.UiText.T("Visualizza assi scelti"),async(_,_)=>{try{if(_dataset is null)throw new InvalidDataException(AstraOS.Core.UiText.T("Importa prima un dataset nel Data Lab oppure esegui una simulazione."));_simulationId=null;_simulationResult=null;_simulationParameters=null;_studioScene=ScientificScene.FromDataset(_dataset,x.SelectedIndex-1,y.SelectedIndex,z.SelectedIndex-1,kind.SelectedIndex==1?"line":"points");await StartStudioAsync();}catch(Exception ex){_studioState.Text=ex.Message;}}));UpdateMap();
        panel.Children.Add(new Expander{Header=AstraOS.Core.UiText.T("Dati tabellari → scena 3D"),Content=map,HorizontalAlignment=HorizontalAlignment.Stretch});
        AddModelControls(panel);
        panel.Children.Add(_studioState);_studioHost=new Grid{Height=_studioMesh is null?390:570,Background=Token("AstraNavy")};panel.Children.Add(_studioHost);WorkspacePanel.Children.Add(Card(panel));
    }
    private async Task ReadStudioFileAsync(string path)
    {
        if(_studioReading)return;_studioReading=true;
        try
        {
            var info=new FileInfo(path);if(!info.Exists||info.Length>8_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Scena assente o superiore a 8 MB."));var stamp=info.LastWriteTimeUtc;
            var scene=await Task.Run(async()=>ScientificScene.Parse(await File.ReadAllTextAsync(path,_windowLifetime.Token)),_windowLifetime.Token);if(_windowClosed)return;
            _simulationId=null;_simulationResult=null;_simulationParameters=null;_studioScene=scene;_studioFile=path;_studioFileTime=stamp;if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.F("{0} · {1} punti · {2}", scene.Title, scene.Series.Sum(s=>s.Points.Count), path);SendStudioScene();
        }
        finally{_studioReading=false;}
    }
    private async Task StartStudioAsync()
    {
        if(_studioScene is null){if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.T("Importa una scena, un dataset o esegui una simulazione.");return;}
        if(_studioView?.CoreWebView2 is not null&&_studioPageLoaded){SendStudioScene();SendStudioModel();return;}if(_studioLoading||_studioHost is null)return;
        int generation=++_studioGeneration;_studioLoading=true;
        try
        {
            var view=new WebView2();_studioView=view;_studioHost.Children.Add(view);
            var env=await CoreWebView2Environment.CreateWithOptionsAsync(null,Path.Combine(AstraPaths.UserDataRoot,"WebView2"),null);if(generation!=_studioGeneration||_windowClosed)return;
            await view.EnsureCoreWebView2Async(env);if(generation!=_studioGeneration||_windowClosed||_v4LastPage!="stem")return;
            var core=view.CoreWebView2;core.Settings.AreDevToolsEnabled=false;core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreHostObjectsAllowed=false;
            core.SetVirtualHostNameToFolderMapping("astra.local",Path.Combine(AppContext.BaseDirectory,"Graphics3D","web"),CoreWebView2HostResourceAccessKind.DenyCors);
            view.NavigationStarting+=(_,e)=>{if(e.Uri!="https://astra.local/stem.html"&&e.Uri!="about:blank")e.Cancel=true;};
            core.NewWindowRequested+=(_,e)=>e.Handled=true;
            core.NavigationCompleted+=(_,e)=>{if(e.IsSuccess&&generation==_studioGeneration){_studioPageLoaded=true;SendStudioScene();SendStudioModel();SendRenderActivity();}else if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.T("Scena non caricata: ")+e.WebErrorStatus;};
            core.WebMessageReceived+=(_,e)=>{if(e.Source!="https://astra.local/stem.html"||e.WebMessageAsJson.Length>1024)return;try{using var d=JsonDocument.Parse(e.WebMessageAsJson);var r=d.RootElement;if(r.ValueKind!=JsonValueKind.Object)return;if(r.TryGetProperty("type",out var mt)&&mt.GetString()=="astra-model-ready"){_studioModelReady=r.GetProperty("ready").GetBoolean();if(_modelState is not null)_modelState.Text=r.TryGetProperty("message",out var msg)?msg.GetString():AstraOS.Core.UiText.T("Modello pronto");return;}if(r.TryGetProperty("type",out var t)&&t.GetString()=="astra-stem-ready"){_studioReady=r.GetProperty("ready").GetBoolean();if(_studioState is not null)_studioState.Text=_studioReady?_studioScene!.Title+AstraOS.Core.UiText.T(" · Vista 3D pronta · ")+_studioScene.Series.Sum(s=>s.Points.Count)+AstraOS.Core.UiText.T(" punti"):AstraOS.Core.UiText.T("WebGL non disponibile. I dati restano esportabili.");}}catch(JsonException){}};
            core.ProcessFailed+=(_,e)=>{StartupDiagnostics.Record("Studio.process.failed",e.ProcessFailedKind.ToString());StopStudio();if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.T("Renderer terminato. Puoi riattivarlo.");};
            core.Navigate("https://astra.local/stem.html");
        }
        catch(Exception ex){StopStudio();if(_studioState is not null)_studioState.Text=AstraOS.Core.UiText.T("3D: ")+ex.Message;StartupDiagnostics.Record("Studio.failed",ex.ToString());}
        finally{if(generation==_studioGeneration)_studioLoading=false;}
    }
    private void SendStudioScene()
    {
        if(!_studioPageLoaded||_studioScene is null||_studioView?.CoreWebView2 is null)return;
        _studioReady=false;
        var p=_preferences.Palette;_studioView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{type="astra-scene",locale=UiText.Language,strings=UiText.WebCatalog(),scene=_studioScene.ToWebData(),palette=new{navy=p.Navy,deep=p.Deep,ice=p.Ice,blue=p.Blue,snow=p.Snow},fps=PerformancePolicy.EffectiveOrbitFps(_preferences),motion=false,quality=_preferences.EcoMode?"low":"balanced"}));
    }
    private void SendRenderActivity()
    {
        var message=JsonSerializer.Serialize(new{type="astra-activity",active=_windowActive});try{_studioView?.CoreWebView2?.PostWebMessageAsJson(message);_orbitView?.CoreWebView2?.PostWebMessageAsJson(message);}catch(Exception){}
    }
    private void StopStudio()
    {
        ++_studioGeneration;_studioReady=false;_studioModelReady=false;_studioLoading=false;_studioPageLoaded=false;
        if(_studioView is not null){try{_studioView.Close();}catch(Exception){}_studioHost?.Children.Remove(_studioView);_studioView=null;}
    }
}
