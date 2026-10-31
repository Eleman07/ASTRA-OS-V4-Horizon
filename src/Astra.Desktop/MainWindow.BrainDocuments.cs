using AstraOS.Core;
using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly EncryptedDocumentStore _documents=new();
    private ComboBox? _brainDocumentChoice;
    private TextBlock? _pdfStatus;
    private CancellationTokenSource? _pdfOperation;
    private bool _pdfBusy;
    private string? _lastPdfSummary;
    private void InitializeBrainDocuments()
    {
        var p=new StackPanel{Spacing=10};p.Children.Add(Label("Second Brain",24));
        p.Children.Add(Label(UiText.T("Il tuo assistente con Nemotron 3 Ultra su OpenRouter: voce, documenti lunghi, conoscenza Obsidian e agenti specializzati. La chiave OpenRouter viene conservata cifrata per il tuo account Windows."),13));
        p.Children.Add(Action(UiText.T("Apri Second Brain · voce e allegati"),(_,_)=>OpenAssistant("Second Brain")));
        p.Children.Add(Label(UiText.T("Prova: apri Chrome · apri STEM · focus 25 minuti · aggiungi attività studiare il capitolo 3 · briefing. I comandi si possono scrivere nella domanda qui sotto."),12));
        BrainPanel.Children.Insert(0,Card(p));
        var pdf=new StackPanel{Spacing=9};pdf.Children.Add(Label(UiText.T("I TUOI PDF"),18));
        pdf.Children.Add(Label(UiText.T("Importa libri e dispense: ricerca su tutte le pagine, riferimenti di pagina e riassunto completo per blocchi. Le scansioni usano OCR locale."),12));
        _brainDocumentChoice=new ComboBox{Header=UiText.T("Documento per le domande e il riassunto"),DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};pdf.Children.Add(_brainDocumentChoice);
        var actions=new StackPanel{Spacing=8};
        actions.Children.Add(Action(UiText.T("Importa PDF…"),async(_,_)=>await PickBrainPdfAsync()));
        actions.Children.Add(Action(UiText.T("Riassunto completo"),async(_,_)=>await SummarizeBrainPdfAsync()));
        actions.Children.Add(Action(UiText.T("Annulla operazione PDF"),(_,_)=>_pdfOperation?.Cancel()));
        actions.Children.Add(Action(UiText.T("Rimuovi PDF selezionato"),async(_,_)=>await RemoveBrainPdfAsync()));pdf.Children.Add(actions);
        _pdfStatus=Label(UiText.T("I PDF restano cifrati sul PC. Seleziona un documento per limitare la ricerca, oppure deselezionalo per cercare nella biblioteca."),12);pdf.Children.Add(_pdfStatus);
        pdf.Children.Add(Action(UiText.T("Cerca in tutti i PDF"),(_,_)=>_brainDocumentChoice.SelectedIndex=-1));
        pdf.Children.Add(Details(UiText.T("Limiti e funzionamento PDF"),Label(UiText.T("Fino a 512 MB, 10.000 pagine e 50 milioni di caratteri per PDF. Il testo viene letto una pagina alla volta e cifrato con il tuo account Windows. Le pagine scansionate richiedono una lingua OCR Windows. I riassunti lunghi possono richiedere tempo e si possono annullare."),12)));
        BrainPanel.Children.Insert(1,Card(pdf));
        AiImportPath.Header=UiText.T("Importa PDF, .txt/.md o archivio ASTRA .json");
        L10n0139.Content=UiText.T("Cerca note e PDF (offline)");
        Closed+=(_,_)=>_pdfOperation?.Cancel();_ = RefreshBrainDocumentsAsync();
        // Runtime paths are available in a collapsed advanced section.
        var runtime=BrainRuntimeCard;
        if(runtime is not null){BrainPanel.Children.Remove(runtime);BrainPanel.Children.Add(Details(UiText.T("Impostazioni del provider AI"),runtime));}
    }
    private async Task RefreshBrainDocumentsAsync(string? select=null)
    {
        try{var docs=await Task.Run(_documents.List);if(_windowClosed||_brainDocumentChoice is null)return;_brainDocumentChoice.ItemsSource=docs;_brainDocumentChoice.SelectedItem=docs.FirstOrDefault(d=>d.Id==select);}
        catch(Exception ex){if(_pdfStatus is not null)_pdfStatus.Text=ex.Message;}
    }
    private async Task PickBrainPdfAsync()
    {
        try{
        var picker=new FileOpenPicker();picker.FileTypeFilter.Add(".pdf");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));
        var file=await picker.PickSingleFileAsync();if(file is not null&&!_windowClosed)await ImportBrainPdfAsync(file.Path);
        }catch(OperationCanceledException){if(!_windowClosed)_pdfStatus!.Text=UiText.T("Operazione PDF annullata.");}
        catch(Exception ex){if(!_windowClosed)_pdfStatus!.Text=ex.Message;}
    }
    private async Task<BrainDocument> ImportBrainPdfAsync(string path,CancellationToken ct=default)
    {
        if(_pdfBusy)throw new InvalidOperationException(UiText.T("Attendi o annulla l'operazione PDF in corso."));
        _pdfBusy=true;_pdfOperation?.Dispose();_pdfOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token,ct);
        try
        {
            var progress=new Progress<(int Page,int Total)>(value=>{if(!_windowClosed&&_pdfStatus is not null)_pdfStatus.Text=UiText.F("Importazione PDF: pagina {0} di {1}",value.Page,value.Total);});
            var doc=await BrainPdfReader.ImportAsync(path,_documents,progress,_pdfOperation.Token);
            await RefreshBrainDocumentsAsync(doc.Id);
            if(_pdfStatus is not null)_pdfStatus.Text=UiText.F("PDF pronto: {0} pagine · {1} con OCR · {2} senza testo. Ora puoi cercare o fare domande.",doc.Pages,doc.OcrPages,doc.EmptyPages);
            return doc;
        }
        finally{_pdfBusy=false;}
    }
    private async Task SummarizeBrainPdfAsync(CancellationToken ct=default)
    {
        _lastPdfSummary=null;
        if(_brainDocumentChoice?.SelectedItem is not BrainDocument doc){_pdfStatus!.Text=UiText.T("Seleziona un PDF importato.");return;}
        if(_pdfBusy){_pdfStatus!.Text=UiText.T("Attendi o annulla l'operazione PDF in corso.");return;}
        _pdfBusy=true;_lastPdfSummary=null;_pdfOperation?.Dispose();_pdfOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token,ct);
        try
        {
            await EnsureAssistantModelAsync(_pdfOperation.Token);
            AiAnswer.Text=await DocumentSummarizer.SummarizeAsync(_documents,doc,
                async(instruction,input,ct)=>await _brain.RunTaskAsync(instruction,input,false,0,ct),
                new Progress<string>(value=>{if(!_windowClosed)_pdfStatus!.Text=value;}),_pdfOperation.Token);
            _lastPdfSummary=AiAnswer.Text;
            _pdfStatus!.Text=UiText.F("Riassunto completato: elaborato il testo di tutte le {0} pagine. Pagine senza testo: {1}.",doc.Pages,doc.EmptyPages);
        }
        catch(OperationCanceledException){_pdfStatus!.Text=UiText.T("Operazione PDF annullata.");}
        catch(Exception ex){_pdfStatus!.Text=ex.Message;}
        finally{_pdfBusy=false;}
    }
    private async Task RemoveBrainPdfAsync()
    {
        try{
        if(_brainDocumentChoice?.SelectedItem is not BrainDocument doc||_pdfBusy)return;
        var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Rimuovere il PDF dalla memoria?"),Content=doc.Name,PrimaryButtonText=UiText.T("Rimuovi"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
        await Task.Run(()=>_documents.Delete(doc.Id));await RefreshBrainDocumentsAsync();_pdfStatus!.Text=UiText.T("Copia cifrata rimossa. Il PDF originale resta al suo posto.");
        }catch(Exception ex){if(!_windowClosed)_pdfStatus!.Text=ex.Message;}
    }
    private async Task<IReadOnlyList<NotePassage>> FindBrainPassagesAsync(string query,CancellationToken ct)
    {
        string? id=(_brainDocumentChoice?.SelectedItem as BrainDocument)?.Id;
        return await Task.Run(()=>_documents.Search(query,id,4,ct),ct);
    }
    private async Task<AssistantInput> ReadSecondBrainInputAsync(string path,CancellationToken ct)
    {
        if(!Path.GetExtension(path).Equals(".pdf",StringComparison.OrdinalIgnoreCase))return await AssistantMedia.ReadAsync(path,ct);
        var document=await ImportBrainPdfAsync(path,ct);
        string text=await Task.Run(()=>string.Join("\n\n",_documents.ReadPages(document.Id,ct).Select(page=>$"[pdf-{document.Id}-p{page.Number}] {document.Name} · page {page.Number}\n{page.Text}")),ct);
        return new AssistantInput(document.Name,"pdf",UiText.F("PDF completo: {0} pagine · {1} con OCR · {2} senza testo estratto. Verifica le trascrizioni OCR contro l'originale.",document.Pages,document.OcrPages,document.EmptyPages)+"\n\n"+text);
    }
    private async Task<string?> ExecuteBrainCommandAsync(string input,CancellationToken ct)
    {
        var command=BrainCommands.Parse(input);if(command is null)return await ExecuteAssistantToolAsync(input,ct);
        switch(command.Kind)
        {
            case "help":return UiText.T("Posso cercare nelle note e nei PDF, parlare e trascrivere audio, spiegare documenti, creare schede e widget, aprire app, avviare un timer focus e aggiungere attività. Prova: apri Chrome, focus 25 minuti, aggiungi attività ripassare, briefing.");
            case "chrome":ChromeLauncher.Open();return UiText.T("Apertura di Chrome richiesta.");
            case "briefing":return UiText.T("Le tue attività aperte:")+"\n"+string.Join("\n",_v4Tasks.Where(t=>!t.Completed).Take(12).Select(t=>"• "+t.Text).DefaultIfEmpty(UiText.T("Nessuna attività aperta.")));
            case "pdf-summary":await SummarizeBrainPdfAsync(ct);return _lastPdfSummary??_pdfStatus?.Text;
            case "cancel-pdf":_pdfOperation?.Cancel();return UiText.T("Annullamento PDF richiesto.");
            case "remember":await Task.Run(()=>_notes.Add("Second Brain",command.Argument,new[]{"memory"}),ct);return UiText.T("Memoria salvata: ")+command.Argument;
            case "focus":AddWidget(new WidgetSpec{Title=UiText.T("Focus"),Kind="focus",Target=DateTimeOffset.Now.AddMinutes(int.Parse(command.Argument)),Text=command.Argument});return UiText.F("Timer focus avviato: {0} minuti.",command.Argument);
            case "task":
                if(_v4Tasks.Count>=100)throw new InvalidOperationException(UiText.T("Limite attività raggiunto."));
                var task=new AstraTaskItem{Text=command.Argument};_v4Tasks.Add(task);try{_v4TaskStore.Save(_v4Tasks);}catch{_v4Tasks.Remove(task);throw;}V4RenderTasks();return UiText.T("Attività aggiunta: ")+command.Argument;
            case "open":
                var routes=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"home","home"},{"stem","stem"},{"gaming","gaming"},{"creator","creator"},{"widget","widgets"},{"widgets","widgets"},{"file explorer","files"},{"files","files"},{"plugin","plugins"},{"plugins","plugins"},{"second brain","brain"},{"astra assistant","brain"},{"assistant","brain"},{"brain","brain"},{"system","system"},{"multitasking","multitasking"}};
                if(routes.TryGetValue(command.Argument,out var route)){Navigate(route);Activate();return UiText.T("Sezione aperta: ")+command.Argument;}
                await EnsureAppsAsync();ct.ThrowIfCancellationRequested();var app=_installedApps.FirstOrDefault(a=>a.Name.Equals(command.Argument,StringComparison.CurrentCultureIgnoreCase));
                if(app is null)return UiText.T("App non trovata. Usa il nome esatto mostrato in App Hub.");LaunchInstalledApp(app);return UiText.T("Apertura richiesta: ")+app.Name;
            default:return null;
        }
    }
}
