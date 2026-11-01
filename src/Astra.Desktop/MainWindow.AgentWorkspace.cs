using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly AgentSkillStore _agentSkillStore=new();
    private readonly List<AgentSkill> _agentSkills=new();
    private AgentSkill? _selectedAgentSkill;
    private string? _agentProjectRoot;
    private ListView? _agentProjectFiles;
    private TextBox? _agentProjectGoal,_agentScriptArguments;
    private TextBlock? _agentProjectStatus;
    private TextBox? _agentRunOutput;
    private NumberBox? _agentTimeout;
    private PreparedChangePlan? _agentChangePlan;
    private string? _agentLastBackup;
    private CancellationTokenSource? _agentOperation;
    private bool _agentBusy;
    private TextBlock? _agentApprovalPreview;
    private ScrollViewer AgentCodePreview(string text)
    {
        var code=new TextBlock{Text=text,IsTextSelectionEnabled=true,TextWrapping=TextWrapping.NoWrap,FontFamily=new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),FontSize=12};
        _agentApprovalPreview=code;
        return new ScrollViewer{Content=code,Height=Math.Clamp(AppRoot.ActualHeight*.3,120,300),MinHeight=120,Padding=new Thickness(10),HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
    private StackPanel BuildAgentSkills()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(UiText.T("Skills installabili"),22));panel.Children.Add(Label(UiText.T("Le skills aggiungono metodi di lavoro e istruzioni. Le capacità dichiarate non concedono da sole il permesso di modificare file, avviare processi o inviare dati."),13));
        try
        {
            foreach(var path in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory,"AssistantSkills"),"*.json"))_agentSkills.Add(AgentSkillRules.Import(File.ReadAllText(path)));
            foreach(var skill in _agentSkillStore.Load()){_agentSkills.RemoveAll(s=>s.Id==skill.Id);_agentSkills.Add(skill);}
        }catch(Exception ex){panel.Children.Add(Label(ex.Message,12));}
        var choose=new ComboBox{Header=UiText.T("Scegli una skill"),DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};var description=Label("",13);var details=Label("",11);panel.Children.Add(choose);panel.Children.Add(description);panel.Children.Add(new Expander{Header=UiText.T("Istruzioni e capacità della skill"),Content=details});
        void Refresh(){choose.ItemsSource=_agentSkills.OrderBy(s=>s.Category).ThenBy(s=>s.Name).ToArray();choose.SelectedIndex=0;}
        choose.SelectionChanged+=(_,_)=>{if(choose.SelectedItem is AgentSkill skill){description.Text=UiText.T(skill.Description);details.Text=skill.Id+"\n"+string.Join(", ",skill.Capabilities)+"\n\n"+skill.Instructions;}};
        panel.Children.Add(Action(UiText.T("Usa questa skill nella chat"),(_,_)=>{if(choose.SelectedItem is AgentSkill skill){_selectedAgentSkill=skill;OpenAssistant();_assistantWindow?.SetSkill(skill);}}));
        panel.Children.Add(Action(UiText.T("Installa skill da JSON o SKILL.md…"),async(_,_)=>
        {
            try
            {
                var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".json");picker.FileTypeFilter.Add(".md");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is null)return;string text=await File.ReadAllTextAsync(file.Path);
                var skill=file.FileType.Equals(".md",StringComparison.OrdinalIgnoreCase)?AgentSkillRules.FromMarkdown(Path.GetFileNameWithoutExtension(file.Name),text):AgentSkillRules.Import(text);
                var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Installare questa skill?"),Content=skill.Name+"\n\n"+skill.Description+"\n\n"+UiText.T("Non verrà eseguito alcuno script. Le azioni richiedono le normali autorizzazioni."),PrimaryButtonText=UiText.T("Installa"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};
                if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
                _agentSkillStore.Install(skill);_agentSkills.RemoveAll(s=>s.Id==skill.Id);_agentSkills.Add(skill);Refresh();choose.SelectedItem=skill;
            }catch(Exception ex){description.Text=UiText.T(ex.Message);}
        }));
        panel.Children.Add(Action(UiText.T("Esporta skill selezionata…"),async(_,_)=>{if(choose.SelectedItem is AgentSkill skill)await SaveLabFileAsync(skill.Id,".json",AgentSkillRules.Export(skill));}));
        panel.Children.Add(Action(UiText.T("Disattiva skill"),(_,_)=>{_selectedAgentSkill=null;_assistantWindow?.SetSkill(null);}));
        var editor=new StackPanel{Spacing=8};string? editingId=null;
        var name=new TextBox{Header=UiText.T("Nome skill")};var summary=new TextBox{Header=UiText.T("Descrizione della skill"),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap};
        var instructions=new TextBox{Header=UiText.T("Istruzioni della skill"),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=180,MaxHeight=500};
        var category=new ComboBox{Header=UiText.T("Categoria"),ItemsSource=new[]{"Personal","Coding","Research","Study","STEM","Creator","Gaming","Productivity"},SelectedIndex=0};
        var preferred=new ComboBox{Header=UiText.T("Tipo di risposta consigliato"),ItemsSource=new[]{"Spiega e guida","Riepilogo operativo","Piano e checklist","Traduci nella lingua Windows","Analizza problemi o errori","Estrai informazioni JSON","Trova note collegate","Grafico 3D dai dati","Crea un widget o automazione"}.Select(UiText.T).ToArray(),SelectedIndex=0};
        editor.Children.Add(name);editor.Children.Add(summary);editor.Children.Add(category);editor.Children.Add(preferred);editor.Children.Add(instructions);
        var capabilities=new StackPanel{Spacing=5};var checks=AgentSkillRules.Capabilities.Select(c=>new CheckBox{Content=c,IsChecked=c=="chat"}).ToArray();foreach(var check in checks)capabilities.Children.Add(check);
        capabilities.Children.Add(Label(UiText.T("Le capacità descrivono l'uso previsto. Non autorizzano da sole script, modifiche ai file o invii di dati."),12));editor.Children.Add(new Expander{Header=UiText.T("Capacità dichiarate"),Content=capabilities});
        editor.Children.Add(Action(UiText.T("Carica selezionata nell'editor"),(_,_)=>
        {
            if(choose.SelectedItem is not AgentSkill skill)return;
            editingId=_agentSkillStore.Load().Any(s=>s.Id==skill.Id)?skill.Id:null;name.Text=skill.Name;summary.Text=skill.Description;instructions.Text=skill.Instructions;category.SelectedItem=skill.Category;if(category.SelectedIndex<0)category.SelectedIndex=0;preferred.SelectedIndex=skill.PreferredTask;
            foreach(var check in checks)check.IsChecked=skill.Capabilities.Contains((string)check.Content);
        }));
        bool drafting=false;
        editor.Children.Add(Action(UiText.T("Genera bozza di istruzioni con OpenRouter"),async(_,_)=>
        {
            if(drafting)return;drafting=true;
            try{if(string.IsNullOrWhiteSpace(name.Text)&&string.IsNullOrWhiteSpace(summary.Text))throw new InvalidDataException(UiText.T("Descrivi prima il compito della skill."));instructions.Text=await RunAssistantTaskAsync("Draft practical reusable skill instructions for ASTRA Assistant with Obsidian knowledge and online specialist models. Specify inputs, method, output and validation criteria. Do not claim permissions or authorize script execution or filesystem changes. Return plain text only.",name.Text+"\n"+summary.Text,false,_windowLifetime.Token);}
            catch(Exception ex){description.Text=UiText.T(ex.Message);}finally{drafting=false;}
        }));
        editor.Children.Add(Action(UiText.T("Salva skill custom"),(_,_)=>
        {
            try
            {
                var skill=new AgentSkill(1,editingId??("org.astra.user."+Guid.NewGuid().ToString("N")),name.Text.Trim(),summary.Text.Trim(),(string?)category.SelectedItem??"Personal",instructions.Text,checks.Where(c=>c.IsChecked==true).Select(c=>(string)c.Content).ToArray(),preferred.SelectedIndex);
                AgentSkillRules.Validate(skill);_agentSkillStore.Install(skill);_agentSkills.RemoveAll(s=>s.Id==skill.Id);_agentSkills.Add(skill);editingId=skill.Id;Refresh();choose.SelectedItem=skill;description.Text=UiText.T("Skill custom salvata sul PC. Puoi usarla nella chat o esportarla.");
            }catch(Exception ex){description.Text=UiText.T(ex.Message);}
        }));
        editor.Children.Add(Action(UiText.T("Nuova skill vuota"),(_,_)=>{editingId=null;name.Text="";summary.Text="";instructions.Text="";preferred.SelectedIndex=0;foreach(var check in checks)check.IsChecked=(string)check.Content=="chat";}));
        panel.Children.Add(new Expander{Header=UiText.T("Crea o modifica una skill custom"),Content=editor});
        panel.Children.Add(Action(UiText.T("Rimuovi skill personale selezionata"),(_,_)=>
        {
            try{if(choose.SelectedItem is not AgentSkill skill||!_agentSkillStore.Load().Any(s=>s.Id==skill.Id))return;_agentSkillStore.Remove(skill);_agentSkills.RemoveAll(s=>s.Id==skill.Id);if(_selectedAgentSkill?.Id==skill.Id){_selectedAgentSkill=null;_assistantWindow?.SetSkill(null);}Refresh();}catch(Exception ex){description.Text=UiText.T(ex.Message);}
        }));
        Refresh();return panel;
    }
    private StackPanel BuildAgentProject()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(UiText.T("Spazio di lavoro progetto"),22));panel.Children.Add(Label(UiText.T("Scegli una cartella per leggere il codice, preparare un piano e proporre modifiche. Nessuna modifica o esecuzione automatica senza una conferma riferita all'azione concreta."),13));
        _agentProjectStatus=Label(UiText.T("Nessuna cartella autorizzata."),12);panel.Children.Add(_agentProjectStatus);
        panel.Children.Add(Action(UiText.T("Scegli cartella di progetto…"),async(_,_)=>
        {
            try{var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var folder=await picker.PickSingleFolderAsync();if(folder is null)return;_agentProjectRoot=SafeFileOperations.DirectoryPath(folder.Path);_agentChangePlan=null;_agentProjectFiles!.ItemsSource=await Task.Run(()=>ProjectWorkspaceTools.List(_agentProjectRoot));_agentProjectStatus.Text=_agentProjectRoot;}catch(Exception ex){_agentProjectStatus.Text=UiText.T(ex.Message);}
        }));
        _agentProjectFiles=new ListView{MaxHeight=220,SelectionMode=ListViewSelectionMode.Single};panel.Children.Add(_agentProjectFiles);
        panel.Children.Add(Action(UiText.T("Leggi il file selezionato nella chat"),async(_,_)=>
        {
            try{if(_agentProjectRoot is null||_agentProjectFiles.SelectedItem is not string relative)return;string source=await Task.Run(()=>ProjectWorkspaceTools.Read(_agentProjectRoot,relative));OpenAssistant("Project · "+relative);_assistantWindow?.SetInput("[PROJECT FILE: "+relative+"]\n"+source);_assistantWindow?.SetMessage(UiText.T("Analizza questo file e proponi un piano di miglioramento verificabile."));}catch(Exception ex){_agentProjectStatus.Text=UiText.T(ex.Message);}
        }));
        _agentProjectGoal=new TextBox{Header=UiText.T("Obiettivo della modifica"),AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=100,MaxHeight=400};panel.Children.Add(_agentProjectGoal);
        panel.Children.Add(Action(UiText.T("Genera proposta per il file selezionato"),async(_,_)=>await PrepareAgentEditsAsync()));
        panel.Children.Add(Action(UiText.T("Rivedi e approva la proposta…"),async(_,_)=>await ApproveAgentEditsAsync()));
        panel.Children.Add(Action(UiText.T("Ripristina ultimo backup…"),async(_,_)=>await RestoreAgentBackupAsync()));
        panel.Children.Add(Action(UiText.T("Annulla elaborazione progetto"),(_,_)=>_agentOperation?.Cancel()));
        var scripts=new StackPanel{Spacing=8};scripts.Children.Add(Label(UiText.T("Gli script vengono eseguiti con i permessi del tuo account, senza sandbox. Possono accedere anche a dati fuori dal progetto: controlla codice, interprete e argomenti prima di autorizzare."),12));
        _agentScriptArguments=new TextBox{Header=UiText.T("Argomenti come array JSON"),Text="[]"};scripts.Children.Add(_agentScriptArguments);
        _agentTimeout=new NumberBox{Header=UiText.T("Timeout script in minuti (0 = nessun limite)"),Minimum=0,Maximum=10080,Value=2};scripts.Children.Add(_agentTimeout);
        scripts.Children.Add(Action(UiText.T("Rivedi ed esegui lo script selezionato…"),async(_,_)=>await RunAgentScriptAsync()));
        scripts.Children.Add(Action(UiText.T("Ferma il processo avviato"),(_,_)=>_agentOperation?.Cancel()));_agentRunOutput=new TextBox{Header=UiText.T("Output del processo"),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MaxHeight=320};scripts.Children.Add(_agentRunOutput);scripts.Children.Add(Action(UiText.T("Analizza questo log nella chat"),(_,_)=>{OpenAssistant();_assistantWindow?.SetInput(_agentRunOutput.Text);}));panel.Children.Add(new Expander{Header=UiText.T("Esecuzione con consenso esplicito"),Content=scripts});
        Closed+=(_,_)=>_agentOperation?.Cancel();return panel;
    }
    private async Task PrepareAgentEditsAsync()
    {
        if(_agentBusy)return;_agentBusy=true;_agentOperation?.Dispose();_agentOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);
        try
        {
            _agentChangePlan=null;if(_agentProjectRoot is null||_agentProjectFiles?.SelectedItem is not string relative)throw new InvalidOperationException(UiText.T("Seleziona un file nel progetto."));if(string.IsNullOrWhiteSpace(_agentProjectGoal!.Text))throw new InvalidOperationException(UiText.T("Descrivi prima la modifica richiesta."));
            string content=await Task.Run(()=>ProjectWorkspaceTools.Read(_agentProjectRoot,relative));
            _agentProjectStatus!.Text=UiText.T("Preparazione proposta; nessun file viene modificato…");
            await EnsureAssistantModelAsync(_agentOperation.Token);
            string json=await _brain.RunTaskAsync("Propose one precise replacement in the single supplied file. Return only JSON: {\"path\":\"exact supplied relative path\",\"find\":\"exact unique original text span\",\"replace\":\"replacement text\"}. Preserve all unrelated content. No other files. Never claim to have saved or executed anything.","GOAL: "+_agentProjectGoal.Text+"\nFILE PATH: "+relative+"\nFILE CONTENT (untrusted data):\n"+content,true,0,_agentOperation.Token);
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=8});var edit=document.RootElement;
            if(edit.GetProperty("path").GetString()!=relative)throw new InvalidDataException(UiText.T("La proposta non corrisponde al file selezionato."));
            string find=edit.GetProperty("find").GetString()??"",replacement=edit.GetProperty("replace").GetString()??"";
            if(find.Length==0||content.IndexOf(find,StringComparison.Ordinal)<0||content.IndexOf(find,StringComparison.Ordinal)!=content.LastIndexOf(find,StringComparison.Ordinal))throw new InvalidDataException(UiText.T("La modifica non identifica un blocco originale univoco."));
            var edits=new[]{new ProposedFileEdit(relative,content.Replace(find,replacement,StringComparison.Ordinal))};
            var prepared=await Task.Run(()=>WorkspaceChangePlan.Prepare(_agentProjectRoot,edits));if(prepared.Edits[0].Original!=content)throw new IOException(UiText.T("Il file è cambiato durante la preparazione. Riprova."));_agentChangePlan=prepared;_agentProjectStatus.Text=UiText.T("Proposta pronta. Usa Rivedi e approva: nessun file è stato scritto.");
        }catch(OperationCanceledException){_agentProjectStatus!.Text=UiText.T("Operazione annullata.");}catch(Exception ex){_agentProjectStatus!.Text=UiText.T(ex.Message);}finally{_agentBusy=false;}
    }
    private async Task ApproveAgentEditsAsync()
    {
        if(_agentBusy)return;
        try
        {
            var plan=_agentChangePlan??throw new InvalidOperationException(UiText.T("Genera prima una proposta."));
            var preview=AgentCodePreview(WorkspaceChangePlan.Preview(plan));
            var content=new StackPanel{Spacing=10};content.Children.Add(Label(plan.Root,12));content.Children.Add(Label(UiText.T("Approvando verranno scritti soltanto i file elencati. Gli originali vengono conservati come backup cifrati. Se un file è cambiato dopo l'anteprima, l'operazione si interrompe."),13));content.Children.Add(preview);
            var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Approvare queste modifiche ai file?"),Content=content,PrimaryButtonText=UiText.T("Applica le modifiche mostrate"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close,MaxWidth=1000};
            if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
            _agentBusy=true;string backup=await Task.Run(()=>WorkspaceChangePlan.Apply(plan,Path.Combine(AstraPaths.UserDataRoot,"assistant","change-backups")));_agentLastBackup=backup;_agentChangePlan=null;_agentProjectStatus!.Text=UiText.T("Modifiche applicate. Backup cifrato: ")+backup;
        }catch(Exception ex){_agentProjectStatus!.Text=UiText.T(ex.Message);}finally{_agentBusy=false;}
    }
    private async Task RunAgentScriptAsync()
    {
        if(_agentBusy)return;
        try
        {
            if(_agentProjectRoot is null||_agentProjectFiles?.SelectedItem is not string relative)throw new InvalidOperationException(UiText.T("Seleziona uno script nel progetto."));
            string interpreter=ProjectWorkspaceTools.Interpreter(_agentProjectRoot,relative);string path=WorkspaceChangePlan.Resolve(_agentProjectRoot,relative);if(new FileInfo(path).Length>400000)throw new InvalidDataException(UiText.T("Script oltre 100.000 caratteri."));byte[] snapshot=File.ReadAllBytes(path);string source=new UTF8Encoding(false,true).GetString(snapshot);if(source.Length>100000)throw new InvalidDataException(UiText.T("Script oltre 100.000 caratteri."));string hash=Convert.ToHexString(SHA256.HashData(snapshot));
            var args=JsonSerializer.Deserialize<string[]>(_agentScriptArguments!.Text)??Array.Empty<string>();if(args.Length>32||args.Any(a=>a is null||a.Length>2000))throw new InvalidDataException(UiText.T("Argomenti script non validi."));
            var body=new StackPanel{Spacing=9};body.Children.Add(Label(interpreter+"\n"+path+"\n"+JsonSerializer.Serialize(args),12));body.Children.Add(Label(UiText.T("Esecuzione senza sandbox, con i permessi del tuo account. Lo script può leggere e modificare dati anche fuori dalla cartella scelta. Autorizza soltanto codice che hai verificato."),13));body.Children.Add(AgentCodePreview(source));
            var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Autorizzare questo script?"),Content=body,PrimaryButtonText=UiText.T("Autorizzo questa esecuzione"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close,MaxWidth=1000};if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
            using var approvedScript=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);if(hash!=Convert.ToHexString(SHA256.HashData(approvedScript)))throw new IOException(UiText.T("Lo script è cambiato dopo l'anteprima."));
            _agentBusy=true;_agentOperation?.Dispose();_agentOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);_agentProjectStatus!.Text=UiText.T("Script in esecuzione; usa Ferma per interromperlo.");
            double minutes=double.IsFinite(_agentTimeout!.Value)?Math.Clamp(_agentTimeout.Value,0,10080):2;
            var result=await ProjectWorkspaceTools.RunApprovedAsync(_agentProjectRoot,relative,interpreter,args,_agentOperation.Token,TimeSpan.FromMinutes(minutes));_agentProjectStatus.Text="Exit code: "+result.ExitCode;_agentRunOutput!.Text=result.Output+(result.Truncated?"\n[Output limited to 64 KB]":"");
        }catch(OperationCanceledException){_agentProjectStatus!.Text=UiText.T("Processo interrotto o timeout raggiunto.");}catch(Exception ex){_agentProjectStatus!.Text=UiText.T(ex.Message);}finally{_agentBusy=false;}
    }
    private async Task RestoreAgentBackupAsync()
    {
        if(_agentBusy)return;
        try
        {
            if(_agentProjectRoot is null||_agentLastBackup is null)throw new InvalidOperationException(UiText.T("Nessun backup di questa sessione da ripristinare."));
            var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Ripristinare gli originali?"),Content=_agentProjectRoot+"\n\n"+UiText.T("Ripristina i file dell'ultima modifica approvata. Se sono stati modificati successivamente, il ripristino si interrompe."),PrimaryButtonText=UiText.T("Ripristina gli originali"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
            _agentBusy=true;await Task.Run(()=>WorkspaceChangePlan.Restore(_agentProjectRoot,_agentLastBackup));_agentLastBackup=null;_agentProjectStatus!.Text=UiText.T("Originali ripristinati.");
        }catch(Exception ex){_agentProjectStatus!.Text=UiText.T(ex.Message);}finally{_agentBusy=false;}
    }
}
