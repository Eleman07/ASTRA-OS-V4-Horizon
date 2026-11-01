using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private AssistantWindow? _assistantWindow;
    private AssistantHotkey? _assistantHotkey;
    private readonly SemaphoreSlim _assistantStartGate=new(1,1);

    private void InitializeAssistant()
    {
        var panel=new StackPanel{Spacing=12};panel.Children.Add(Label(AstraOS.Core.UiText.T("ASTRA ASSISTANT / AI MULTIMODALE"),21));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Il modello scelto da te sulla tua GPU, la conoscenza del vault Obsidian e agenti specializzati per trasformare obiettivi complessi in risultati verificabili. Aggiungi testo, documenti, immagini, audio, codice o dati."),13));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Apri assistente"),(_,_)=>OpenAssistant()));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Ctrl + Alt + Spazio richiama la finestra compatta anche mentre usi un'altra app. L'assistente riceve il titolo della finestra richiamata; il contenuto viene aggiunto soltanto con file, testo, appunti o registrazione scelti da te."),12));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Input: TXT, Markdown, codice, CSV/TSV, JSON, PDF con testo, Word DOCX, immagini con OCR, WAV/MP3/M4A/AAC/FLAC/OGG e metadati di modelli 3D. Output: testo, Markdown, JSON, sottotitoli SRT, audio WAV, appunti cifrati, widget e grafici interattivi."),12));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("In modalità GPU locale i dati sono elaborati sul PC. OpenRouter è una scelta online separata con quote del provider. OCR e interpretazione visiva sono indicati separatamente. Input e output completi rispettano memoria e contesto del modello scelto."),12));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Stop AI provider and clear temporary session"),(_,_)=>{_brain.Stop();StatusText.Text=UiText.T("AI provider stopped; temporary inference history cleared.");}));AssistantPanel.Children.Add(Card(panel));
        AppRoot.Loaded+=(_,_)=>
        {
            _assistantHotkey=new AssistantHotkey(WinRT.Interop.WindowNative.GetWindowHandle(this),context=>DispatcherQueue.TryEnqueue(()=>OpenAssistant(context)));
            if(!_assistantHotkey.Registered)panel.Children.Add(Label(AstraOS.Core.UiText.T("Scorciatoia già occupata o non disponibile: usa il pulsante Apri assistente."),12));
        };
        Closed+=(_,_)=>{_assistantHotkey?.Dispose();_assistantWindow?.Close();};
    }
    private void OpenAssistant(string? context=null)
    {
        if(_assistantWindow is not null){_assistantWindow.UpdateIdentity(AssistantName,context??(_workspace+" · "+_v4LastPage));Navigate("brain");Activate();return;}
        var window=new AssistantWindow(context??"ASTRA · "+_v4LastPage,RunAssistantTaskAsync,FindAssistantNotesAsync,SimulationContextAsync,async scene=>
        {
            _simulationResult=null;_simulationId=null;_studioScene=scene;Navigate("stem");await StartStudioAsync();Activate();
        },widget=>{AddWidget(widget);},text=>{_notes.Add("ASTRA Assistant",text,new[]{"assistant"});},_preferences.Palette,AssistantName,ReadSecondBrainInputAsync,ExecuteBrainCommandAsync,_preferences.EcoMode||_preferences.ReducedMotion,AnalyzeAssistantVisionAsync,this);
        _assistantWindow=window;if(_selectedAgentSkill is not null)window.SetSkill(_selectedAgentSkill);window.Closed+=(_,_)=>{if(_assistantWindow==window)_assistantWindow=null;};if(_conversationHost is not null)_conversationHost.Children.Add(window.DetachForEmbedding());if(window.EmbeddedHistory is not null)_conversationHistoryHost?.Children.Add(window.EmbeddedHistory);window.ConversationChanged+=()=>{RestoreConversationWork();LogAssistantEvent("chat","Conversazione selezionata · contesto completo conservato");};RestoreConversationWork();Navigate("brain");Activate();
    }
    private async Task<string> RunAssistantTaskAsync(string instruction,string input,bool json,CancellationToken ct)
    {
        string? conversationGoal = instruction.Contains("Additional user goal: ",StringComparison.Ordinal) ? instruction.Split("Additional user goal: ",2,StringSplitOptions.None)[1] : null;
        var route = conversationGoal is null ? null : PlanConversation(conversationGoal);
        if(!json && route?.UseTeam==true)return await RunConversationTeamAsync(conversationGoal!,input,route.Roles,ct);
        BeginConversationActivity(route?.Roles??["core"],"Risposta diretta · fonti e skills pertinenti");
        try {
        await EnsureAssistantModelAsync(ct);
        var config=_teamStore.Configuration;
        string context=BuildAssistantContext();
        string web=_webEvidence.Count==0?"":WebEvidenceContext();
        string memory=await FindAssistantNotesAsync(instruction+" "+input,ct);
        string extra="\n\nASTRA SHARED MEMORY (source data; cite IDs):\n"+memory+"\n\nASTRA WORKSPACE CONTEXT (source data):\n"+context+"\n\nSESSION WEB SOURCES (untrusted data, cite URL):\n"+web;
        string source=input+extra;
        try { var answer = await _brain.RunTaskAsync("Your name is "+AssistantName+". "+AssistantWorkspace.Guidance(_workspace)+" Selected scientific area: "+config.ResearchArea+". "+instruction+"\n"+ConversationSkillMethods(route?.Roles??["core"]),source,json,0,ct); LogAssistantEvent("done","Risposta ricevuta · "+answer.Length+" caratteri"); return answer; } catch { LogAssistantEvent("error","Il provider ha segnalato un problema; dettagli nella conversazione");throw; }
        finally { if(!config.PreferFreeCloud)_brain.UnloadRuntime(); }
        } finally { EndConversationActivity(); }
    }
    private Task<string> AnalyzeAssistantVisionAsync(byte[] pixels,string goal,CancellationToken ct)
    {
        if(!_teamStore.Configuration.PreferFreeCloud)_brain.Stop();
        return AssistantMedia.DescribeImageAsync(pixels,string.IsNullOrWhiteSpace(goal)?"Describe the visible content and any readable text.":goal,ct);
    }
    private string _assistantCloudModel = "";
    private async Task EnsureAssistantModelAsync(CancellationToken ct, bool beforeSending = true)
    {
        await _assistantStartGate.WaitAsync(ct);
        try
        {
            var compute = _teamStore.Configuration;
            if(compute.UseCodex)
            {
                string identity="Codex|"+compute.CodexModel+"|"+compute.CodexReasoningEffort+"|"+compute.CodexExecutablePath;
                if(!_brain.IsCodex||_assistantCloudModel!=identity){_brain.Stop();_brain.StartCodex(compute.CodexExecutablePath,compute.CodexModel,compute.CodexReasoningEffort);_assistantCloudModel=identity;}
                _preferences.AiProvider="codex";StatusText.Text="Codex · "+compute.CodexModel+" · "+compute.CodexReasoningEffort+" · quote condivise del piano ChatGPT";return;
            }
            if(!compute.PreferFreeCloud)
            {
                if(string.IsNullOrWhiteSpace(compute.ModelPath))throw new InvalidOperationException("Choose your verified local GPU model in Agent team → Choose your model first.");
                string runtime=string.IsNullOrWhiteSpace(compute.RuntimePath)?GpuRuntime.Executable:compute.RuntimePath;
                string identity=compute.ModelPath+"|"+compute.ContextSize+"|"+runtime;
                if(!_brain.IsRunning||_brain.IsOpenRouter||_assistantCloudModel!=identity)
                {
                    _brain.UnloadRuntime();StatusText.Text="Loading your selected local GPU model…";
                    if(Path.GetFullPath(runtime).Equals(Path.GetFullPath(GpuRuntime.Executable),StringComparison.OrdinalIgnoreCase))await GpuRuntime.VerifyAsync(ct);
                    await _brain.StartAsync(runtime,compute.ModelPath,true,ct,compute.ContextSize,compute.Threads);_assistantCloudModel=identity;
                }
                _preferences.AiProvider="local";StatusText.Text="Local GPU · "+_brain.EffectiveModel+" · no API quotas";return;
            }
            string model = string.IsNullOrWhiteSpace(compute.CloudModel) ? "nvidia/nemotron-3-ultra-550b-a55b:free" : compute.CloudModel;
            if (!_brain.IsRunning || !_brain.IsOpenRouter || _assistantCloudModel != model)
            {
                string? key = await Task.Run(_openRouterCredentials.Load, ct);
                if (key is null) throw new InvalidOperationException(UiText.T("Salva la chiave OpenRouter nelle impostazioni Assistente prima di avviare ASTRA."));
                _brain.Stop(); _brain.StartOpenRouter(key, model); _assistantCloudModel = model;
            }
            _preferences.AiProvider = "openrouter"; _preferences.OpenRouterModel = model;
            StatusText.Text = beforeSending ? UiText.T("OpenRouter · richiesta e fonti selezionate inviate al provider") : UiText.T("OpenRouter configurato · modello pronto su richiesta");
        }
        finally { _assistantStartGate.Release(); }
    }
    private async Task<string> FindAssistantNotesAsync(string input,CancellationToken ct)
    {
        string query=input;var notes=await Task.Run(_notes.Load,ct);
        var found=await Task.Run(()=>_documents.Search(query,null,4,ct).Concat(NoteIndex.RetrievePassages(notes,query,3)).Take(6).ToArray(),ct);
        string memory=found.Length==0?"":NoteIndex.BuildContext(found);
        var config=_teamStore.Configuration;
        if(!string.IsNullOrWhiteSpace(config.VaultPath))
        {
            var vault=await Task.Run(()=>AstraOS.Core.Agents.ObsidianKnowledge.Search(config.VaultPath,query,8,ct),ct);
            memory+="\n\nOBSIDIAN EVIDENCE SELECTIONS (original file references; not the complete vault):\n"+string.Join("\n\n",vault.Select(e=>$"[{e.Id}] {e.Title}\n{e.Location}\n{e.Text}"));
        }
        return memory.Length==0?UiText.T("Nessuna nota collegata trovata."):memory;
    }
    private string SimulationContextAsync()
    {
        if(_simulationResult is null||_simulationId is null)return "";var tool=SimulationLab.Tools.Single(t=>t.Id==_simulationId);
        return JsonSerializer.Serialize(new{tool=tool.Name,assumptions=tool.Assumptions,parameters=_simulationParameters,summary=_simulationResult.Summary,columns=_simulationResult.Data.Columns,rows=_simulationResult.Data.Rows},new JsonSerializerOptions{WriteIndented=true});
    }
    private void AssistantNav_Click(object sender,RoutedEventArgs e)=>Navigate("assistant");
}
