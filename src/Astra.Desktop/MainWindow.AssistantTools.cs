using System.Text;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly AssistantRoutineStore _assistantRoutineStore=new();
    private readonly List<AssistantRoutine> _personalRoutines=new();
    private readonly List<WebEvidence> _webEvidence=new();
    private StackPanel? _assistantMemoryPanel,_webResults;
    private ComboBox? _routineChoice;
    private TextBox? _routinePreview,_webQuery;
    private TextBlock? _routineLog,_assistantContextView;
    private CancellationTokenSource? _routineOperation,_webOperation;
    private TextBlock? _assistantHeroName;
    private bool _routineBusy;
    private string AssistantName=>AssistantWorkspace.Name(_workspace);

    private void InitializeAssistantTools()
    {
        try{_personalRoutines.AddRange(_assistantRoutineStore.Load());}catch(Exception ex){StartupDiagnostics.Record("Assistant.routines",ex.Message);}
        var memory=new StackPanel{Spacing=12};
        // Preserve the actual PDF library, encrypted notes and settings controls.
        foreach(var element in BrainPanel.Children.ToArray()){BrainPanel.Children.Remove(element);memory.Children.Add(element);}
        _assistantMemoryPanel=memory;
        var hero=new StackPanel{Spacing=12,Orientation=Orientation.Horizontal};_assistantHeroName=Label(AssistantName,18);_assistantHeroName.VerticalAlignment=VerticalAlignment.Center;hero.Children.Add(_assistantHeroName);
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};row.Children.Add(Action(UiText.T("Apri la conversazione"),(_,_)=>OpenAssistant()));row.Children.Add(Action(UiText.T("Ferma AI e libera memoria"),(_,_)=>{_brain.Stop();StatusText.Text=UiText.T("Provider AI arrestato; cronologia temporanea cancellata.");}));hero.Children.Add(row);
        var quick=new StackPanel{Spacing=7};
        foreach(var (label,message) in new[]{("Organizza il mio lavoro","Aiutami a scegliere il prossimo passo dalle mie attività e dal contesto disponibile."),("Tutor e quiz","Crea un quiz dai documenti che ho importato. Indica le fonti e non inventare il contenuto mancante."),("Gaming coach","gaming coach"),("Revisione Creator","creator audit")})
            quick.Children.Add(Action(UiText.T(label),(_,_)=>{OpenAssistant();_assistantWindow?.SetMessage(UiText.T(message));}));
        hero.Children.Add(new Expander{Header=UiText.T("Spunti per iniziare"),Content=quick});BrainPanel.Children.Add(Card(hero));
        var tabs=new TabView{IsAddTabButtonVisible=false,TabWidthMode=TabViewWidthMode.SizeToContent,MinHeight=360};
        TabViewItem Tab(string name,UIElement content)=>new(){Header=UiText.T(name),IsClosable=false,Content=content,Padding=new Thickness(8)};
        tabs.TabItems.Add(Tab("Memoria e documenti",memory));
        tabs.TabItems.Add(Tab("Agent team",BuildAgentTeam()));
        tabs.SelectedIndex=1;
        tabs.TabItems.Add(Tab("Knowledge graph",BuildBrainGraph()));
        tabs.TabItems.Add(Tab("Ricerca web",BuildAssistantWeb()));
        tabs.TabItems.Add(Tab("Routine e automazioni",BuildAssistantRoutines()));
        tabs.TabItems.Add(Tab("Skills",BuildAgentSkills()));
        tabs.TabItems.Add(Tab("Progetti e codice",BuildAgentProject()));
        var context=new StackPanel{Spacing=10};context.Children.Add(Label(UiText.T("Il contesto viene costruito dai dati disponibili in ASTRA. Le altre app non vengono lette in modo continuo."),13));
        _assistantContextView=Label("",13);context.Children.Add(_assistantContextView);context.Children.Add(Action(UiText.T("Aggiorna contesto"),(_,_)=>_assistantContextView.Text=BuildAssistantContext()));
        context.Children.Add(Action(UiText.T("Usa questo contesto nella chat"),(_,_)=>{OpenAssistant();_assistantWindow?.SetInput(BuildAssistantContext());}));tabs.TabItems.Add(Tab("Contesto",context));BrainPanel.Children.Add(tabs);
        Closed+=(_,_)=>{_webOperation?.Cancel();_routineOperation?.Cancel();};
    }

    private void RefreshAssistantIdentity()
    {
        bool unified=true;
        BrainNav.Visibility=unified?Visibility.Collapsed:Visibility.Visible;
        if(_assistantMemoryPanel?.Children.FirstOrDefault() is Border{Child:StackPanel legacy})
        {
            if(legacy.Children.FirstOrDefault() is TextBlock title)title.Text=unified?UiText.T("Memoria personale"):"Second Brain";
            if(legacy.Children.ElementAtOrDefault(2) is Button open)open.Content=UiText.T("Apri la conversazione");
        }
        L10n0219.Text=unified?"ASTRA Assistant":UiText.T("Assistente");
        if(_assistantHeroName is not null)_assistantHeroName.Text=AssistantName;
        ToolTipService.SetToolTip(L10n0215,AssistantName);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(L10n0215,AssistantName);
        if(_v4LastPage=="brain"){PageTitle.Text=AssistantName;PageSubtitle.Text=UiText.T("Chat · memoria · fonti web · routine · strumenti del profilo");}
        if(_assistantContextView is not null)_assistantContextView.Text=BuildAssistantContext();
        _assistantWindow?.UpdateIdentity(AssistantName,_workspace+" · "+_v4LastPage);
    }
    private string BuildAssistantContext()
    {
        var text=new StringBuilder("Workspace: "+_workspace+"\nStudy: "+StudyCatalog.Name(_preferences.StudyDomain,_preferences.EngineeringTrack));
        text.AppendLine("\nOpen tasks:");foreach(var t in _v4Tasks.Where(t=>!t.Completed))text.AppendLine("- "+t.Text);
        if(_workspace=="gaming")text.AppendLine("\nMeasured gaming data:\n"+GamingStatus());
        if(_workspace=="creator")text.AppendLine("\nCreator asset audit:\n"+CreatorDeliveryAudit());
        if(_workspace=="stem")text.AppendLine("\nSimulation data:\n"+SimulationContextAsync());
        text.AppendLine("\n"+AssistantWorkspace.Guidance(_workspace));return text.ToString();
    }
    private string CreatorDeliveryAudit()
    {
        if(_creativeAssets.Count==0)return UiText.T("La bacheca è vuota. Aggiungi gli asset nel profilo Creator per una revisione basata sui metadati reali.");
        var result=new StringBuilder();result.AppendLine(UiText.F("Asset: {0} · durata storyboard: {1:F1} s",_creativeAssets.Count,_creativeAssets.Sum(a=>a.Seconds)));
        foreach(var asset in _creativeAssets)result.AppendLine($"• {asset.Name}: {asset.Width}×{asset.Height}, {asset.Seconds:F1}s; "+(string.IsNullOrWhiteSpace(asset.Note)?UiText.T("nota/inquadratura mancante"):asset.Note)+"; "+(string.IsNullOrWhiteSpace(asset.AltText)?UiText.T("testo alternativo assente"):asset.AltText)+"; "+(string.IsNullOrWhiteSpace(asset.Rights)?UiText.T("fonte/licenza non registrata"):asset.Rights));
        result.AppendLine(CreatorPreflight());
        result.AppendLine(UiText.T("Da verificare prima della consegna: licenze e crediti, sottotitoli, leggibilità, aspect ratio, audio e formati richiesti. Questi controlli non attestano automaticamente i diritti né la qualità audiovisiva."));return result.ToString();
    }
    private StackPanel BuildAssistantWeb()
    {
        var p=new StackPanel{Spacing=10};p.Children.Add(Label(UiText.T("Cerca sul web o leggi una pagina HTTPS. Soltanto la query o l'indirizzo inseriti vengono inviati al sito; le note e i PDF restano locali."),13));
        _webQuery=new TextBox{Header=UiText.T("Query o indirizzo HTTPS")};p.Children.Add(_webQuery);
        p.Children.Add(Action(UiText.T("Cerca e mostra le fonti"),async(_,_)=>await WebUiAsync(false)));
        p.Children.Add(Action(UiText.T("Leggi questa pagina"),async(_,_)=>await WebUiAsync(true)));
        p.Children.Add(Action(UiText.T("Apri ricerca nel browser"),(_,_)=>{try{OpenAssistantSearchBrowser(_webQuery.Text);}catch(Exception ex){StatusText.Text=UiText.T(ex.Message);}}));
        p.Children.Add(Action(UiText.T("Annulla ricerca"),(_,_)=>_webOperation?.Cancel()));
        _webResults=new StackPanel{Spacing=10};p.Children.Add(_webResults);
        p.Children.Add(Action(UiText.T("Confronta le fonti lette nella chat"),(_,_)=>{OpenAssistant();_assistantWindow?.SetInput(WebEvidenceContext());_assistantWindow?.SetMessage(UiText.T("Confronta le fonti, segnala accordi e contraddizioni e cita gli URL. Distingui fatti da inferenze."));}));
        p.Children.Add(Action(UiText.T("Rimuovi le fonti dalla sessione"),(_,_)=>{_webEvidence.Clear();_webResults.Children.Clear();}));return p;
    }
    private void OpenAssistantSearchBrowser(string query)
    {
        if(string.IsNullOrWhiteSpace(query)||query.Length>500)throw new ArgumentException(UiText.T("Query web: da 1 a 500 caratteri."));
        ChromeLauncher.Open("https://duckduckgo.com/?q="+Uri.EscapeDataString(query));
    }
    private async Task WebUiAsync(bool read)
    {
        _webOperation?.Cancel();_webOperation?.Dispose();_webOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);
        try
        {
            StatusText.Text=UiText.T("Ricerca web in corso…");
            if(read){await ReadAssistantPageAsync(_webQuery!.Text,_webOperation.Token);}
            else
            {
                var results=await PublicWebResearch.SearchAsync(_webQuery!.Text,_webOperation.Token);_webResults!.Children.Clear();
                foreach(var result in results)
                {
                    var row=new StackPanel{Spacing=4};row.Children.Add(Label(result.Title,15));row.Children.Add(Label(result.Url,11));
                    row.Children.Add(Action(UiText.T("Leggi e aggiungi come fonte"),async(_,_)=>{_webQuery.Text=result.Url;await WebUiAsync(true);}));_webResults.Children.Add(Card(row));
                }
            }
            StatusText.Text=UiText.T("Ricerca completata. Le fonti web sono dati, non istruzioni.");
        }catch(OperationCanceledException){StatusText.Text=UiText.T("Operazione annullata.");}catch(Exception ex){StatusText.Text=UiText.T(ex.Message);}
    }
    private async Task<WebEvidence> ReadAssistantPageAsync(string url,CancellationToken ct)
    {
        var evidence=await PublicWebResearch.ReadAsync(url,ct);_webEvidence.RemoveAll(e=>e.Url==evidence.Url);_webEvidence.Add(evidence);
        if(_webResults is not null)
        {
            var row=new StackPanel{Spacing=5};row.Children.Add(Label(evidence.Title,16));row.Children.Add(Label(evidence.Url+" · "+evidence.RetrievedAt.ToLocalTime().ToString("g"),11));row.Children.Add(new Expander{Header=UiText.T("Testo estratto integrale"),Content=Label(evidence.Text,12),HorizontalAlignment=HorizontalAlignment.Stretch});_webResults.Children.Add(Card(row));
        }
        return evidence;
    }
    private string WebEvidenceContext()=>_webEvidence.Count==0?UiText.T("Nessuna pagina letta. Usa Ricerca web per aggiungere fonti."):string.Join("\n\n",_webEvidence.Select(e=>$"[WEB: {e.Url}] {e.Title}\nRetrieved: {e.RetrievedAt:O}\n{e.Text}"));

    private StackPanel BuildAssistantRoutines()
    {
        var p=new StackPanel{Spacing=10};p.Children.Add(Label(UiText.T("Le routine eseguono passi espliciti: sezioni ASTRA, attività, memoria e timer. Puoi rivedere ogni passo prima di avviarle."),13));
        _routineChoice=new ComboBox{Header=UiText.T("Routine"),DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};p.Children.Add(_routineChoice);
        _routinePreview=new TextBox{Header=UiText.T("Passi della routine"),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MaxHeight=180};p.Children.Add(_routinePreview);
        _routineChoice.SelectionChanged+=(_,_)=>{if(_routineChoice.SelectedItem is AssistantRoutine r)_routinePreview.Text=UiText.T(r.Description)+"\n"+string.Join("\n",r.Steps.Select((s,i)=>$"{i+1}. {s.Kind}: {s.Argument}"));};
        p.Children.Add(Action(UiText.T("Avvia questa routine"),async(_,_)=>{if(_routineChoice.SelectedItem is AssistantRoutine r)await RunAssistantRoutineUiAsync(r);}));
        p.Children.Add(Action(UiText.T("Annulla routine"),(_,_)=>_routineOperation?.Cancel()));
        var interval=new NumberBox{Header=UiText.T("Intervallo automazione (minuti)"),Minimum=5,Maximum=10080,Value=60};p.Children.Add(interval);
        p.Children.Add(Action(UiText.T("Crea automazione disattivata"),(_,_)=>
        {
            try{if(_routineChoice.SelectedItem is not AssistantRoutine r)return;AddWidget(new WidgetSpec{Kind="automation",Title=r.Name,Text=UiText.T("Routine programmata"),Automation=new AutomationSpec{Enabled=false,Action="routine",Argument=r.Id,IntervalMinutes=(int)interval.Value,NextDue=DateTimeOffset.Now.AddMinutes(interval.Value)}});StatusText.Text=UiText.T("Automazione creata. Abilitala nella sezione Widget quando desideri eseguirla.");}catch(Exception ex){StatusText.Text=UiText.T(ex.Message);}
        }));
        _routineLog=Label("",12);p.Children.Add(_routineLog);
        var editor=new StackPanel{Spacing=8};var name=new TextBox{Header=UiText.T("Nome routine")};var lines=new TextBox{Header=UiText.T("Un passo per riga: navigate:stem, focus:25, task:testo, remember:testo"),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=120};editor.Children.Add(name);editor.Children.Add(lines);
        editor.Children.Add(Action(UiText.T("Salva routine personale"),(_,_)=>
        {
            try
            {
                var steps=lines.Text.Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(line=>{var pair=line.Trim().Split(':',2);if(pair.Length!=2)throw new InvalidDataException(UiText.T("Usa azione:argomento per ogni passo."));return new AssistantAction(pair[0].Trim(),pair[1].Trim());}).ToArray();
                var routine=new AssistantRoutine(Guid.NewGuid().ToString("N"),name.Text.Trim(),UiText.T("Routine personale"),steps);
                var next=_personalRoutines.Append(routine).ToArray();AssistantRoutineStore.Validate(next);_assistantRoutineStore.Save(next);_personalRoutines.Add(routine);RefreshRoutineChoices(routine.Id);
            }catch(Exception ex){_routineLog.Text=ex.Message;}
        }));
        editor.Children.Add(Action(UiText.T("Rimuovi routine personale selezionata"),(_,_)=>{if(_routineChoice.SelectedItem is AssistantRoutine r&&_personalRoutines.Contains(r)){_personalRoutines.Remove(r);_assistantRoutineStore.Save(_personalRoutines);RefreshRoutineChoices();}}));
        p.Children.Add(new Expander{Header=UiText.T("Crea una routine personale"),Content=editor});RefreshRoutineChoices();return p;
    }
    private void RefreshRoutineChoices(string? id=null){if(_routineChoice is null)return;var all=AssistantWorkspace.Routines.Select(r=>r with{Name=UiText.T(r.Name)}).Concat(_personalRoutines).ToArray();_routineChoice.ItemsSource=all;_routineChoice.SelectedItem=all.FirstOrDefault(r=>r.Id==id)??all[0];}
    private async Task RunAssistantRoutineUiAsync(AssistantRoutine routine)
    {
        try{await RunAssistantRoutineAsync(routine,_windowLifetime.Token);}catch(OperationCanceledException){_routineLog!.Text+=UiText.T("\nRoutine annullata; i passi già completati restano applicati.");}catch(Exception ex){_routineLog!.Text+= "\n"+ex.Message;}
    }
    private async Task RunAssistantRoutineAsync(AssistantRoutine routine,CancellationToken ct)
    {
        AssistantWorkspace.Validate(routine.Steps);if(_routineBusy)throw new InvalidOperationException(UiText.T("Una routine è già in esecuzione."));
        _routineBusy=true;_routineOperation?.Dispose();_routineOperation=CancellationTokenSource.CreateLinkedTokenSource(ct);ct=_routineOperation.Token;_routineLog!.Text="";
        try
        {
            foreach(var step in routine.Steps)
            {
                ct.ThrowIfCancellationRequested();string? result=step.Kind switch
                {
                    "navigate"=>await ExecuteBrainCommandAsync("open "+step.Argument,ct),
                    "focus"=>await ExecuteBrainCommandAsync("focus "+step.Argument+" minutes",ct),
                    "task"=>await ExecuteBrainCommandAsync("add task "+step.Argument,ct),
                    "remember"=>await ExecuteBrainCommandAsync("remember that "+step.Argument,ct),
                    _=>throw new InvalidDataException("Unknown routine action")
                };
                _routineLog.Text+= "✓ "+result+"\n";await Task.Yield();
            }
        }finally{_routineBusy=false;}
    }
    private async Task<string?> ExecuteAssistantToolAsync(string input,CancellationToken ct)
    {
        foreach(string prefix in new[]{"team ","agents ","agenti ","delega "})
            if(input.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))
            {
                var job=_teamStore.Enqueue(input[prefix.Length..],context:BuildAssistantContext(),project:_agentProjectRoot??"");
                _selectedTeamJob=job.Id;StartTeamWorker();RefreshTeamJobs();
                return "Agent team job queued: "+job.Id+"\nPlan: "+string.Join(" → ",job.Steps.Select(s=>AstraOS.Core.Agents.AgentCatalog.Get(s.AgentId).Name))+"\nFollow progress and export results in ASTRA Assistant → Agent team. Public web research is off for chat delegation; enable it when creating a job in the team panel.";
            }
        var action=AssistantWorkspace.Parse(input);if(action is null)return null;
        switch(action.Kind)
        {
            case "context":return BuildAssistantContext();
            case "navigate":Navigate(action.Argument);return UiText.T("Centro di controllo aperto.");
            case "web-search":
                try{var results=await PublicWebResearch.SearchAsync(action.Argument,ct);return UiText.T("Risultati web; non sono ancora pagine lette:")+"\n"+string.Join("\n",results.Select(r=>r.Title+"\n"+r.Url))+"\n"+UiText.T("Usa leggi pagina https://… per aggiungere una fonte.");}
                catch(HttpRequestException ex){return ex.Message+"\n"+UiText.T("Puoi usare Apri ricerca nel browser nella scheda Ricerca web.");}
            case "web-read":var evidence=await ReadAssistantPageAsync(action.Argument,ct);return $"[WEB: {evidence.Url}] {evidence.Title}\n{evidence.Text}";
            case "routine":RefreshRoutineChoices(action.Argument);return UiText.T("Routine preparata. Controlla i passi nella scheda Routine e avviala con il relativo pulsante.");
            case "creator-audit":return CreatorDeliveryAudit();
            case "gaming-coach":
                return GamingStatus()+"\n"+(_gamingSamples.Count==0?UiText.T("Avvia una registrazione dal Gaming Lab per ottenere consigli basati su misure reali."):UiText.T("Confronta la stessa scena, risoluzione e impostazioni. I valori di utilizzo non dimostrano da soli un collo di bottiglia e non misurano FPS o latenza."));
            case "study":case "quiz":
                string notes=await FindAssistantNotesAsync(action.Argument,ct);
                return await RunAssistantTaskAsync(action.Kind=="quiz"?"Create five grounded questions, then an answer key with source identifiers. If evidence is absent, say so.":"Create a realistic study plan with prerequisites, active recall and a way to verify progress. Distinguish document evidence from suggested planning.","Topic: "+action.Argument+"\nSources:\n"+notes, false,ct);
            default:return null;
        }
    }
}
