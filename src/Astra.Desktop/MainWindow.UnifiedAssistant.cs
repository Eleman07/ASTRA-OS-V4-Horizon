using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private Grid? _unifiedRoot, _conversationHost, _graphHost, _conversationHistoryHost;
    private Border? _unifiedSidebar;
    private TextBox? _activityTerminal;
    private TextBlock? _livePhase, _renderBudgetLabel;
    private ProgressBar? _conversationProgress;
    private string _conversationMode = "auto", _unifiedJobFingerprint = "";
    private bool _conversationWorking;
    private string[] _activeConversationRoles = [], _previousConversationRoles = [];
    private CheckBox? _conversationWeb;
    private ComboBox? _conversationSimulation;
    private TextBox? _conversationSimulationParameters;
    private readonly EncryptedRecords<AssistantActivityEvent> _activityRecords = new(Path.Combine(AstraPaths.UserDataRoot,"agents","activity"));
    private sealed record AssistantActivityEvent(DateTimeOffset At, string Kind, string Message);
    private RenderBudget CurrentRenderBudget => RenderBudget.Select(SystemSnapshot.Capture().PhysicalMemoryMb,Environment.ProcessorCount,_preferences.EcoMode,_preferences.ReducedMotion);

    private void BuildUnifiedAssistant(StackPanel memory)
    {
        if(OrbitPanel.Children.FirstOrDefault() is Border orbitControls){OrbitPanel.Children.Remove(orbitControls);OrbitPanel.Children.Insert(0,Details("Desktop 3D · controlli e qualità",orbitControls));}
        var defaults=_teamStore.Configuration;AgentSkillDefaults.InstallMissing(defaults);_teamStore.Settings.Put("runtime",defaults);
        var legacy = (StackPanel)BuildAgentTeam(); // Reuse the actual controls and durable worker, not a second UI model.
        var disclosures = legacy.Children.OfType<Expander>().ToArray();
        foreach(var disclosure in disclosures)legacy.Children.Remove(disclosure);
        _unifiedRoot = new Grid { ColumnSpacing=12, RowSpacing=10 };
        _unifiedRoot.RowDefinitions.Add(new(){Height=GridLength.Auto});
        _unifiedRoot.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        _unifiedRoot.ColumnDefinitions.Add(new(){Width=new GridLength(238)});
        _unifiedRoot.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        _unifiedRoot.ColumnDefinitions.Add(new(){Width=new GridLength(420)});
        var header = new Grid { ColumnSpacing=12 };
        header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var heading = new StackPanel {Spacing=3};
        _assistantHeroName=TeamHeading("ASTRA / CONVERSATION OS",22);heading.Children.Add(_assistantHeroName);
        _livePhase=Label("Un obiettivo. La tua conoscenza. La squadra giusta.",12);heading.Children.Add(_livePhase);header.Children.Add(heading);
        var tools = new StackPanel {Orientation=Orientation.Horizontal,Spacing=7};
        tools.Children.Add(Action("☷ Pannelli",(_,_)=>{if(_unifiedSidebar is null)return;bool show=_unifiedSidebar.Visibility!=Visibility.Visible;_unifiedSidebar.Visibility=show?Visibility.Visible:Visibility.Collapsed;_unifiedRoot.ColumnDefinitions[0].Width=new GridLength(show?238:0);}));
        tools.Children.Add(Action("↻ Grafo",async(_,_)=>{if(_loadBrainGraph is not null)await _loadBrainGraph();}));
        tools.Children.Add(Action("□ Ferma",(_,_)=>{_assistantWindow?.CancelCurrent();if(_selectedTeamJob is not null&&_teamStore.Records.Get(_selectedTeamJob) is {Status:"Queued" or "Running" or "WaitingForProvider"} active&&active.ConversationId==_assistantWindow?.CurrentConversationId)_teamStore.Cancel(active.Id);_brain.Stop();LogAssistantEvent("cancel","Arresto richiesto dall'utente");}));
        Grid.SetColumn(tools,1);header.Children.Add(tools);Grid.SetColumnSpan(header,3);_unifiedRoot.Children.Add(header);

        var sidebar = new StackPanel { Spacing=9, Padding=new Thickness(12) };
        sidebar.Children.Add(TeamHeading("CONTROL ROOM",14));
        _conversationHistoryHost=new Grid{Height=350};sidebar.Children.Add(Details("Conversazioni salvate",_conversationHistoryHost));
        var mode = new ComboBox {Header="Orchestrazione",ItemsSource=new[]{"Automatica · consigliata","Risposta diretta","Specialisti + revisione"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};
        mode.SelectionChanged+=(_,_)=>_conversationMode=mode.SelectedIndex switch {1=>"direct",2=>"team",_=>"auto"};sidebar.Children.Add(mode);
        var input = new ComboBox {Header="Tipo di input",ItemsSource=new[]{"Tutti · rilevamento automatico","Documenti e dati","Immagini / OCR","Audio"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};
        input.SelectionChanged+=(_,_)=>{OpenAssistant();_assistantWindow!.PreferredInput=input.SelectedIndex switch {1=>"document",2=>"image",3=>"audio",_=>"all"};};sidebar.Children.Add(input);
        _conversationWeb = new CheckBox {Content="Fonti web per i nuovi incarichi",IsChecked=false};sidebar.Children.Add(_conversationWeb);
        var calculations=new StackPanel{Spacing=8};_conversationSimulation=new ComboBox{Header="Strumento deterministico",DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};
        _conversationSimulation.Items.Add(new SimulationChoice("","Nessun calcolo allegato"));foreach(var tool in SimulationLab.Tools)_conversationSimulation.Items.Add(new SimulationChoice(tool.Id,tool.Name));_conversationSimulation.SelectedIndex=0;calculations.Children.Add(_conversationSimulation);
        _conversationSimulationParameters=new TextBox{Header="Parametri JSON · unità dello strumento",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,PlaceholderText="{\"alt\":400,\"j2\":0}"};calculations.Children.Add(_conversationSimulationParameters);sidebar.Children.Add(Details("Calcoli e simulazioni",calculations));
        foreach(var disclosure in disclosures)
        {
            string name=disclosure.Header?.ToString()??"";
            if(name.Contains("Codex") || name.Contains("Obsidian") || name.Contains("squadra") || name.Contains("Reusable") || name.Contains("Choose your model"))
            {
                if(name.Contains("Choose your model")&&disclosure.Content is StackPanel runtime){if(runtime.Children.FirstOrDefault() is Expander local&&local.Header?.ToString()?.Contains("Advanced")==true)runtime.Children.Remove(local);disclosure.Header="OpenRouter · modelli e recupero";}
                disclosure.IsExpanded=false;sidebar.Children.Add(disclosure);
            }
        }
        var credentials = new StackPanel {Spacing=8};var key = new PasswordBox {Header="La tua chiave OpenRouter"};credentials.Children.Add(key);
        credentials.Children.Add(Action("Salva chiave cifrata Windows",(_,_)=>{try{_openRouterCredentials.Save(key.Password);key.Password="";LogAssistantEvent("config","Chiave OpenRouter salvata nello store cifrato Windows");}catch(Exception ex){_livePhase.Text=ex.Message;}}));
        sidebar.Children.Add(Details("OpenRouter · credenziali",credentials));
        sidebar.Children.Add(Details("Crea / importa / modifica skills",BuildAgentSkills()));
        sidebar.Children.Add(Details("Memoria e documenti",memory));
        sidebar.Children.Add(Details("Progetti, script e approvazioni",BuildAgentProject()));
        sidebar.Children.Add(Details("Fonti web",BuildAssistantWeb()));
        sidebar.Children.Add(Details("Routine e automazioni",BuildAssistantRoutines()));
        _assistantContextView=Label("Il contesto pertinente viene aggiunto ai tuoi messaggi.",12);
        sidebar.Children.Add(Details("Contesto",_assistantContextView));
        var activityControls = legacy.Children.OfType<Border>().ToArray();
        if(activityControls.Length>=4)
        {
            var jobs=activityControls[^2];legacy.Children.Remove(jobs);sidebar.Children.Add(Details("Lavori e checkpoint",jobs));
            var result=activityControls[^1];legacy.Children.Remove(result);sidebar.Children.Add(Details("Report completi e fonti",result));
        }
        sidebar.Children.Add(Action("Prepara il mio prossimo risultato",(_,_)=>{OpenAssistant();_assistantWindow?.SetMessage("Dalle fonti disponibili individua il prossimo risultato utile, le informazioni mancanti e criteri verificabili. Non inventare dati.");}));
        foreach(var disclosure in sidebar.Children.OfType<Expander>())disclosure.Expanding+=(_,_)=>{_unifiedRoot.ColumnDefinitions[0].Width=new GridLength(310);};
        _unifiedSidebar=new Border {Child=new ScrollViewer{Content=sidebar,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},CornerRadius=new(16),Background=Token("AstraNavy")};
        Grid.SetRow(_unifiedSidebar,1);_unifiedRoot.Children.Add(_unifiedSidebar);

        var center = new Grid {RowSpacing=10};center.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});center.RowDefinitions.Add(new(){Height=GridLength.Auto});
        _graphHost=new Grid {MinHeight=260};ConfigureUnifiedGraph();var graphSurface=new Grid();graphSurface.Children.Add(_graphHost);_graphPlaceholder=Label("Il grafo si carica automaticamente quando apri ASTRA Assistant.",13);_graphPlaceholder.Margin=new Thickness(30);_graphPlaceholder.HorizontalAlignment=HorizontalAlignment.Center;_graphPlaceholder.VerticalAlignment=VerticalAlignment.Center;_graphPlaceholder.IsHitTestVisible=false;graphSurface.Children.Add(_graphPlaceholder);center.Children.Add(new Border{Child=graphSurface,CornerRadius=new(16),BorderThickness=new(1),BorderBrush=Token("AstraIce")});
        var terminal = new StackPanel {Spacing=5};
        var terminalHeader=new Grid();terminalHeader.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});terminalHeader.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        terminalHeader.Children.Add(TeamHeading("›_ LIVE ACTIVITY",12));
        _renderBudgetLabel=Label("",10);Grid.SetColumn(_renderBudgetLabel,1);terminalHeader.Children.Add(_renderBudgetLabel);terminal.Children.Add(terminalHeader);
        _conversationProgress=new ProgressBar {IsIndeterminate=false,Value=0,Height=3};terminal.Children.Add(_conversationProgress);
        _activityTerminal=new TextBox {IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.NoWrap,Height=148,FontFamily=new FontFamily("Consolas"),FontSize=11,Background=Token("AstraNavy"),Foreground=Token("AstraIce"),BorderThickness=new(0),Padding=new(10)};
        terminal.Children.Add(_activityTerminal);Grid.SetRow(terminal,1);center.Children.Add(terminal);Grid.SetRow(center,1);Grid.SetColumn(center,1);_unifiedRoot.Children.Add(center);
        _conversationHost=new Grid();Grid.SetRow(_conversationHost,1);Grid.SetColumn(_conversationHost,2);
        _unifiedRoot.Children.Add(new Border {Child=_conversationHost,CornerRadius=new(16),Background=Token("AstraNavy")});
        var conversationBorder=(Border)_unifiedRoot.Children.Last();Grid.SetRow(conversationBorder,1);Grid.SetColumn(conversationBorder,2);
        BrainPanel.Children.Add(_unifiedRoot);
        AppRoot.SizeChanged+=(_,_)=>ResizeUnifiedAssistant();
        _unifiedRoot.Loaded+=(_,_)=>{ResizeUnifiedAssistant();UpdateUnifiedVisuals();};
        Closed+=(_,_)=>StopBrainGraph();
        LogAssistantEvent("ready","Conversazione unificata · routing automatico · eventi osservabili");
    }

    private void ResizeUnifiedAssistant()
    {
        if(_unifiedRoot is null)return;
        double width=Math.Max(600,AppRoot.ActualWidth-260);
        _unifiedRoot.Height=Math.Max(620,AppRoot.ActualHeight-220);
        if(OrbitViewHost.Parent is Border orbitSurface)orbitSurface.Height=Math.Max(380,AppRoot.ActualHeight-285);
        _unifiedRoot.ColumnDefinitions[2].Width=new GridLength(width<1100?350:420);
        if(width<1150 && _unifiedSidebar is not null){_unifiedSidebar.Visibility=Visibility.Collapsed;_unifiedRoot.ColumnDefinitions[0].Width=new GridLength(0);}
    }

    private ConversationRoute PlanConversation(string goal)
    {
        var plan=ConversationDispatch.Plan(goal,_conversationMode,_teamSpecialistChoices.Where(c=>c.Box.IsChecked==true).Select(c=>c.Id),_previousConversationRoles);
        return _conversationSimulation?.SelectedItem is SimulationChoice{Id.Length:>0}?plan with{UseTeam=true}:plan;
    }
    private string ConversationSkillMethods(IEnumerable<string> roles) => string.Join("\n\n",roles.Select(id=>AgentCatalog.Prompt(AgentCatalog.Get(id))+"\n"+AgentSkillDefaults.Instructions(_teamStore.Configuration,AgentCatalog.Get(id))));
    private void BeginConversationActivity(string[] roles,string label)
    {
        _conversationWorking=true;_activeConversationRoles=roles;_previousConversationRoles=roles;
        if(_conversationProgress is not null)_conversationProgress.IsIndeterminate=!_preferences.ReducedMotion;
        if(_livePhase is not null)_livePhase.Text=label;
        LogAssistantEvent("route",string.Join(" → ",roles.Select(id=>AgentCatalog.Get(id).Name)));
        var settings=_teamStore.Configuration;LogAssistantEvent("skills","Metodi di ruolo + "+roles.Sum(id=>settings.SkillAssignments.GetValueOrDefault(id,[]).Length)+" skills assegnate");
        SendGraphActivity();
    }
    private void EndConversationActivity()
    {
        _conversationWorking=false;_activeConversationRoles=[];
        if(_conversationProgress is not null){_conversationProgress.IsIndeterminate=false;_conversationProgress.Value=100;}
        if(_livePhase is not null)_livePhase.Text="Pronto · la conoscenza resta collegata";
        SendGraphActivity();
    }
    private async Task<string> RunConversationTeamAsync(string goal,string source,string[] roles,CancellationToken ct)
    {
        var parameters=string.IsNullOrWhiteSpace(_conversationSimulationParameters?.Text)?new Dictionary<string,double>():JsonSerializer.Deserialize<Dictionary<string,double>>(_conversationSimulationParameters.Text)??throw new InvalidDataException("Parametri JSON non validi.");
        string simulation=(_conversationSimulation?.SelectedItem as SimulationChoice)?.Id??"";
        if(simulation.Length>0&&SimulationLab.Tools.Single(t=>t.Id==simulation).Parameters.Any()==true&&parameters.Keys.Any(key=>SimulationLab.Tools.Single(t=>t.Id==simulation).Parameters.All(p=>p.Id!=key)))throw new InvalidDataException("Parametro sconosciuto per lo strumento selezionato.");
        if(string.IsNullOrWhiteSpace(goal))goal="Analizza le fonti allegate e produci il risultato richiesto con evidenze verificabili.";
        var job=_teamStore.Enqueue(goal,roles,source+"\n\n"+BuildAssistantContext(),_agentProjectRoot??"",_conversationWeb?.IsChecked==true);
        _teamStore.Update(job.Id,j=>{j.ConversationId=_assistantWindow?.CurrentConversationId??"";j.SimulationId=simulation;j.SimulationParameters=parameters;if(source.Length>0)j.Evidence.Add(new("conversation-source","Conversation and attachments",source,"User-supplied context; previous model replies are UNVERIFIED and cannot prove execution","attachment"));});
        if(_assistantWindow?.CurrentConversationId is {Length:>0} conversationId)_teamStore.Conversations.Put(conversationId,new(job.Id));
        BeginConversationActivity(roles,"Analisi → specialisti → verifica → consegna");
        _selectedTeamJob=job.Id;_unifiedJobFingerprint="";StartTeamWorker();RefreshTeamJobs();
        LogAssistantEvent("queued","Incarico persistente "+job.Id+" · "+job.Steps.Count+" passaggi");
        try
        {
            while(true)
            {
                ct.ThrowIfCancellationRequested();
                var current=await Task.Run(()=>_teamStore.Records.Get(job.Id),ct)??throw new IOException("Incarico non trovato.");
                UpdateConversationJob(current);
                if(current.Status=="Completed")
                {
                    if(current.ConversationId.Length>0&&!current.ConversationDelivered){await Task.Delay(300,ct);continue;}
                    LogAssistantEvent("done","Consegna completata · revisione "+(current.QualityNeedsReview?"da verificare":"eseguita dal modello"));
                    if(_brainGraphView is not null){try{await SendBrainGraphAsync();}catch(OperationCanceledException) when(!ct.IsCancellationRequested){}}
                    if(current.ConversationDelivered)_assistantWindow?.MarkResponsePersisted();return AgentConversationDelivery.Output(current);
                }
                if(current.Status is "Failed" or "Cancelled")throw new InvalidOperationException(current.Error.Length>0?current.Error:"Incarico "+current.Status);
                await Task.Delay(TimeSpan.FromSeconds(2),ct);
            }
        }
        catch(OperationCanceledException){if(!_windowClosed){_teamStore.Cancel(job.Id);LogAssistantEvent("cancel","Incarico annullato su richiesta");}throw;}
        catch{LogAssistantEvent("error","Il lavoro richiede attenzione; dettagli nella conversazione e nel report");throw;}
        finally{EndConversationActivity();}
    }
    private void RefreshUnifiedActivity()
    {
        if(_selectedTeamJob is not null && _teamStore.Records.Get(_selectedTeamJob) is { } job){bool changed=_unifiedJobFingerprint!=ConversationStamp(job);UpdateConversationJob(job);if(changed&&job.ConversationDelivered&&job.ConversationId==_assistantWindow?.CurrentConversationId)_assistantWindow.RefreshPersistedMessages();}
    }
    private static string ConversationStamp(AgentJob job)=>job.Id+":"+job.Status+":"+string.Join("|",job.Steps.Select(s=>s.AgentId+":"+s.Status))+":"+job.NextAttemptUtc+":"+job.ConversationDelivered;
    private void RestoreConversationWork()
    {
        _previousConversationRoles=[];_selectedTeamJob=null;EndConversationActivity();
        string id=_assistantWindow?.CurrentConversationId??"";
        if(id.Length>0&&_teamStore.Conversations.Get(id) is { } link&&_teamStore.Records.Get(link.JobId) is { } job)
        {_selectedTeamJob=job.Id;_previousConversationRoles=job.Steps.Select(s=>s.AgentId).Where(r=>r is not "coordinator" and not "reviewer").Distinct().ToArray();_unifiedJobFingerprint="";UpdateConversationJob(job);}
    }
    private void UpdateConversationJob(AgentJob job)
    {
        string stamp=ConversationStamp(job);
        if(_unifiedJobFingerprint==stamp)return;_unifiedJobFingerprint=stamp;
        var active=job.Steps.FirstOrDefault(s=>s.Status!="Completed");
        string text=job.Status+" · "+job.Steps.Count(s=>s.Status=="Completed")+"/"+job.Steps.Count+" · "+(active is null?"Consegna":AgentCatalog.Get(active.AgentId).Name);
        if(job.NextAttemptUtc is { } retry)text+=" · prossimo tentativo "+retry.ToLocalTime().ToString("HH:mm:ss");
        LogAssistantEvent("stage",text);_assistantWindow?.ReportProgress(text);
        if(_livePhase is not null)_livePhase.Text=text;
        _conversationWorking=job.Status=="Running";_activeConversationRoles=active is null?[]:[active.AgentId];
        if(_conversationProgress is not null){_conversationProgress.IsIndeterminate=_conversationWorking&&!_preferences.ReducedMotion;if(!_conversationWorking)_conversationProgress.Value=job.Steps.Count==0?0:100d*job.Steps.Count(s=>s.Status=="Completed")/job.Steps.Count;}
        SendGraphActivity();
    }
    private void LogAssistantEvent(string kind,string message)
    {
        var item=new AssistantActivityEvent(DateTimeOffset.UtcNow,kind,message);
        if(_activityTerminal is not null){_activityTerminal.Text+=$"{item.At.ToLocalTime():HH:mm:ss} [{kind.ToUpperInvariant()}] {message}\n";_activityTerminal.SelectionStart=_activityTerminal.Text.Length;}
        try{_activityRecords.Put(Guid.NewGuid().ToString("N"),item);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){StartupDiagnostics.Record("Assistant.activity",ex.GetType().Name);}
    }
    private void UpdateUnifiedVisuals()
    {
        if(_unifiedRoot is null || _windowClosed)return;
        bool graph=_windowActive&&_v4LastPage=="brain";
        if(_conversationProgress is not null)_conversationProgress.IsIndeterminate=graph&&_conversationWorking&&!_preferences.ReducedMotion;
        if(!graph)StopBrainGraph();
        else {if(_assistantWindow is null)OpenAssistant();if(_loadBrainGraph is not null)_= _loadBrainGraph();}
        if(!_windowActive||_v4LastPage!="orbit")Stop3DCore();
        else if(!_orbitActive&&!_orbitInitializing)_=Start3DAsync();
        _renderBudgetLabel!.Text=graph?$"AUTO · {CurrentRenderBudget.GraphNodes} nodi · {CurrentRenderBudget.FramesPerSecond} FPS":"Renderer rilasciato";
    }
}
