using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private readonly EncryptedWidgetStore _widgetStore=new();
    private List<WidgetSpec> _widgets=new();
    private readonly Dictionary<string,TextBlock> _widgetValues=new();
    private readonly DispatcherTimer _widgetTimer=new(){Interval=TimeSpan.FromSeconds(1)};
    private Grid? _widgetGrid;
    private TextBlock? _widgetStatus,_widgetLog;
    private readonly Queue<string> _widgetEvents=new();
    private TextBox? _widgetTitle,_widgetText,_automationArgument;
    private ComboBox? _widgetKind,_widgetColor,_widgetWidth,_widgetZone,_automationTrigger,_automationAction;
    private ToggleSwitch? _automationEnabled;
    private NumberBox? _automationInterval;
    private TimePicker? _automationTime;
    private DatePicker? _widgetDate;
    private string? _editingWidget;
    private bool _widgetTickBusy;
    private void InitializeWidgets()
    {
        try{_widgets=_widgetStore.Load();}catch(Exception ex){StatusText.Text=AstraOS.Core.UiText.T("Widget: ")+ex.Message;}
        var catalog=new StackPanel{Spacing=10};catalog.Children.Add(Label(AstraOS.Core.UiText.T("WIDGET / IL TUO SPAZIO PERSONALE"),20));
        catalog.Children.Add(Label(AstraOS.Core.UiText.T("Aggiungi strumenti dal catalogo o crea una scheda personale. Le automazioni funzionano mentre ASTRA è aperto, anche se usi un'altra app; gli eventi scaduti vengono eseguiti una sola volta al ritorno."),12));
        var kinds=new[]{"Orologio e fusi orari","Monitor di sistema","Testo personale","Conto alla rovescia","Timer focus","Attività","Collegamento workspace","Automazione"};
        var choose=new ComboBox{ItemsSource=kinds,SelectedIndex=0,Header=AstraOS.Core.UiText.T("Catalogo widget"),HorizontalAlignment=HorizontalAlignment.Stretch};catalog.Children.Add(choose);
        catalog.Children.Add(Action(AstraOS.Core.UiText.T("Aggiungi al mio spazio"),(_,_)=>{try{var w=new WidgetSpec{Kind=WidgetRules.Kinds[choose.SelectedIndex],Title=kinds[choose.SelectedIndex],Text=choose.SelectedIndex==4?"25":choose.SelectedIndex==6?"stem":"",Target=choose.SelectedIndex==3?DateTimeOffset.Now.AddDays(1):null};AddWidget(w);_widgetStatus!.Text=AstraOS.Core.UiText.T("Widget aggiunto.");}catch(Exception ex){_widgetStatus!.Text=ex.Message;}}));
        var io=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};io.Children.Add(Action(AstraOS.Core.UiText.T("Importa widget…"),async(_,_)=>await ImportWidgetsAsync()));io.Children.Add(Action(AstraOS.Core.UiText.T("Esporta widget…"),async(_,_)=>await ExportWidgetsAsync()));catalog.Children.Add(io);WidgetsPanel.Children.Add(Card(catalog));
        BuildWidgetEditor(kinds);_widgetStatus=Label(AstraOS.Core.UiText.T("Configurazioni cifrate per il tuo account Windows."),12);WidgetsPanel.Children.Add(_widgetStatus);
        _widgetGrid=new Grid{ColumnSpacing=12,RowSpacing=12};WidgetsPanel.Children.Add(_widgetGrid);
        var events=new StackPanel{Spacing=6};events.Children.Add(Label(AstraOS.Core.UiText.T("ATTIVITÀ DELLE AUTOMAZIONI"),16));_widgetLog=Label(AstraOS.Core.UiText.T("Nessuna automazione eseguita."),12);events.Children.Add(_widgetLog);WidgetsPanel.Children.Add(Card(events));
        RenderWidgets();_widgetTimer.Tick+=async(_,_)=>await TickWidgetsAsync();_widgetTimer.Start();Closed+=(_,_)=>_widgetTimer.Stop();
    }
    private void BuildWidgetEditor(string[] kindNames)
    {
        var p=new StackPanel{Spacing=8};_widgetTitle=new TextBox{Header=AstraOS.Core.UiText.T("Nome del widget"),MaxLength=80};_widgetText=new TextBox{Header=AstraOS.Core.UiText.T("Testo / minuti focus / percorso workspace"),AcceptsReturn=true,MaxLength=2000,MinHeight=70};
        _widgetKind=new ComboBox{Header=AstraOS.Core.UiText.T("Tipo di widget"),ItemsSource=kindNames,SelectedIndex=2};_widgetColor=new ComboBox{Header=AstraOS.Core.UiText.T("Stile del tema"),ItemsSource=new[]{"Tema","Ice","Snow"},SelectedIndex=0};_widgetWidth=new ComboBox{Header=AstraOS.Core.UiText.T("Larghezza"),ItemsSource=new[]{"Una colonna","Due colonne"},SelectedIndex=0};
        _widgetZone=new ComboBox{Header=AstraOS.Core.UiText.T("Fuso orario dell'orologio"),ItemsSource=new[]{"local"}.Concat(TimeZoneInfo.GetSystemTimeZones().Select(z=>z.Id)).ToArray(),SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};_widgetDate=new DatePicker{Header=AstraOS.Core.UiText.T("Scadenza del conto alla rovescia"),Date=DateTimeOffset.Now.AddDays(1)};
        foreach(var e in new UIElement[]{_widgetTitle,_widgetKind,_widgetText,_widgetColor,_widgetWidth,_widgetZone,_widgetDate})p.Children.Add(e);
        _automationEnabled=new ToggleSwitch{Header=AstraOS.Core.UiText.T("Associa un'automazione")};p.Children.Add(_automationEnabled);
        var auto=new StackPanel{Spacing=8,Visibility=Visibility.Collapsed};_automationEnabled.Toggled+=(_,_)=>auto.Visibility=_automationEnabled.IsOn?Visibility.Visible:Visibility.Collapsed;
        _automationTrigger=new ComboBox{Header=AstraOS.Core.UiText.T("Quando"),ItemsSource=new[]{"Ogni N minuti","Ogni giorno all'ora scelta"},SelectedIndex=0};_automationInterval=new NumberBox{Header=AstraOS.Core.UiText.T("Intervallo (minuti)"),Minimum=1,Maximum=10080,Value=30};_automationTime=new TimePicker{Header=AstraOS.Core.UiText.T("Ora locale"),Time=new TimeSpan(9,0,0)};
        _automationAction=new ComboBox{Header=AstraOS.Core.UiText.T("Azione"),ItemsSource=new[]{"Promemoria nell'app","Apri una sezione ASTRA","Avvia timer focus","Salva una nota cifrata","Esegui routine Assistant"}.Select(UiText.T).ToArray(),SelectedIndex=0};_automationArgument=new TextBox{Header=AstraOS.Core.UiText.T("Testo / nome sezione / minuti / ID routine"),MaxLength=1500};
        foreach(var e in new UIElement[]{_automationTrigger,_automationInterval,_automationTime,_automationAction,_automationArgument})auto.Children.Add(e);
        auto.Children.Add(Label(AstraOS.Core.UiText.T("Sezione: home, stem, gaming, creator, widgets, files, assistant, windows-tools, brain, plugins. Le automazioni non eseguono script o comandi esterni."),12));p.Children.Add(auto);
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};buttons.Children.Add(Action(AstraOS.Core.UiText.T("Salva widget"),(_,_)=>SaveWidgetEditor()));buttons.Children.Add(Action(AstraOS.Core.UiText.T("Nuovo widget"),(_,_)=>{_editingWidget=null;_widgetTitle.Text="";_widgetText.Text="";_automationEnabled.IsOn=false;}));p.Children.Add(buttons);
        WidgetsPanel.Children.Add(new Expander{Header=AstraOS.Core.UiText.T("Crea o modifica il tuo widget"),Content=p,HorizontalAlignment=HorizontalAlignment.Stretch});
    }
    private void SaveWidgetEditor()
    {
        try
        {
            var automation=_automationEnabled!.IsOn?new AutomationSpec{Enabled=true,Trigger=_automationTrigger!.SelectedIndex==0?"interval":"daily",IntervalMinutes=(int)_automationInterval!.Value,Hour=_automationTime!.Time.Hours,Minute=_automationTime.Time.Minutes,Action=WidgetRules.Actions[_automationAction!.SelectedIndex],Argument=_automationArgument!.Text}:_widgets.FirstOrDefault(w=>w.Id==_editingWidget)?.Automation is {} previous?previous with{Enabled=false}:null;
            if(automation is not null)automation=automation with{NextDue=WidgetRules.Next(automation,DateTimeOffset.Now,TimeZoneInfo.Local)};
            var widget=new WidgetSpec{Id=_editingWidget??Guid.NewGuid().ToString("N"),Title=_widgetTitle!.Text.Trim(),Kind=WidgetRules.Kinds[_widgetKind!.SelectedIndex],Text=_widgetText!.Text,Color=new[]{"theme","ice","snow"}[_widgetColor!.SelectedIndex],Width=_widgetWidth!.SelectedIndex+1,TimeZone=_widgetZone!.SelectedItem?.ToString()??"local",Target=_widgetKind!.SelectedIndex==3?_widgetDate!.Date:null,Automation=automation};
            if(widget.Kind=="workspace"&&!NavigationCatalog.Routes.Contains(widget.Text))throw new InvalidDataException(AstraOS.Core.UiText.T("Inserisci un percorso workspace valido."));
            WidgetRules.Validate(widget);if(_editingWidget is null)AddWidget(widget);else{int i=_widgets.FindIndex(w=>w.Id==_editingWidget);if(i>=0){_widgets[i]=widget;_widgetStore.Save(_widgets);RenderWidgets();}}_widgetStatus!.Text=AstraOS.Core.UiText.T("Widget salvato.");_editingWidget=widget.Id;
        }catch(Exception ex){_widgetStatus!.Text=AstraOS.Core.UiText.T("Widget non salvato: ")+ex.Message;}
    }
    private void AddWidget(WidgetSpec widget)
    {
        if(_widgets.Count>=WidgetRules.MaxWidgets)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 32 widget: rimuovi una scheda per aggiungerne un'altra."));WidgetRules.Validate(widget);_widgets.Add(widget);_widgetStore.Save(_widgets);RenderWidgets();
    }
    private void EditWidget(WidgetSpec w)
    {
        _editingWidget=w.Id;_widgetTitle!.Text=w.Title;_widgetText!.Text=w.Text;_widgetKind!.SelectedIndex=Array.IndexOf(WidgetRules.Kinds,w.Kind);_widgetColor!.SelectedIndex=Array.IndexOf(new[]{"theme","ice","snow"},w.Color);_widgetWidth!.SelectedIndex=w.Width-1;_widgetZone!.SelectedItem=w.TimeZone;_widgetDate!.Date=w.Target??DateTimeOffset.Now.AddDays(1);
        _automationEnabled!.IsOn=w.Automation?.Enabled??false;if(w.Automation is {} a){_automationTrigger!.SelectedIndex=a.Trigger=="interval"?0:1;_automationInterval!.Value=a.IntervalMinutes;_automationTime!.Time=new TimeSpan(a.Hour,a.Minute,0);_automationAction!.SelectedIndex=Array.IndexOf(WidgetRules.Actions,a.Action);_automationArgument!.Text=a.Argument;}
        if(WidgetsPanel.Children.OfType<Expander>().FirstOrDefault() is {} editor){editor.IsExpanded=true;editor.StartBringIntoView();}
    }
    private void RenderWidgets()
    {
        if(_widgetGrid is null)return;_widgetGrid.Children.Clear();_widgetGrid.ColumnDefinitions.Clear();_widgetGrid.RowDefinitions.Clear();_widgetValues.Clear();int cols=Math.Clamp((int)((AppRoot.ActualWidth-SidebarColumn.Width.Value-70)/340),1,4);for(int i=0;i<cols;i++)_widgetGrid.ColumnDefinitions.Add(new ColumnDefinition());int row=0,col=0;
        foreach(var w in _widgets)
        {
            int span=Math.Min(cols,w.Width);if(col+span>cols){row++;col=0;}if(_widgetGrid.RowDefinitions.Count<=row)_widgetGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            var textColor=Token(w.Color=="ice"?"AstraIceText":w.Color=="snow"?"AstraInk":"AstraDeepText");var p=new StackPanel{Spacing=9};p.Children.Add(new TextBlock{Text=w.Title,FontSize=17,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold,Foreground=textColor,TextWrapping=TextWrapping.Wrap});var value=new TextBlock{FontSize=w.Kind=="clock"?26:14,TextWrapping=TextWrapping.Wrap,Foreground=textColor,IsTextSelectionEnabled=true};_widgetValues[w.Id]=value;p.Children.Add(value);
            if(w.Kind=="focus")p.Children.Add(Action(AstraOS.Core.UiText.T("Avvia focus"),(_,_)=>{int minutes=int.TryParse(w.Text,out var n)?Math.Clamp(n,1,180):25;int index=_widgets.FindIndex(x=>x.Id==w.Id);_widgets[index]=w with{Target=DateTimeOffset.Now.AddMinutes(minutes)};_widgetStore.Save(_widgets);UpdateWidgetValues();}));
            if(w.Kind=="workspace")p.Children.Add(Action(AstraOS.Core.UiText.T("Apri"),(_,_)=>{if(NavigationCatalog.Routes.Contains(w.Text))Navigate(w.Text);}));
            if(w.Automation is {} a){var enabled=new ToggleSwitch{Header=AstraOS.Core.UiText.T("Automazione"),IsOn=a.Enabled};enabled.Toggled+=(_,_)=>{int i=_widgets.FindIndex(x=>x.Id==w.Id);var current=_widgets[i];_widgets[i]=current with{Automation=current.Automation! with{Enabled=enabled.IsOn,NextDue=WidgetRules.Next(current.Automation!,DateTimeOffset.Now,TimeZoneInfo.Local)}};_widgetStore.Save(_widgets);};p.Children.Add(enabled);}
            var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=6};actions.Children.Add(Action(AstraOS.Core.UiText.T("Modifica"),(_,_)=>EditWidget(w)));actions.Children.Add(Action(AstraOS.Core.UiText.T("Rimuovi"),(_,_)=>{_widgets.RemoveAll(x=>x.Id==w.Id);_widgetStore.Save(_widgets);RenderWidgets();}));p.Children.Add(actions);
            var card=Card(p);card.Background=Token(w.Color=="ice"?"AstraIce":w.Color=="snow"?"AstraSnow":"AstraDeep");Grid.SetRow(card,row);Grid.SetColumn(card,col);Grid.SetColumnSpan(card,span);_widgetGrid.Children.Add(card);col+=span;if(col>=cols){row++;col=0;}
        }UpdateWidgetValues();
    }
    private void UpdateWidgetValues()
    {
        var now=DateTimeOffset.Now;foreach(var w in _widgets)if(_widgetValues.TryGetValue(w.Id,out var text))
        {
            var remaining=w.Target-now;text.Text=w.Kind switch
            {
                "clock"=>TimeZoneInfo.ConvertTime(now,w.TimeZone=="local"?TimeZoneInfo.Local:TimeZoneInfo.FindSystemTimeZoneById(w.TimeZone)).ToString("HH:mm:ss\nddd dd MMM"),
                "system"=>AstraOS.Core.UiText.F("CPU {0}\nRAM {1}\n{2}", V4CpuText.Text, V4RamText.Text, LiveSampleStatus.Text),
                "tasks"=>AstraOS.Core.UiText.F("{0} attività aperte · {1} completate", _v4Tasks.Count(t=>!t.Completed), _v4Tasks.Count(t=>t.Completed)),
                "countdown"=>remaining.HasValue&&remaining.Value>TimeSpan.Zero?AstraOS.Core.UiText.F("{0} giorni · {1:00}:{2:00}:{3:00}", remaining.Value.Days, remaining.Value.Hours, remaining.Value.Minutes, remaining.Value.Seconds):AstraOS.Core.UiText.T("Scadenza raggiunta"),
                "focus"=>w.Target.HasValue&&remaining>TimeSpan.Zero?$"{remaining!.Value.TotalMinutes:00}:{remaining.Value.Seconds:00}":AstraOS.Core.UiText.T("Pronto per una nuova sessione"),
                "automation"=>w.Automation is {Enabled:true} a?AstraOS.Core.UiText.F("Prossima esecuzione: {0:g}", a.NextDue.ToLocalTime()):AstraOS.Core.UiText.T("Configura un'automazione con Modifica"),
                _=>w.Text
            };
        }
    }
    private async Task TickWidgetsAsync()
    {
        if(_widgetTickBusy||_windowClosed)return;_widgetTickBusy=true;
        try
        {
            var now=DateTimeOffset.Now;
            foreach(var w in WidgetRules.Due(_widgets,now))
            {
                var a=w.Automation!;int i=_widgets.FindIndex(x=>x.Id==w.Id);_widgets[i]=w with{Automation=a with{NextDue=WidgetRules.Next(a,now,TimeZoneInfo.Local)}};_widgetStore.Save(_widgets);
                try
                {
                    switch(a.Action)
                    {
                        case "navigate":Navigate(a.Argument);break;
                        case "routine":
                            var routine=AssistantWorkspace.Routines.Concat(_personalRoutines).FirstOrDefault(r=>r.Id==a.Argument)??throw new InvalidOperationException(UiText.T("Routine non più disponibile."));
                            await RunAssistantRoutineAsync(routine,_windowLifetime.Token);break;
                        case "focus":
                            int focusIndex=_widgets.FindIndex(x=>x.Kind=="focus");
                            var focus=new WidgetSpec{Title=AstraOS.Core.UiText.T("Focus"),Kind="focus",Text=a.Argument,Target=now.AddMinutes(int.Parse(a.Argument))};
                            if(focusIndex>=0)_widgets[focusIndex]=_widgets[focusIndex] with{Target=focus.Target,Text=a.Argument};else if(_widgets.Count<32)_widgets.Add(focus);
                            _widgetStore.Save(_widgets);if(_v4LastPage=="widgets")RenderWidgets();break;
                        case "note":await Task.Run(()=>_notes.Add(w.Title,a.Argument,new[]{"widget","automazione"}),_windowLifetime.Token);break;
                    }
                    LogWidgetEvent(w.Title+": "+a.Argument);
                }catch(Exception ex){LogWidgetEvent(w.Title+": "+ex.Message);}
            }
            foreach(var w in _widgets.Where(w=>w.Kind=="focus"&&w.Target.HasValue&&w.Target<=now).ToArray()){int i=_widgets.FindIndex(x=>x.Id==w.Id);_widgets[i]=w with{Target=null};_widgetStore.Save(_widgets);LogWidgetEvent(w.Title+AstraOS.Core.UiText.T(": sessione terminata"));}
            if(_v4LastPage=="widgets")UpdateWidgetValues();
        }
        catch(Exception ex){StartupDiagnostics.Record("Widgets.tick.failed",ex.ToString());}finally{_widgetTickBusy=false;_widgetTimer.Interval=TimeSpan.FromSeconds(_v4LastPage=="widgets"?1:_preferences.EcoMode?15:5);}
    }
    private void LogWidgetEvent(string text){_widgetEvents.Enqueue(DateTime.Now.ToString("HH:mm")+" · "+text);while(_widgetEvents.Count>8)_widgetEvents.Dequeue();if(_widgetLog is not null)_widgetLog.Text=string.Join("\n",_widgetEvents.Reverse());StatusText.Text=text;}
    private async Task ImportWidgetsAsync()
    {
        try{var p=new FileOpenPicker();p.FileTypeFilter.Add(".json");WinRT.Interop.InitializeWithWindow.Initialize(p,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await p.PickSingleFileAsync();if(file is null)return;if(new FileInfo(file.Path).Length>128000)throw new InvalidDataException(AstraOS.Core.UiText.T("Pacchetto oltre 128 KB."));var incoming=WidgetRules.Import(await File.ReadAllTextAsync(file.Path));if(_widgets.Count+incoming.Count>32)throw new InvalidDataException(AstraOS.Core.UiText.T("Il pacchetto supera il limite di 32 widget."));foreach(var w in incoming)_widgets.Add(w with{Id=Guid.NewGuid().ToString("N"),Automation=w.Automation is {} a?a with{Enabled=false}:null});_widgetStore.Save(_widgets);RenderWidgets();_widgetStatus!.Text=AstraOS.Core.UiText.T("Widget importati. Le automazioni importate sono disattivate finché non le abiliti.");}catch(Exception ex){_widgetStatus!.Text=AstraOS.Core.UiText.T("Importazione: ")+ex.Message;}
    }
    private async Task ExportWidgetsAsync()
    {
        try{var p=new FileSavePicker{SuggestedFileName="astra-widgets"};p.FileTypeChoices.Add("Widget JSON",new[]{".json"});WinRT.Interop.InitializeWithWindow.Initialize(p,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await p.PickSaveFileAsync();if(file is not null){await File.WriteAllTextAsync(file.Path,WidgetRules.Export(_widgets));_widgetStatus!.Text=AstraOS.Core.UiText.T("Widget esportati in JSON leggibile: ")+file.Path;}}catch(Exception ex){_widgetStatus!.Text=AstraOS.Core.UiText.T("Esportazione: ")+ex.Message;}
    }
    private void WidgetsNav_Click(object sender,RoutedEventArgs e)=>Navigate("widgets");
}
