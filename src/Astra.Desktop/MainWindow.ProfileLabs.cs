using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private bool _gamingRecording;
    private readonly List<GamingSample> _gamingSamples=new();
    private GamingSummary? _gamingPrevious,_gamingCurrent;
    private TextBlock? _gamingLabStatus,_creativeState;
    private SessionHudWindow? _sessionHud;
    private List<CreativeAsset> _creativeAssets=new();
    private StackPanel? _creativeList;
    private string? _subtitleSource;
    private TextBlock? _subtitleState;
    private NumberBox? _subtitleOffset,_subtitleScale;
    private TextBox? _gamingGameName,_gamingScenario,_gamingSettings;
    private string _sessionGameName="",_sessionScenario="",_sessionSettings="";
    private readonly List<GamingSessionRecord> _gamingHistory=new();
    private ComboBox? _creatorDeliveryTarget;
    private TextBlock? _creatorPreflightState;
    private static string GamingSummaryPath=>Path.Combine(AstraPaths.UserDataRoot,"gaming","last-session.json");
    private static string GamingHistoryPath=>Path.Combine(AstraPaths.UserDataRoot,"gaming","profiles.dpapi");
    private sealed record GamingSessionRecord(string Game,string Scenario,string Settings,GamingSummary Summary);
    private void InitializeProfileLabs()
    {
        try
        {
            if(File.Exists(GamingHistoryPath)&&new FileInfo(GamingHistoryPath).Length<2_000_000)
            {var bytes=ProtectedData.Unprotect(File.ReadAllBytes(GamingHistoryPath),null,DataProtectionScope.CurrentUser);try{_gamingHistory.AddRange(JsonSerializer.Deserialize<List<GamingSessionRecord>>(bytes)??new());}finally{CryptographicOperations.ZeroMemory(bytes);}}
            if(_gamingHistory.Count>50)_gamingHistory.RemoveRange(0,_gamingHistory.Count-50);
            _creativeAssets=CreatorMedia.Load();
        }
        catch(Exception ex){StartupDiagnostics.Record("ProfileLabs.load",ex.Message);}
        Closed+=(_,_)=>{if(_gamingRecording)StopGamingRecording();_sessionHud?.Close();};
    }
    private void AddGamingLab()
    {
        WorkspacePanel.Children.Add(Card(BuildGamingStrategist()));
        var p=new StackPanel{Spacing=9};p.Children.Add(Label(AstraOS.Core.UiText.T("GAME SESSION LAB / MISURA E CONFRONTA"),19));
        p.Children.Add(Label(AstraOS.Core.UiText.T("Registra CPU di sistema, motore GPU più occupato e RAM durante il gioco. La registrazione continua quando ASTRA è in secondo piano: campioni ogni 5 s, oppure 15 s in Eco. Non misura FPS, latenza, temperature o frame time e non modifica il gioco."),12));
        _gamingGameName=new TextBox{Header=UiText.T("Gioco / applicazione"),PlaceholderText="Es. Kerbal Space Program 2",MaxLength=120};
        _gamingScenario=new TextBox{Header=UiText.T("Scena o benchmark ripetibile"),PlaceholderText="Es. stesso salvataggio, stessa rotta",MaxLength=160};
        _gamingSettings=new TextBox{Header=UiText.T("Risoluzione e preset grafico"),PlaceholderText="Es. 2560×1440 · Alto",MaxLength=120};
        p.Children.Add(_gamingGameName);p.Children.Add(_gamingScenario);p.Children.Add(_gamingSettings);
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=7};row.Children.Add(Action(AstraOS.Core.UiText.T("Registra prestazioni"),async(_,_)=>{StartGamingRecording();await RefreshLiveMetricsAsync(true);}));row.Children.Add(Action(AstraOS.Core.UiText.T("Termina e confronta"),(_,_)=>StopGamingRecording()));row.Children.Add(Action(AstraOS.Core.UiText.T("Apri Session HUD"),(_,_)=>{if(_sessionHud is null){_sessionHud=new SessionHudWindow(GamingStatus,_preferences.Palette);_sessionHud.Closed+=(_,_)=>_sessionHud=null;}_sessionHud.Activate();}));p.Children.Add(row);
        _gamingLabStatus=Label(GamingStatus(),13);p.Children.Add(_gamingLabStatus);
        p.Children.Add(Action(AstraOS.Core.UiText.T("Esporta telemetria CSV…"),async(_,_)=>{try{if(_gamingSamples.Count==0)throw new InvalidOperationException(AstraOS.Core.UiText.T("Non ci sono campioni della sessione."));await SaveLabFileAsync("astra-gaming-session",".csv",GamingAnalysis.Csv(_gamingSamples));}catch(Exception ex){_gamingLabStatus.Text=ex.Message;}}));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Chiedi un'analisi all'assistente"),(_,_)=>{OpenAssistant("Gaming · analisi della sessione");_assistantWindow?.SetInput("Analizza queste misure reali, senza dedurre FPS o cause non misurate. Suggerisci un confronto ripetibile.\n"+GamingStatus()+"\n"+GamingAnalysis.Csv(_gamingSamples.TakeLast(300)));}));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Impostazioni modalità gioco Windows"),(_,_)=>OpenSystemSettings("ms-settings:gaming-gamemode")));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Multitasking e sessioni del profilo"),(_,_)=>Navigate("multitasking")));WorkspacePanel.Children.Add(Card(p));
    }
    private void StartGamingRecording()
    {
        if(_gamingRecording)return;
        _sessionGameName=string.IsNullOrWhiteSpace(_gamingGameName?.Text)?UiText.T("Sessione di prova senza nome"):_gamingGameName.Text.Trim();
        _sessionScenario=string.IsNullOrWhiteSpace(_gamingScenario?.Text)?UiText.T("Sessione generale"):_gamingScenario.Text.Trim();
        _sessionSettings=string.IsNullOrWhiteSpace(_gamingSettings?.Text)?UiText.T("Impostazioni non specificate"):_gamingSettings.Text.Trim();
        string key=GamingProfileKey(_sessionGameName,_sessionScenario,_sessionSettings);
        _gamingPrevious=_gamingHistory.LastOrDefault(x=>GamingProfileKey(x.Game,x.Scenario,x.Settings)==key)?.Summary;
        _gamingSamples.Clear();_gamingCurrent=null;_gamingRecording=true;_sessionStarted=DateTimeOffset.Now;
        if(_gamingLabStatus is not null)_gamingLabStatus.Text=GamingStatus();
    }
    private void RecordGamingSample(LiveSystemSample sample)
    {
        if(!_gamingRecording)return;_gamingSamples.Add(new(sample.Time,sample.CpuPercent,sample.GpuPercent,sample.RamPercent));if(_gamingSamples.Count>=12000)StopGamingRecording();if(_gamingLabStatus is not null)_gamingLabStatus.Text=GamingStatus();
    }
    private void StopGamingRecording()
    {
        if(!_gamingRecording)return;_gamingRecording=false;_sessionStarted=null;if(!_windowActive)_monitorTimer.Stop();if(_gamingSamples.Count==0)return;_gamingCurrent=GamingAnalysis.Summarize(_gamingSamples);
        try{Directory.CreateDirectory(Path.GetDirectoryName(GamingSummaryPath)!);string tmp=GamingSummaryPath+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(_gamingCurrent));File.Move(tmp,GamingSummaryPath,true);}catch(Exception ex){StartupDiagnostics.Record("Gaming.save",ex.Message);}
        try
        {
            _gamingHistory.Add(new GamingSessionRecord(_sessionGameName,_sessionScenario,_sessionSettings,_gamingCurrent));
            if(_gamingHistory.Count>50)_gamingHistory.RemoveRange(0,_gamingHistory.Count-50);
            byte[] clear=JsonSerializer.SerializeToUtf8Bytes(_gamingHistory);try{Directory.CreateDirectory(Path.GetDirectoryName(GamingHistoryPath)!);string temp=GamingHistoryPath+"."+Guid.NewGuid().ToString("N")+".tmp";byte[] cipher=ProtectedData.Protect(clear,null,DataProtectionScope.CurrentUser);try{try{File.WriteAllBytes(temp,cipher);File.Move(temp,GamingHistoryPath,true);}finally{if(File.Exists(temp))File.Delete(temp);}}finally{CryptographicOperations.ZeroMemory(cipher);}}finally{CryptographicOperations.ZeroMemory(clear);}
        }
        catch(Exception ex){StartupDiagnostics.Record("Gaming.history.save",ex.Message);}
        if(_gamingLabStatus is not null)_gamingLabStatus.Text=GamingStatus();
    }
    private static string GamingProfileKey(string game,string scenario,string settings)=>string.Join("|",new[]{game,scenario,settings}.Select(s=>s.Trim().ToUpperInvariant()));
    private string GamingStatus()
    {
        string P(double? value)=>value.HasValue?value.Value.ToString("F1",CultureInfo.CurrentCulture)+"%":UiText.T("Non disponibile");
        var last=_gamingSamples.LastOrDefault();var text=_gamingRecording?UiText.F("REGISTRAZIONE · {0} · {1} · {2} · {3} campioni · {4:F1} min",_sessionGameName,_sessionScenario,_sessionSettings,_gamingSamples.Count,(DateTimeOffset.Now-_sessionStarted!.Value).TotalMinutes):UiText.T("Registrazione ferma");
        if(last is not null)text+=UiText.F("\nCPU {0} · GPU {1} · RAM {2}",P(last.Cpu),P(last.Gpu),P(last.Ram));
        if(_gamingSamples.TakeLast(3).Count(s=>s.Ram>=90)==3)text+="\n"+UiText.T("RAM sopra il 90% negli ultimi tre campioni. Verifica le app che consumano memoria.");
        else if(_gamingSamples.TakeLast(3).Count(s=>s.Gpu>=95)==3)text+="\n"+UiText.T("GPU molto occupata negli ultimi tre campioni. Confronta la stessa scena con impostazioni grafiche diverse.");
        else if(_gamingSamples.TakeLast(3).Count(s=>s.Cpu>=90)==3)text+="\n"+UiText.T("CPU di sistema molto occupata. Controlla quali processi contribuiscono in Gestione attività.");
        if(_gamingCurrent is {} current)
        {
            text+=UiText.F("\nMedia CPU {0} · picco CPU {1}\nMedia GPU {2} · picco RAM {3}",P(current.CpuMean),P(current.CpuPeak),P(current.GpuMean),P(current.RamPeak));
            if(_gamingPrevious is {} previous)
                text+=UiText.F("\nConfronto con la sessione precedente per questo stesso gioco, scena e preset: CPU {0} pt · GPU {1} pt · RAM picco {2} pt. Misure indicative: durata, carico e condizioni devono essere comparabili.",Delta(current.CpuMean,previous.CpuMean),Delta(current.GpuMean,previous.GpuMean),Delta(current.RamPeak,previous.RamPeak));
            else text+="\n"+UiText.T("Prima misura salvata per questa combinazione gioco/scena/preset. Le sessioni successive verranno confrontate solo con le stesse condizioni.");
        }
        return text;
        static string Delta(double? now,double? before)=>now.HasValue&&before.HasValue?(now.Value-before.Value).ToString("+0.0;-0.0;0.0",CultureInfo.CurrentCulture):UiText.T("n/d");
    }
    private void AddCreatorLab()
    {
        WorkspacePanel.Children.Add(Card(BuildCreatorLaunchStudio()));
        var p=new StackPanel{Spacing=9};p.Children.Add(Label(AstraOS.Core.UiText.T("CREATOR STUDIO / MEDIA, PALETTE E STORYBOARD"),19));
        p.Children.Add(Label(AstraOS.Core.UiText.T("Crea una bacheca con immagini, video e audio locali. ASTRA legge dimensioni, durata e peso, estrae colori dominanti dalle immagini e prepara uno storyboard ordinato. I file originali restano nella loro cartella; la bacheca è cifrata per il tuo account."),12));
        _creatorDeliveryTarget=new ComboBox{Header=UiText.T("Destinazione di consegna"),HorizontalAlignment=HorizontalAlignment.Stretch};
        foreach(var (label,ratio) in new[]{("YouTube / schermo panoramico · 16:9","16:9"),("Reels / Shorts · verticale · 9:16","9:16"),("Post quadrato · 1:1","1:1")})_creatorDeliveryTarget.Items.Add(new ComboBoxItem{Content=UiText.T(label),Tag=ratio});
        _creatorDeliveryTarget.SelectedIndex=0;p.Children.Add(_creatorDeliveryTarget);
        var preflightRow=new StackPanel{Orientation=Orientation.Horizontal,Spacing=7};preflightRow.Children.Add(Action(UiText.T("Controllo consegna"),(_,_)=>_creatorPreflightState!.Text=CreatorPreflight()));
        preflightRow.Children.Add(Action(UiText.T("Esporta report di consegna…"),async(_,_)=>{try{await SaveLabFileAsync("astra-creator-preflight",".md",CreatorPreflight());}catch(Exception ex){_creatorPreflightState!.Text=ex.Message;}}));p.Children.Add(preflightRow);
        _creatorPreflightState=Label(UiText.T("Controlla formato, sottotitoli, descrizioni accessibili, crediti e tempi prima della pubblicazione."),12);p.Children.Add(_creatorPreflightState);
        p.Children.Add(Action(AstraOS.Core.UiText.T("Aggiungi media alla bacheca…"),async(_,_)=>await PickCreativeAssetsAsync()));
        _creativeState=Label(AstraOS.Core.UiText.T("Fino a 32 asset; i metadati video/audio dipendono dai codec installati in Windows."),12);p.Children.Add(_creativeState);
        _creativeList=new StackPanel{Spacing=8};p.Children.Add(_creativeList);
        p.Children.Add(Action(AstraOS.Core.UiText.T("Esporta storyboard Markdown…"),async(_,_)=>{try{await SaveLabFileAsync("astra-storyboard",".md",CreatorStoryboard());}catch(Exception ex){_creativeState.Text=ex.Message;}}));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Esporta palette CSS…"),async(_,_)=>{try{var colors=_creativeAssets.SelectMany(a=>a.Colors).Distinct().ToArray();if(colors.Length==0)throw new InvalidOperationException(AstraOS.Core.UiText.T("Aggiungi un'immagine per ricavare una palette."));await SaveLabFileAsync("astra-palette",".css",":root {\n"+string.Join("\n",colors.Select((c,i)=>$"  --asset-color-{i+1}: {c};\n  --asset-ink-{i+1}: {AstraPalette.ContrastingText(c)};"))+"\n}");}catch(Exception ex){_creativeState.Text=ex.Message;}}));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Assistente per brief, montaggio e accessibilità"),(_,_)=>{OpenAssistant("Creator · bacheca progetto");_assistantWindow?.SetInput("Proponi un piano di lavoro e una checklist di consegna/accessibilità dai soli metadati. Non affermare di avere visto immagini o filmati.\n"+CreatorStoryboard());}));
        WorkspacePanel.Children.Add(Card(p));RenderCreativeAssets();AddSubtitleLab();
    }
    private async Task PickCreativeAssetsAsync()
    {
        try{var picker=new FileOpenPicker();foreach(var ext in CreatorMedia.Extensions)picker.FileTypeFilter.Add(ext);WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var files=await picker.PickMultipleFilesAsync();if(files.Count+_creativeAssets.Count>32)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 asset nella bacheca."));int added=0;foreach(var file in files){if(_creativeAssets.Any(a=>a.Path.Equals(file.Path,StringComparison.OrdinalIgnoreCase)))continue;try{_creativeAssets.Add(await CreatorMedia.ReadAsync(file.Path));added++;}catch(Exception ex){_creativeState!.Text=file.Name+": "+ex.Message;}}CreatorMedia.Save(_creativeAssets);RenderCreativeAssets();_creativeState!.Text=UiText.F("Aggiunti {0} asset. Ordina le inquadrature e modifica durata e note prima dell'esportazione.",added);}catch(Exception ex){if(_creativeState is not null)_creativeState.Text=ex.Message;}
    }
    private void RenderCreativeAssets()
    {
        if(_creativeList is null)return;_creativeList.Children.Clear();foreach(var a in _creativeAssets)
        {
            var p=new StackPanel{Spacing=6};p.Children.Add(Label(a.Name,15));p.Children.Add(Label(AstraOS.Core.UiText.F("{0} · {1} × {2} · {3:F2} MB", a.Kind, a.Width, a.Height, a.Bytes/1048576.0)+(a.SourceSeconds is double durationSeconds?UiText.F(" · durata file {0:F2} s",durationSeconds):""),12));
            var colors=new StackPanel{Orientation=Orientation.Horizontal,Spacing=5};foreach(var color in a.Colors){var text=new TextBlock{Text=color,FontSize=11,Foreground=new SolidColorBrush(ColorFromHex(AstraPalette.ContrastingText(color)))};colors.Children.Add(new Border{Background=new SolidColorBrush(ColorFromHex(color)),Padding=new Thickness(8),CornerRadius=new CornerRadius(5),Child=text});}p.Children.Add(colors);
            var note=new TextBox{Header=UiText.T("Nota / inquadratura"),Text=a.Note,MaxLength=1000};var duration=new NumberBox{Header=UiText.T("Durata nello storyboard (s)"),Value=a.Seconds,Minimum=.1,Maximum=86400};
            var alt=new TextBox{Header=UiText.T("Descrizione accessibile / testo alternativo"),Text=a.AltText,MaxLength=500};var rights=new TextBox{Header=UiText.T("Fonte, licenza o crediti"),Text=a.Rights,MaxLength=500};p.Children.Add(note);p.Children.Add(duration);p.Children.Add(alt);p.Children.Add(rights);
            var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6};row.Children.Add(Action(AstraOS.Core.UiText.T("Salva dettagli"),(_,_)=>{int index=_creativeAssets.FindIndex(x=>x.Id==a.Id);_creativeAssets[index]=a with{Note=note.Text,AltText=alt.Text,Rights=rights.Text,Seconds=double.IsFinite(duration.Value)?Math.Clamp(duration.Value,.1,86400):5};CreatorMedia.Save(_creativeAssets);RenderCreativeAssets();}));
            void Move(int delta){int index=_creativeAssets.FindIndex(x=>x.Id==a.Id),target=index+delta;if(target>=0&&target<_creativeAssets.Count){(_creativeAssets[index],_creativeAssets[target])=(_creativeAssets[target],_creativeAssets[index]);CreatorMedia.Save(_creativeAssets);RenderCreativeAssets();}}
            row.Children.Add(Action("↑",(_,_)=>Move(-1)));row.Children.Add(Action("↓",(_,_)=>Move(1)));row.Children.Add(Action(AstraOS.Core.UiText.T("Rimuovi"),(_,_)=>{_creativeAssets.RemoveAll(x=>x.Id==a.Id);CreatorMedia.Save(_creativeAssets);RenderCreativeAssets();}));p.Children.Add(row);_creativeList.Children.Add(Card(p));
        }
    }
    private string CreatorPreflight()
    {
        string target=_creatorDeliveryTarget?.SelectedItem is ComboBoxItem item?item.Tag as string??"16:9":"16:9";
        var (ratio,width,height)=target switch{"9:16"=>(9d/16,1080u,1920u),"1:1"=>(1d,1080u,1080u),_=>(16d/9,1920u,1080u)};
        var report=new StringBuilder(UiText.T("# ASTRA Creator · Controllo consegna")+"\n\n");
        report.AppendLine(UiText.F("Destinazione: {0} · dimensione minima suggerita: {1}×{2}",target,width,height));
        report.AppendLine(UiText.F("Asset: {0} · durata storyboard: {1:F1} s",_creativeAssets.Count,_creativeAssets.Sum(a=>a.Seconds)));
        if(_creativeAssets.Count==0)report.AppendLine("- "+UiText.T("Aggiungi almeno un asset prima della consegna."));
        foreach(var asset in _creativeAssets)
        {
            var issues=new List<string>();
            if(string.IsNullOrWhiteSpace(asset.Rights))issues.Add(UiText.T("fonte, licenza o credito non registrati"));
            if(asset.Kind=="image"&&string.IsNullOrWhiteSpace(asset.AltText))issues.Add(UiText.T("testo alternativo mancante"));
            if(asset.Width>0&&asset.Height>0)
            {
                double current=(double)asset.Width/asset.Height;
                if(Math.Abs(current-ratio)/ratio>0.04)issues.Add(UiText.F("rapporto d'aspetto {0}:{1} diverso da {2}",asset.Width,asset.Height,target));
                if(asset.Width<width||asset.Height<height)issues.Add(UiText.F("risoluzione inferiore al suggerimento {0}×{1}",width,height));
            }
            if(string.IsNullOrWhiteSpace(asset.Note))issues.Add(UiText.T("inquadratura o nota storyboard mancante"));
            report.AppendLine($"- {asset.Name}: "+(issues.Count==0?UiText.T("controlli dei metadati superati; verifica comunque il media e le impostazioni di esportazione."):string.Join("; ",issues)));
        }
        if(_subtitleSource is null)report.AppendLine("- "+UiText.T("Nessun file SRT verificato in ASTRA. Aggiungi o controlla i sottotitoli se richiesti per la consegna."));
        else report.AppendLine(UiText.F("- File SRT caricato: {0} cue; controlla il testo e la sincronizzazione rispetto al video esportato.",SubtitleTimeline.Parse(_subtitleSource).Count));
        if(!_creativeAssets.Any(a=>a.Kind=="audio"))report.AppendLine("- "+UiText.T("Nessun asset audio separato nell'elenco; controlla musica, mix e diritti nell'editor di destinazione."));
        report.AppendLine("\n"+UiText.T("I controlli usano solo metadati dei file e note inserite dall'utente. ASTRA non analizza i fotogrammi video e non certifica diritti, accessibilità, colore, qualità audio o conformità alle piattaforme."));
        return report.ToString();
    }
    private string CreatorStoryboard()
    {
        var result=new StringBuilder("# ASTRA · "+UiText.T("Storyboard")+"\n\n");double cursor=0;foreach(var a in _creativeAssets){result.AppendLine($"## {_creativeAssets.IndexOf(a)+1}. {a.Name.Replace("\n"," ")}\n\n{cursor:F2}–{cursor+a.Seconds:F2} s · {a.Kind} · {a.Width}×{a.Height}\n\n{a.Note}\n\n"+UiText.T("Testo alternativo")+": "+a.AltText+"\n\n"+UiText.T("Crediti")+": "+a.Rights+"\n\n"+UiText.T("File")+": `"+Path.GetFileName(a.Path).Replace("`","")+"`\n\n"+UiText.T("Palette")+": "+string.Join(", ",a.Colors)+"\n");cursor+=a.Seconds;}return result.AppendLine(UiText.F("Durata complessiva: {0:F2} s",cursor)).ToString();
    }
    private void AddSubtitleLab()
    {
        var p=new StackPanel{Spacing=8};p.Children.Add(Label(AstraOS.Core.UiText.T("SUBTITLE LAB / SINCRONIZZA E CORREGGI"),18));p.Children.Add(Label(AstraOS.Core.UiText.T("Importa SRT, applica un offset e una scala temporale per correggere ritardo e deriva. La trascrizione dell'assistente può essere esportata in SRT e aperta qui."),12));
        p.Children.Add(Action(AstraOS.Core.UiText.T("Importa sottotitoli SRT…"),async(_,_)=>{try{var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".srt");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is null)return;if(new FileInfo(file.Path).Length>2_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("SRT oltre 2 MB."));var text=await File.ReadAllTextAsync(file.Path);var cues=SubtitleTimeline.Parse(text);_subtitleSource=text;_subtitleState!.Text=UiText.F("{0} sottotitoli caricati · fine {1}",cues.Count,cues[^1].End);}catch(Exception ex){_subtitleState!.Text=ex.Message;}}));
        _subtitleOffset=new NumberBox{Header=UiText.T("Offset (ms; negativo anticipa)"),Minimum=-3600000,Maximum=3600000,Value=0};_subtitleScale=new NumberBox{Header=UiText.T("Scala temporale (1 = invariata)"),Minimum=.5,Maximum=2,Value=1};p.Children.Add(_subtitleOffset);p.Children.Add(_subtitleScale);_subtitleState=Label(_subtitleSource is null?AstraOS.Core.UiText.T("Nessun SRT importato."):AstraOS.Core.UiText.T("SRT pronto per la regolazione."),12);p.Children.Add(_subtitleState);
        p.Children.Add(Action(AstraOS.Core.UiText.T("Esporta SRT sincronizzato…"),async(_,_)=>{try{if(_subtitleSource is null)throw new InvalidOperationException(AstraOS.Core.UiText.T("Importa prima un SRT."));var adjusted=SubtitleTimeline.Retime(SubtitleTimeline.Parse(_subtitleSource),_subtitleOffset.Value,_subtitleScale.Value);await SaveLabFileAsync("astra-subtitles",".srt",SubtitleTimeline.Export(adjusted));_subtitleState.Text=UiText.F("{0} sottotitoli esportati",adjusted.Count);}catch(Exception ex){_subtitleState.Text=ex.Message;}}));WorkspacePanel.Children.Add(Card(p));
    }
    private async Task SaveLabFileAsync(string name,string extension,string text)
    {
        var picker=new FileSavePicker{SuggestedFileName=name};picker.FileTypeChoices.Add(extension,new[]{extension});WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSaveFileAsync();if(file is not null){await File.WriteAllTextAsync(file.Path,text);StatusText.Text=UiText.T("Esportato: ")+file.Path;}
    }
}
