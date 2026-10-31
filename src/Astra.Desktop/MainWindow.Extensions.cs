using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using AstraOS.Core;
using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;
using AstraOS.Desktop.Plugins;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private readonly EncryptedNoteStore _notes = new();
    private readonly LocalLlmSession _brain = new();
    private readonly OpenRouterCredentialStore _openRouterCredentials = new();
    private readonly PluginHost _plugins = new();
    private IReadOnlyList<(DeclarativePlugin Plugin,string Folder)>? _recipeCache;
    private int _pluginRenderRevision;
    private bool _loadingAiSettings;
    private bool _orbitActive;
    // Instantiate WebView2 only when the user enables the optional Orbital scene.
    private WebView2? _orbitView;
    private int _orbitGeneration;
    private bool _orbitInitializing;
    private bool _orbitReady;
    private IReadOnlyDictionary<string,ExplorerEntry> _desktopOrbitEntries = new Dictionary<string,ExplorerEntry>();
    private CancellationTokenSource? _inferenceCancellation;
    private CancellationTokenSource? _aiStartupCancellation;

    private void InitializeExtensions()
    {
        Closed += (_, _) => { _aiStartupCancellation?.Cancel(); _inferenceCancellation?.Cancel(); Stop3DCore(); _brain.Dispose(); };
        IsoAppPath.Text = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var selection=LocalAiPaths.Resolve(AppContext.BaseDirectory,_preferences.AiExecutablePath,_preferences.AiModelPath);
        ApplyAssistantAiSelection(selection,true);
        InitializeOpenRouterModels();
        AiRuntimePath.TextChanged+=(_,_)=>RefreshAssistantRuntimeUi();
        RefreshAssistantRuntimeUi();
    }

    private void ApplyAssistantAiSelection(LocalAiSelection selection,bool save)
    {
        AiRuntimePath.Text=selection.Runtime;AiModelPath.Text=selection.Model;
        if(save&&(_preferences.AiExecutablePath!=selection.Runtime||_preferences.AiModelPath!=selection.Model))
        {
            _preferences.AiExecutablePath=selection.Runtime;_preferences.AiModelPath=selection.Model;
            try{_preferenceStore.Save(_preferences);}catch(Exception ex){StartupDiagnostics.Record("AI.paths.save",ex.Message);}
        }
        RefreshAssistantRuntimeUi();
    }
    private void RefreshAssistantRuntimeUi()
    {
        _preferences.AiProvider="openrouter";UseAiGpuToggle.IsOn=false;UseAiGpuToggle.IsEnabled=false;
        AiUseBundledButton.Visibility=Visibility.Collapsed;AiUseBundledLegacyButton.Visibility=Visibility.Collapsed;L10n0226.Visibility=Visibility.Collapsed;
        AiStartButton.Content=UiText.T("Avvia OpenRouter");
    }
    private void InitializeOpenRouterModels()
    {
        _loadingAiSettings=true;
        AiModelChoice.Items.Clear();
        AiModelChoice.Items.Add(new ComboBoxItem { Content=UiText.T("NVIDIA Nemotron 3 Ultra · gratuito"),Tag="nvidia/nemotron-3-ultra-550b-a55b:free" });
        if(_preferences.OpenRouterModel!="nvidia/nemotron-3-ultra-550b-a55b:free")
            AiModelChoice.Items.Add(new ComboBoxItem { Content=_preferences.OpenRouterModel,Tag=_preferences.OpenRouterModel });
        AiModelChoice.SelectedIndex=AiModelChoice.Items.Count>1?1:0;
        AiModelChoice.SelectionChanged+=(_,_)=>
        {
            if(_loadingAiSettings||AiModelChoice.SelectedItem is not ComboBoxItem item||item.Tag is not string id)return;
            _preferences.OpenRouterModel=id;
            try{_preferenceStore.Save(_preferences);}catch(Exception ex){StartupDiagnostics.Record("OpenRouter.model.save",ex.Message);}
            if(_brain.IsOpenRouter)_brain.Stop();
        };
        _loadingAiSettings=false;
        if(_preferences.AiProvider is not("openrouter" or "local"))_preferences.AiProvider="openrouter";
        RefreshAssistantRuntimeUi();
        AiStatusValue.Text=_openRouterCredentials.HasCredential?UiText.T("Chiave OpenRouter salvata. ASTRA non richiede Zero Data Retention (ZDR); i provider potrebbero conservare o usare i prompt secondo le proprie policy."):UiText.T("Aggiungi una chiave API OpenRouter per attivare il router dei modelli gratuiti.");
    }
    private void SaveOpenRouterKey_Click(object sender,RoutedEventArgs e)
    {
        string key=AiOpenRouterKey.Password;
        try
        {
            _openRouterCredentials.Save(key);AiOpenRouterKey.Password="";_preferences.AiProvider="openrouter";
            _preferenceStore.Save(_preferences);_brain.Stop();RefreshAssistantRuntimeUi();
            AiStatusValue.Text=UiText.T("Chiave OpenRouter salvata con Windows DPAPI. Non è ancora stata inviata alcuna richiesta.");
        }
        catch(Exception ex){AiStatusValue.Text=ex.Message;}
    }
    private void UseSavedOpenRouter_Click(object sender,RoutedEventArgs e)
    {
        if(!_openRouterCredentials.HasCredential){AiStatusValue.Text=UiText.T("Salva prima una chiave API OpenRouter.");return;}
        _aiStartupCancellation?.Cancel();_inferenceCancellation?.Cancel();_brain.Stop();_preferences.AiProvider="openrouter";
        _preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();AiStatusValue.Text=UiText.T("OpenRouter è selezionato. Non è ancora stata inviata alcuna richiesta.");
    }
    private void ForgetOpenRouterKey_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            _brain.Stop();_openRouterCredentials.Delete();AiOpenRouterKey.Password="";
            _preferences.AiProvider="openrouter";_preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();
            AiStatusValue.Text=UiText.T("Chiave OpenRouter eliminata. Salva una chiave per riprendere le richieste online.");
        }
        catch(Exception ex){AiStatusValue.Text=UiText.T("Impossibile eliminare la chiave OpenRouter: ")+ex.Message;}
    }
    private void UseLocalAi_Click(object sender,RoutedEventArgs e)
    {
        _aiStartupCancellation?.Cancel();_inferenceCancellation?.Cancel();_brain.Stop();
        _preferences.AiProvider="openrouter";_preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();
        AiStatusValue.Text=UiText.T("AI locale selezionata. Avviala esplicitamente o invia una richiesta per caricare il modello incluso.");
    }
    private async void RefreshOpenRouterModels_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            string? key=_openRouterCredentials.Load();if(key is null)throw new InvalidOperationException(UiText.T("Salva prima una chiave API OpenRouter."));
            AiStatusValue.Text=UiText.T("Ricerca dei modelli gratuiti…");
            var models=await LocalLlmSession.GetFreeModelsAsync(key,_windowLifetime.Token);
            _loadingAiSettings=true;AiModelChoice.Items.Clear();
            AiModelChoice.Items.Add(new ComboBoxItem{Content=UiText.T("NVIDIA Nemotron 3 Ultra · gratuito"),Tag="nvidia/nemotron-3-ultra-550b-a55b:free"});
            foreach(var model in models)AiModelChoice.Items.Add(new ComboBoxItem{Content=model.Name+" · "+model.Id,Tag=model.Id});
            int selected=0;for(int i=0;i<AiModelChoice.Items.Count;i++)if(AiModelChoice.Items[i] is ComboBoxItem item&&item.Tag as string==_preferences.OpenRouterModel)selected=i;
            AiModelChoice.SelectedIndex=selected;_loadingAiSettings=false;
            if(selected==0&&_preferences.OpenRouterModel!="nvidia/nemotron-3-ultra-550b-a55b:free")_preferences.OpenRouterModel="nvidia/nemotron-3-ultra-550b-a55b:free";
            _preferenceStore.Save(_preferences);
            AiStatusValue.Text=models.Count==0?UiText.T("Nessun modello gratuito fisso trovato al momento. Prova il routing automatico."):UiText.F("Trovati {0} modelli gratuiti.",models.Count);
        }
        catch(Exception ex){_loadingAiSettings=false;AiStatusValue.Text=ex.Message;}
    }
    private void UseBundledAi_Click(object sender,RoutedEventArgs e)
    {
        _aiStartupCancellation?.Cancel();_inferenceCancellation?.Cancel();_brain.Stop();
        _preferences.AiProvider="openrouter";
        ApplyAssistantAiSelection(LocalAiPaths.Resolve(AppContext.BaseDirectory,null,null),true);
        _preferenceStore.Save(_preferences);RefreshAssistantRuntimeUi();
        AiStatusValue.Text=UiText.T("AI inclusa selezionata. Premi Avvia oppure invia un messaggio nella conversazione.");
    }

    private void ExtensionNavigation(string page)
    {
        BrainPanel.Visibility = page == "brain" ? Visibility.Visible : Visibility.Collapsed;
        PluginsPanel.Visibility = page == "plugins" ? Visibility.Visible : Visibility.Collapsed;
        OrbitPanel.Visibility = page == "orbit" ? Visibility.Visible : Visibility.Collapsed;
        IsoPanel.Visibility = page == "iso" ? Visibility.Visible : Visibility.Collapsed;
        if (page != "orbit") Stop3DCore();
        BrainNav.Background = page == "brain" ? Brush("#1B3B64") : Brush("#0B1527");
        PluginsNav.Background = page == "plugins" ? Brush("#1B3B64") : Brush("#0B1527");
        OrbitNav.Background = page == "orbit" ? Brush("#1B3B64") : Brush("#0B1527");
        IsoNav.Background = page == "iso" ? Brush("#1B3B64") : Brush("#0B1527");
    }
    private void BrainNav_Click(object sender, RoutedEventArgs e) { Navigate("brain"); RefreshNotes(); }
    private async void PluginsNav_Click(object sender, RoutedEventArgs e) { Navigate("plugins"); await RenderPluginsAsync(); }
    private void OrbitNav_Click(object sender, RoutedEventArgs e) => Navigate("orbit");
    private void IsoNav_Click(object sender, RoutedEventArgs e) => Navigate("iso");

    // The default model runs in process; only an explicitly selected external runtime uses localhost.
    private async void StartAi_Click(object sender, RoutedEventArgs e)
    {
        if (_brain.IsRunning) { AiStatusValue.Text = AstraOS.Core.UiText.T("Il modello è già in esecuzione."); return; }
        _aiStartupCancellation?.Dispose();
        _aiStartupCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        AiStartButton.IsEnabled = false;
        AiStatusValue.Text = AstraOS.Core.UiText.T("Caricamento del modello locale…");
        try
        {
            await EnsureAssistantModelAsync(_aiStartupCancellation.Token,beforeSending:false);
            AiStatusValue.Text = _brain.IsOpenRouter?UiText.T("OpenRouter configurato. Non è stato inviato alcun prompt; la prima inferenza richiede il consenso di sessione."):_brain.IsRunning?AstraOS.Core.UiText.T("AI locale pronta · nessun servizio cloud"):UiText.T("Provider locale pronto.");
        }
        catch (OperationCanceledException) { AiStatusValue.Text = AstraOS.Core.UiText.T("Avvio del modello annullato."); }
        catch (Exception ex) { AiStatusValue.Text = AstraOS.Core.UiText.T("Avvio non riuscito: ") + ex.Message; }
        finally { AiStartButton.IsEnabled = true; }
    }
    private void StopAi_Click(object sender, RoutedEventArgs e)
    {
        _aiStartupCancellation?.Cancel();
        _inferenceCancellation?.Cancel();
        _brain.Stop();
        AiStatusValue.Text = AstraOS.Core.UiText.T("Motore arrestato; cronologia temporanea eliminata dalla sessione ASTRA.");
    }
    private void ClearAi_Click(object sender, RoutedEventArgs e)
    {
        _brain.ClearConversation(); AiAnswer.Text = AstraOS.Core.UiText.T("Conversazione in memoria cancellata (non modifica le note cifrate).");
    }
    private async void AskAi_Click(object sender, RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(AiQuestion.Text)){AiAnswer.Text=UiText.T("Scrivi una domanda o un comando.");return;}
        _inferenceCancellation?.Dispose();
        _inferenceCancellation = new CancellationTokenSource();
        AiAskButton.IsEnabled = false;
        AiCancelButton.IsEnabled = true;
        AiAnswer.Text = AstraOS.Core.UiText.T("Elaborazione in corso…");
        try
        {
            var command=await ExecuteBrainCommandAsync(AiQuestion.Text,_inferenceCancellation.Token);
            if(command is not null){AiAnswer.Text=command;return;}
            await EnsureAssistantModelAsync(_inferenceCancellation.Token);
            string prefix = BrainMode.SelectedIndex switch
            {
                1 => "Prepara un riassunto strutturato con idee principali e limiti. ",
                2 => "Genera flashcard con domanda e risposta, adatte a ripasso attivo. ",
                3 => "Spiega tecnicamente e per passaggi, indicando ipotesi e formule se pertinenti. ",
                4 => "Crea un piano operativo con obiettivi, attività e criteri verificabili. ",
                _ => ""
            };
            var pdfPassages=await FindBrainPassagesAsync(AiQuestion.Text,_inferenceCancellation.Token);
            AiAnswer.Text = await _brain.AskAsync(prefix + AiQuestion.Text, await Task.Run(_notes.Load), _inferenceCancellation.Token,pdfPassages,AssistantName,BuildAssistantContext());
            if(pdfPassages.Count>0)AiAnswer.Text+="\n\n"+UiText.T("Fonti PDF:")+"\n"+string.Join("\n",pdfPassages.Select(p=>p.Title).Distinct());
        }
        catch (OperationCanceledException) { AiAnswer.Text = AstraOS.Core.UiText.T("Richiesta annullata."); }
        catch (Exception ex) { AiAnswer.Text = AstraOS.Core.UiText.T("Second Brain: ") + ex.Message; }
        finally { AiAskButton.IsEnabled = true; AiCancelButton.IsEnabled = false; }
    }
    private async void PickAiFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            bool runtime = sender is FrameworkElement { Tag: "runtime" };
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(runtime ? ".exe" : ".gguf");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            var file = await picker.PickSingleFileAsync(); if (file is null || _windowClosed) return;
            if (runtime && !file.Name.Equals("llama-server.exe", StringComparison.OrdinalIgnoreCase))
            { AiStatusValue.Text = AstraOS.Core.UiText.T("Scegli llama-server.exe, il server locale di llama.cpp."); return; }
            if (runtime) { AiRuntimePath.Text = file.Path; _preferences.AiExecutablePath = file.Path; UseAiGpuToggle.IsEnabled=true; }
            else { AiModelPath.Text = file.Path; _preferences.AiModelPath = file.Path; }
            _preferenceStore.Save(_preferences);
            AiStatusValue.Text = AstraOS.Core.UiText.T("File selezionato e percorso salvato. L'AI si avvia soltanto premendo Avvia modello locale.");
        }
        catch (Exception ex) { if (!_windowClosed) AiStatusValue.Text = AstraOS.Core.UiText.T("Selezione file non riuscita: ") + ex.Message; }
    }
    private void CancelAi_Click(object sender, RoutedEventArgs e) => _inferenceCancellation?.Cancel();
    private async void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        var title = AiNoteTitle.Text;
        var content = AiNoteContent.Text;
        var tags = AiNoteTags.Text.Split(',');
        SaveNoteButton.IsEnabled = false;
        try
        {
            await Task.Run(() => _notes.Add(title, content, tags));
            AiNoteTitle.Text = ""; AiNoteContent.Text = ""; AiNoteTags.Text = "";
            RefreshNotes();
            AiStatusValue.Text = AstraOS.Core.UiText.T("Nota cifrata localmente con Windows DPAPI (utente corrente).");
        }
        catch (Exception ex) { AiStatusValue.Text = AstraOS.Core.UiText.T("Impossibile salvare nota: ") + ex.Message; }
        finally { SaveNoteButton.IsEnabled = true; }
    }
    private async void ImportNote_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.GetFullPath(AiImportPath.Text.Trim().Trim('"'));
            if(Path.GetExtension(path).Equals(".pdf",StringComparison.OrdinalIgnoreCase)){await ImportBrainPdfAsync(path);AiImportPath.Text="";return;}
            bool archive = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase);
            if (!File.Exists(path) || !new[] { ".txt", ".md", ".json" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException(AstraOS.Core.UiText.T("Seleziona un file .txt/.md o un archivio ASTRA .json."));
            if (new FileInfo(path).Length > (archive ? 16_000_000 : 16_000)) throw new InvalidDataException(AstraOS.Core.UiText.T("File superiore al limite consentito."));
            await Task.Run(() =>
            {
                if (archive) _notes.ImportArchive(File.ReadAllText(path));
                else _notes.Add(Path.GetFileName(path), File.ReadAllText(path));
            });
            AiImportPath.Text = "";
            RefreshNotes();
            AiStatusValue.Text = AstraOS.Core.UiText.T("File importato e cifrato. Nessuna cartella viene scansionata automaticamente.");
        }
        catch (Exception ex) { AiStatusValue.Text = AstraOS.Core.UiText.T("Importazione non riuscita: ") + ex.Message; }
    }
    // V3: lexical passage retrieval only; never executes text from notes or reaches network.
    private async void SearchNotes_Click(object sender, RoutedEventArgs e)
    {
        var query = NoteSearchQuery.Text.Trim();
        if (query.Length < 2 || query.Length > 240)
        { NoteSearchResults.Text = AstraOS.Core.UiText.T("Scrivi un termine di ricerca (2–240 caratteri)."); return; }
        var searchButton = sender as Button;
        if (searchButton is not null) searchButton.IsEnabled = false;
        NoteSearchResults.Text = AstraOS.Core.UiText.T("Ricerca locale in corso…");
        try
        {
            var notes=await Task.Run(()=>NoteIndex.RetrievePassages(_notes.Load(),query,3));
            var pdf=await FindBrainPassagesAsync(query,_windowLifetime.Token);
            var passages=pdf.Concat(notes).Take(7).ToArray();
            NoteSearchResults.Text = passages.Length == 0
                ? AstraOS.Core.UiText.T("Nessun risultato. La ricerca è offline e non modifica le note.")
                : string.Join("\n\n", passages.Select((x, i) =>
                    $"{i+1}. {x.Title} · [NOTA: {x.NoteId}]\n" +
                    (x.Text.Length > 280 ? x.Text[..280] + "…" : x.Text)));
        }
        catch (Exception ex) when (ex is IOException or System.Security.Cryptography.CryptographicException or InvalidDataException or UnauthorizedAccessException)
        { NoteSearchResults.Text = AstraOS.Core.UiText.T("Impossibile leggere l'archivio cifrato: ") + ex.Message; }
        finally { if (searchButton is not null) searchButton.IsEnabled = true; }
    }

    private void RefreshNotes_Click(object sender, RoutedEventArgs e) => RefreshNotes();
    private async void RefreshNotes()
    {
        NotesList.Children.Clear();
        NoteCount.Text = AstraOS.Core.UiText.T("Caricamento note locali…");
        try
        {
            var notes = await Task.Run(_notes.Load);
            NoteCount.Text = AstraOS.Core.UiText.F("Note personali cifrate: {0}", notes.Count);
            foreach (var note in notes.OrderByDescending(n => n.CreatedUtc).Take(50))
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                var open = new Button { Content = note.Title + (note.Tags.Length == 0 ? "" : "  #" + string.Join(" #", note.Tags)) };
                open.Click += async (_, _) =>
                {
                    var content = new TextBlock { Text = note.Content, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
                    await new ContentDialog { XamlRoot = AppRoot.XamlRoot, Title = note.Title,
                        Content = new ScrollViewer { Content = content, MaxHeight = 420 }, CloseButtonText = AstraOS.Core.UiText.T("Chiudi") }.ShowAsync();
                };
                row.Children.Add(open);
                var remove = new Button { Content = AstraOS.Core.UiText.T("Elimina"), Tag = note.Id };
                remove.Click += DeleteNote_Click;
                row.Children.Add(remove);
                NotesList.Children.Add(row);
            }
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            NoteCount.Text = AstraOS.Core.UiText.T("Impossibile decifrare le note. I dati originali NON vengono sovrascritti: ") + ex.Message;
        }
    }
    private async void ExportNotes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var confirm = new ContentDialog { XamlRoot = AppRoot.XamlRoot, Title = AstraOS.Core.UiText.T("Esporta copia delle note"),
                Content = AstraOS.Core.UiText.T("La copia JSON esportata è in chiaro. Scegli una posizione privata. L'archivio originale rimane cifrato con DPAPI."),
                PrimaryButtonText = AstraOS.Core.UiText.T("Scegli destinazione"), CloseButtonText = AstraOS.Core.UiText.T("Annulla"), DefaultButton = ContentDialogButton.Close };
            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
            var picker = new Windows.Storage.Pickers.FileSavePicker { SuggestedFileName = "astra-notes-" + DateTime.Now.ToString("yyyyMMdd") };
            picker.FileTypeChoices.Add("Archivio note JSON", new[] { ".json" });
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
            var file = await picker.PickSaveFileAsync(); if (file is null) return;
            var notes = await Task.Run(_notes.Load);
            var json = System.Text.Json.JsonSerializer.Serialize(new { schemaVersion = 1, notes }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(file.Path, json);
            AiStatusValue.Text = AstraOS.Core.UiText.F("Esportate {0} note: {1}", notes.Count, file.Path);
        }
        catch (Exception ex) { if (!_windowClosed) AiStatusValue.Text = AstraOS.Core.UiText.T("Esportazione non riuscita: ") + ex.Message; }
    }
    private async void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id }) return;
        var dialog = new ContentDialog { XamlRoot = AppRoot.XamlRoot, Title = AstraOS.Core.UiText.T("Eliminare la nota?"),
            Content = AstraOS.Core.UiText.T("L'operazione modifica l'archivio cifrato. Non è previsto un cestino interno."),
            PrimaryButtonText = AstraOS.Core.UiText.T("Elimina"), CloseButtonText = AstraOS.Core.UiText.T("Annulla") };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { await Task.Run(() => _notes.Delete(id)); RefreshNotes(); }
        catch (Exception ex) { AiStatusValue.Text = AstraOS.Core.UiText.T("Eliminazione non riuscita: ") + ex.Message; }
    }

    private async void DeleteAllNotes_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = AppRoot.XamlRoot,
            Title = AstraOS.Core.UiText.T("Eliminare TUTTE le note cifrate?"),
            Content = AstraOS.Core.UiText.T("Questa azione elimina l'archivio locale Second Brain e non è annullabile tramite ASTRA. ") +
                      AstraOS.Core.UiText.T("Non elimina backup esterni eventualmente creati dall'utente."),
            PrimaryButtonText = AstraOS.Core.UiText.T("Elimina definitivamente"), CloseButtonText = AstraOS.Core.UiText.T("Annulla"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { _inferenceCancellation?.Cancel(); await Task.Run(_notes.Clear); RefreshNotes();
            AiStatusValue.Text = AstraOS.Core.UiText.T("Archivio note locale eliminato."); }
        catch (Exception ex) { AiStatusValue.Text = AstraOS.Core.UiText.T("Eliminazione fallita: ") + ex.Message; }
    }

    // Plugins: discovered manifests are data; arbitrary code is executed only after a warning and consent.
    private async void ReloadPlugins_Click(object sender, RoutedEventArgs e) { _recipeCache=null; await RenderPluginsAsync(); }
    private void NativePluginToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (PluginsList is not null && _preferences is not null) _ = RenderPluginsAsync();
    }

    private async Task RenderPluginsAsync()
    {
        int revision=++_pluginRenderRevision;
        PluginsList.Children.Clear();
        try
        {
            PluginStatus.Text=AstraOS.Core.UiText.T("Caricamento plugin locali…");
            _recipeCache ??= await Task.Run(_plugins.DiscoverDeclarative);
            if(_windowClosed||revision!=_pluginRenderRevision)return;
            var search=PluginSearchBox.Text.Trim();var category=PluginCategory.SelectedItem?.ToString()??"Tutti";
            var recipes = _recipeCache.Where(x=>(category=="Tutti"||x.Plugin.Category==category)&&
                (x.Plugin.Name+" "+x.Plugin.Description+" "+x.Plugin.Category).Contains(search,StringComparison.CurrentCultureIgnoreCase)).ToArray();
            PluginStatus.Text = AstraOS.Core.UiText.F("{0} plugin mostrati / {1} disponibili · operazioni locali senza codice esterno", recipes.Length, _recipeCache.Count);
            foreach (var (recipe, folder) in recipes)
            {
                var row = new StackPanel { Spacing = 6 };
                row.Children.Add(new TextBlock { Text = recipe.Name + " · " + recipe.Id + AstraOS.Core.UiText.T(" · DATI"),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                row.Children.Add(new TextBlock { Text = recipe.Description, TextWrapping = TextWrapping.Wrap });
                row.Children.Add(new TextBlock { Text = recipe.Category, Foreground=Token("AstraIce"), FontSize=12 });
                var sample=new Button{Content=AstraOS.Core.UiText.T("Usa esempio")};
                sample.Click+=(_,_)=>{PluginInput.Text=recipe.Example;PluginInput.Header=AstraOS.Core.UiText.T("Input · ")+recipe.Name;PluginResult.Text="";};
                row.Children.Add(sample);
                var button = new Button { Content = AstraOS.Core.UiText.T("Applica trasformazione"), Tag = (recipe, folder) };
                button.Click += RunRecipe_Click;
                button.IsEnabled = !_preferences.DisabledPlugins.Contains(recipe.Id);
                var enabled = new CheckBox { Content = AstraOS.Core.UiText.T("Abilitato"), IsChecked = button.IsEnabled };
                enabled.Click += (_, _) =>
                {
                    var old = _preferences.DisabledPlugins.ToList();
                    if (enabled.IsChecked == true) _preferences.DisabledPlugins.Remove(recipe.Id);
                    else if (!_preferences.DisabledPlugins.Contains(recipe.Id)) _preferences.DisabledPlugins.Add(recipe.Id);
                    try { _preferenceStore.Save(_preferences); button.IsEnabled = enabled.IsChecked == true; }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { _preferences.DisabledPlugins = old; enabled.IsChecked = !old.Contains(recipe.Id); PluginStatus.Text = AstraOS.Core.UiText.T("Preferenza non salvata: ") + ex.Message; }
                };
                row.Children.Add(enabled);
                row.Children.Add(button);
                PluginsList.Children.Add(row);
            }
            if (!NativePluginToggle.IsOn) return;
            var list = _plugins.Discover();
            PluginStatus.Text += AstraOS.Core.UiText.F(" · {0} plugin nativi NON isolati (modalità sviluppatore)", list.Count);
            foreach (var (manifest, folder) in list)
            {
                var row = new StackPanel { Spacing = 6 };
                row.Children.Add(new TextBlock { Text = manifest.Name + " · " + manifest.Id + AstraOS.Core.UiText.T(" · NON ISOLATO"),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                row.Children.Add(new TextBlock { Text = manifest.Description, TextWrapping = TextWrapping.Wrap });
                var button = new Button { Content = AstraOS.Core.UiText.T("Esegui eseguibile esterno con conferma"), Tag = (manifest, folder) };
                button.Click += RunPlugin_Click;
                row.Children.Add(button);
                PluginsList.Children.Add(row);
            }
        }
        catch (Exception ex) { PluginStatus.Text = AstraOS.Core.UiText.T("Errore scansione plugin: ") + ex.Message; }
    }
    private void PluginFilter_Changed(object sender,RoutedEventArgs e)
    {if(_preferences is not null && PluginsList is not null)_ = RenderPluginsAsync();}

    private void RunRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ValueTuple<AstraOS.Core.DeclarativePlugin, string> entry) return;
        if (_preferences.DisabledPlugins.Contains(entry.Item1.Id)) { PluginStatus.Text = AstraOS.Core.UiText.T("Plugin disabilitato."); return; }
        try { var result=_plugins.ExecuteDeclarative(entry.Item1, entry.Item2, PluginInput.Text);PluginResult.Text=string.IsNullOrEmpty(result)?AstraOS.Core.UiText.T("Nessun risultato per questo input."):result;PluginStatus.Text=AstraOS.Core.UiText.T("Completato: ")+entry.Item1.Name; }
        catch (Exception ex) { PluginResult.Text="";PluginStatus.Text = AstraOS.Core.UiText.T("Ricetta non eseguita: ") + ex.Message; }
    }

    private void RunPlugin_Click(object sender, RoutedEventArgs e) =>
        PluginStatus.Text = AstraOS.Core.UiText.T("V3: esecuzione di plugin nativi non disponibile. Usa ricette dichiarative senza codice.");

    // On-demand offline WebGL, no Explorer replacement / wallpaper hook.
    private async void Start3D_Click(object sender, RoutedEventArgs e) => await Start3DAsync();
    private async Task Start3DAsync()
    {
        if (_orbitActive || _orbitInitializing) { OrbitStatus.Text = AstraOS.Core.UiText.T("La visualizzazione è già attiva o in caricamento."); return; }
        var generation = ++_orbitGeneration;
        _orbitInitializing = true;
        _orbitReady = false;
        _desktopOrbitEntries = new Dictionary<string,ExplorerEntry>();
        OrbitStatus.Text = AstraOS.Core.UiText.T("Inizializzazione WebView2…");
        OrbitStartButton.IsEnabled = false;
        try
        {
            string assetDirectory = Path.Combine(AppContext.BaseDirectory, "Graphics3D", "web");
            if (!File.Exists(Path.Combine(assetDirectory, "index.html")))
                throw new FileNotFoundException(AstraOS.Core.UiText.T("Scena offline mancante."));
            // WebView2 has native/runtime dependencies. Loading it from MainWindow.xaml
            // would instantiate it even while the Orbital panel is collapsed. Keep it
            // out of the startup XAML tree so optional 3D cannot block the dashboard.
            if (_orbitView is null)
            {
                _orbitView = new WebView2 { Visibility = Visibility.Collapsed };
                OrbitViewHost.Children.Add(_orbitView);
            }
            var view = _orbitView;
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null,
                Path.Combine(AstraPaths.UserDataRoot, "WebView2"), null);
            if (generation != _orbitGeneration || _windowClosed) return;
            await view.EnsureCoreWebView2Async(environment);
            if (generation != _orbitGeneration || _windowClosed || _v4LastPage != "orbit") return;
            var core = view.CoreWebView2 ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("WebView2 non inizializzato."));
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreHostObjectsAllowed = false;
            core.SetVirtualHostNameToFolderMapping("astra.local", assetDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
            view.NavigationStarting -= OrbitView_NavigationStarting;
            view.NavigationStarting += OrbitView_NavigationStarting;
            core.ProcessFailed += OrbitProcessFailed;
            core.WebMessageReceived += OrbitWebMessageReceived;
            core.NavigationCompleted += OrbitNavigationCompleted;
            view.Visibility = Visibility.Visible;
            var cap = AstraOS.Core.PerformancePolicy.EffectiveOrbitFps(_preferences);
            var quality = _preferences.EcoMode ? "low" : OrbitQualityCombo.SelectedIndex switch { 0 => "low", 2 => "high", _ => "balanced" };
            core.Navigate($"https://astra.local/index.html?fps={cap}&motion={(_preferences.ReducedMotion ? 0 : 1)}&quality={quality}");
            _orbitActive = true;
            OrbitStatus.Text = AstraOS.Core.UiText.F("Caricamento scena offline · limite {0} FPS · qualità {1}", cap, quality);
        }
        catch (Exception ex)
        {
            if (generation == _orbitGeneration && !_windowClosed) { OrbitStatus.Text = AstraOS.Core.UiText.T("3D non disponibile: ") + ex.Message; Stop3DCore(); }
            StartupDiagnostics.Record("Orbital.initialization.failed", ex.ToString());
        }
        finally { if (generation == _orbitGeneration) _orbitInitializing = false; if (!_windowClosed) OrbitStartButton.IsEnabled = true; }
    }
    private void OrbitView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == "https" && uri.Host == "astra.local" && uri.AbsolutePath == "/index.html" &&
              System.Text.RegularExpressions.Regex.IsMatch(uri.Query, @"^\?fps=(15|30|60)&motion=(0|1)&quality=(low|balanced|high)$")) &&
            e.Uri != "about:blank") e.Cancel = true;
    }
    private void Stop3D_Click(object sender, RoutedEventArgs e) { Stop3DCore(); OrbitStatus.Text = AstraOS.Core.UiText.T("3D arrestato e pagina scaricata."); }
    private void Stop3DCore()
    {
        ++_orbitGeneration; _orbitInitializing = false;
        if (_orbitView is not null)
        {
            var view = _orbitView; _orbitView = null;
            view.NavigationStarting -= OrbitView_NavigationStarting;
            if (view.CoreWebView2 is not null)
            {
                view.CoreWebView2.ProcessFailed -= OrbitProcessFailed;
                view.CoreWebView2.WebMessageReceived -= OrbitWebMessageReceived;
                view.CoreWebView2.NavigationCompleted -= OrbitNavigationCompleted;
            }
            OrbitViewHost.Children.Remove(view);
            view.Close();
        }
        _orbitActive = false;
        _orbitReady = false;
    }
    private async void OrbitNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess) { OrbitStatus.Text = AstraOS.Core.UiText.T("Scena non caricata: ") + args.WebErrorStatus; Stop3DCore(); return; }
        ApplyOrbitPalette();
        await RefreshDesktopOrbitAsync();
    }
    private async void OrbitWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (_orbitView is null || !Uri.TryCreate(args.Source, UriKind.Absolute, out var uri) || uri.Host != "astra.local") return;
        try
        {
            if (args.WebMessageAsJson.Length > 1024) return;
            using var doc = System.Text.Json.JsonDocument.Parse(args.WebMessageAsJson);
            if(doc.RootElement.TryGetProperty("type",out var action)&&action.ValueKind==System.Text.Json.JsonValueKind.String)
            {
                var command=action.GetString();
                if(command=="astra-desktop-refresh"){await RefreshDesktopOrbitAsync();return;}
                if(command is "astra-desktop-open" or "astra-desktop-explorer")
                {
                    if(!doc.RootElement.TryGetProperty("id",out var key)||key.ValueKind!=System.Text.Json.JsonValueKind.String||!_desktopOrbitEntries.TryGetValue(key.GetString()??"",out var entry)||!FileExplorerService.IsCurrentDesktopEntry(entry))return;
                    if(command=="astra-desktop-open")await OpenEntryAsync(entry);
                    else OpenWindowsExplorer(entry.IsDirectory?entry.FullPath:Path.GetDirectoryName(entry.FullPath)!);
                    return;
                }
            }
            if (!doc.RootElement.TryGetProperty("type", out var type) || type.ValueKind != System.Text.Json.JsonValueKind.String || type.GetString() != "astra-orbit-ready" ||
                !doc.RootElement.TryGetProperty("ready", out var ready) || ready.ValueKind is not (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False)) return;
            _orbitReady = ready.GetBoolean();
            OrbitStatus.Text = _orbitReady ? AstraOS.Core.UiText.F("Desktop orbitale pronto · {0} file/cartelle reali · cap {1} FPS", _desktopOrbitEntries.Count, PerformancePolicy.EffectiveOrbitFps(_preferences)) : AstraOS.Core.UiText.T("WebGL non disponibile; dashboard operativa.");
        }
        catch (System.Text.Json.JsonException ex) { StartupDiagnostics.Record("Orbital.message.invalid", ex.Message); }
    }
    private async Task RefreshDesktopOrbitAsync()
    {
        int generation=_orbitGeneration;
        try
        {
            var entries=await Task.Run(()=>FileExplorerService.ReadDesktop(_windowLifetime.Token),_windowLifetime.Token);
            if(_windowClosed||generation!=_orbitGeneration||_orbitView?.CoreWebView2 is null)return;
            _desktopOrbitEntries=entries.ToDictionary(e=>e.Id,StringComparer.Ordinal);
            _orbitView.CoreWebView2.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(new{type="astra-desktop",items=entries.Select(e=>new{id=e.Id,name=e.Name,folder=e.IsDirectory,kind=e.Kind,size=e.Size,modified=e.Modified,location=e.Location==Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)?"Desktop condiviso":"Desktop personale"})}));
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!_windowClosed)OrbitStatus.Text=AstraOS.Core.UiText.T("Desktop non leggibile: ")+ex.Message;StartupDiagnostics.Record("DesktopOrbit.read.failed",ex.Message);}
    }
    private void ApplyOrbitPalette()
    {
        _orbitView?.CoreWebView2?.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(new
        { type = "astra-theme", locale=UiText.Language, strings=UiText.WebCatalog(), navy = _preferences.Palette.Navy, ice = _preferences.Palette.Ice, blue = _preferences.Palette.Blue, snow = _preferences.Palette.Snow, deep = _preferences.Palette.Deep }));
    }
    private void OrbitProcessFailed(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2ProcessFailedEventArgs args)
    {
        StartupDiagnostics.Record("Orbital.process.failed", args.ProcessFailedKind.ToString());
        Stop3DCore(); if (!_windowClosed) OrbitStatus.Text = AstraOS.Core.UiText.T("Processo WebView2 terminato. Puoi riattivare la scena; la dashboard resta disponibile.");
    }

    // ISO: script is always started as a bounded, explicit user action, never on application startup.
    private async void PlanIso_Click(object sender, RoutedEventArgs e) => await ExecuteIsoAsync(build: false);
    private async void BuildIso_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = AppRoot.XamlRoot, Title = AstraOS.Core.UiText.T("Generare ISO con contenuto Microsoft?"),
            Content = AstraOS.Core.UiText.T("Confermi di possedere o utilizzare legittimamente il media Windows 11 indicato? ") +
                AstraOS.Core.UiText.T("ASTRA genera un'immagine locale e NON la carica o distribuisce. Non ridistribuire la ISO contenente Windows senza i necessari diritti. ") +
                AstraOS.Core.UiText.T("L'operazione può richiedere vari GB e temporaneamente occupare spazio disco."),
            PrimaryButtonText = AstraOS.Core.UiText.T("Procedi alla build locale"), CloseButtonText = AstraOS.Core.UiText.T("Annulla"), DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) await ExecuteIsoAsync(build: true);
    }
    private async Task ExecuteIsoAsync(bool build)
    {
        IsoPlanButton.IsEnabled = false; IsoBuildButton.IsEnabled = false;
        IsoResult.Text = build ? AstraOS.Core.UiText.T("Creazione locale in corso…") : AstraOS.Core.UiText.T("Validazione percorsi e prerequisiti…");
        try
        {
            var script = Path.Combine(AppContext.BaseDirectory, "builder", "New-AstraIso.ps1");
            if (!File.Exists(script)) throw new FileNotFoundException(AstraOS.Core.UiText.T("Builder script non distribuito"), script);
            var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-File", script,
                "-SourceMediaDirectory", IsoSourcePath.Text, "-AstraDistributionDirectory", IsoAppPath.Text,
                "-OutputIso", IsoOutputPath.Text }) start.ArgumentList.Add(arg);
            if (build) start.ArgumentList.Add("-Build");
            using var process = Process.Start(start) ?? throw new InvalidOperationException(AstraOS.Core.UiText.T("Impossibile eseguire PowerShell."));
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(25));
            // Process streams are consumed simultaneously to prevent pipe deadlock.
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw;
            }
            var output = (await stdout) + "\n" + (await stderr);
            IsoResult.Text = (process.ExitCode == 0 ? AstraOS.Core.UiText.T("COMPLETATO\n") : AstraOS.Core.UiText.F("ERRORE ({0})\n", process.ExitCode)) + output[..Math.Min(output.Length, 8000)];
        }
        catch (Exception ex) { IsoResult.Text = AstraOS.Core.UiText.T("ISO Builder: ") + ex.Message; }
        finally { IsoPlanButton.IsEnabled = true; IsoBuildButton.IsEnabled = true; }
    }
}
