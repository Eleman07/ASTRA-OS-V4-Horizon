using System.Security.Principal;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private ComboBox? _studyDomainBox,_studyTrackBox;
    private Border? _simulationCard;
    private void InitializeStudyProfile()
    {
        string account=Environment.UserDomainName+"\\"+Environment.UserName;
        try{using var identity=WindowsIdentity.GetCurrent();account=identity.Name;}catch(System.Security.SecurityException){}
        UserAccountName.Text=account;UserAccountName.TextTrimming=TextTrimming.CharacterEllipsis;
        ToolTipService.SetToolTip(UserAccountName,account);
        var user=new StackPanel{Spacing=10};user.Children.Add(Label(AstraOS.Core.UiText.T("IL TUO ACCOUNT WINDOWS"),19));
        user.Children.Add(Label(account));user.Children.Add(new TextBox{Header=AstraOS.Core.UiText.T("Cartella del profilo utente"),Text=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),IsReadOnly=true});
        user.Children.Add(Label(AstraOS.Core.UiText.T("Identità della sessione Windows corrente. Le credenziali restano gestite da Windows."),12));
        user.Children.Add(Action(AstraOS.Core.UiText.T("Apri il mio profilo in File Explorer"),(_,_)=>{Navigate("files");_=LoadFilesAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));}));
        user.Children.Add(Action(AstraOS.Core.UiText.T("Apri il mio account nelle impostazioni Windows"),(_,_)=>OpenSystemSettings("ms-settings:yourinfo")));
        ProfilesPanel.Children.Insert(0,Card(user));
        var study=new StackPanel{Spacing=10};study.Children.Add(Label(AstraOS.Core.UiText.T("IL TUO PERCORSO STEM"),19));
        study.Children.Add(Label(AstraOS.Core.UiText.T("Seleziona l'area e l'indirizzo: lo Studio mostra soltanto i modelli pertinenti. Python 3D e Data Lab sono comuni a tutte le discipline."),12));
        _studyDomainBox=new ComboBox{Header=AstraOS.Core.UiText.T("Area di studio"),ItemsSource=StudyCatalog.Domains,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=_preferences.StudyDomain,HorizontalAlignment=HorizontalAlignment.Stretch};
        _studyTrackBox=new ComboBox{Header=AstraOS.Core.UiText.T("Indirizzo di ingegneria"),ItemsSource=StudyCatalog.Engineering,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=_preferences.EngineeringTrack,HorizontalAlignment=HorizontalAlignment.Stretch};
        _studyTrackBox.Visibility=_preferences.StudyDomain=="engineering"?Visibility.Visible:Visibility.Collapsed;
        var info=Label(StudyCatalog.Name(_preferences.StudyDomain,_preferences.EngineeringTrack),12);
        void Save()
        {
            if(_studyDomainBox.SelectedValue is not string d||_studyTrackBox.SelectedValue is not string t)return;
            _preferences.StudyDomain=d;_preferences.EngineeringTrack=t;_studyTrackBox.Visibility=d=="engineering"?Visibility.Visible:Visibility.Collapsed;
            try{_preferenceStore.Save(_preferences);info.Text=AstraOS.Core.UiText.F("{0} · {1} strumenti integrati disponibili · salvato", StudyCatalog.Name(d,t), SimulationLab.For(d,t).Count);}catch(Exception ex){info.Text=AstraOS.Core.UiText.T("Salvataggio: ")+ex.Message;}
        }
        _studyDomainBox.SelectionChanged+=(_,_)=>Save();_studyTrackBox.SelectionChanged+=(_,_)=>Save();
        study.Children.Add(_studyDomainBox);study.Children.Add(_studyTrackBox);study.Children.Add(info);study.Children.Add(Action(AstraOS.Core.UiText.T("Apri il mio Studio STEM"),(_,_)=>Navigate("stem")));
        ProfilesPanel.Children.Insert(1,Card(study));
    }
    private void AddSimulationPanel()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(AstraOS.Core.UiText.T("SIMULATION LAB / ")+StudyCatalog.Name(_preferences.StudyDomain,_preferences.EngineeringTrack).ToUpperInvariant(),18));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Modelli numerici didattici eseguiti sul dispositivo. Ipotesi e limiti sono riportati per ogni modello; non sostituiscono software validato per certificazione o progetto esecutivo."),12));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Cambia percorso di studio"),ProfilesNav_Click));
        var tools=SimulationLab.For(_preferences.StudyDomain,_preferences.EngineeringTrack);
        var choose=new ComboBox{Header=AstraOS.Core.UiText.T("Simulazione pertinente al tuo percorso"),ItemsSource=tools,DisplayMemberPath="Name",HorizontalAlignment=HorizontalAlignment.Stretch};
        var fields=new Grid{RowSpacing=8,ColumnSpacing=12};fields.ColumnDefinitions.Add(new ColumnDefinition());fields.ColumnDefinitions.Add(new ColumnDefinition());var assumptions=Label("",12);var state=Label(AstraOS.Core.UiText.T("Imposta i parametri, quindi esegui il modello."),12);var inputs=new Dictionary<string,NumberBox>();
        bool running=false;
        var run=Action(AstraOS.Core.UiText.T("Esegui simulazione"),async (_,_)=>
        {
            if(running || choose.SelectedItem is not SimulationTool tool)return;
            running=true;var values=inputs.ToDictionary(k=>k.Key,k=>k.Value.Value);choose.IsEnabled=false;foreach(var box in inputs.Values)box.IsEnabled=false;state.Text=AstraOS.Core.UiText.T("Calcolo numerico in corso…");
            try
            {
                var result=await Task.Run(()=>SimulationLab.Run(tool.Id,values,_windowLifetime.Token),_windowLifetime.Token);
                if(_windowClosed||_v4LastPage!="stem")return;
                _dataset=result.Data;_studioScene=result.Scene;state.Text=result.Summary+"\n"+result.Data.Rows.Count+AstraOS.Core.UiText.T(" campioni calcolati · disponibili per esportazione CSV e scena JSON.");
                if(_studioState is not null)_studioState.Text=result.Scene.Title+AstraOS.Core.UiText.T(" · risultato calcolato");
                await SetSimulationModelAsync(tool.Id,result,values);
                _studioHost?.StartBringIntoView(new BringIntoViewOptions{AnimationDesired=MotionAllowed});
            }
            catch(OperationCanceledException){}
            catch(Exception ex){if(!_windowClosed)state.Text=AstraOS.Core.UiText.T("Calcolo non eseguito: ")+ex.Message;}
            finally{running=false;if(!_windowClosed){choose.IsEnabled=true;foreach(var box in inputs.Values)box.IsEnabled=true;}}
        });
        choose.SelectionChanged+=(_,_)=>{fields.Children.Clear();fields.RowDefinitions.Clear();inputs.Clear();if(choose.SelectedItem is not SimulationTool tool)return;assumptions.Text=tool.Subjects+"\n"+tool.Assumptions;foreach(var p in tool.Parameters){var box=new NumberBox{Header=p.Label,Value=p.Default,Minimum=p.Min,Maximum=p.Max,SpinButtonPlacementMode=NumberBoxSpinButtonPlacementMode.Compact};int index=inputs.Count;if(index%2==0)fields.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});Grid.SetRow(box,index/2);Grid.SetColumn(box,index%2);box.HorizontalAlignment=HorizontalAlignment.Stretch;inputs[p.Id]=box;fields.Children.Add(box);}state.Text=AstraOS.Core.UiText.T("Parametri iniziali di esempio · nessun dato sperimentale.");};
        panel.Children.Add(choose);panel.Children.Add(Details(AstraOS.Core.UiText.T("Ipotesi e dettagli del modello"),assumptions));panel.Children.Add(fields);panel.Children.Add(run);panel.Children.Add(state);
        var exports=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};exports.Children.Add(Action(AstraOS.Core.UiText.T("Esporta risultati CSV…"),async(_,_)=>await ExportScientificAsync(false)));exports.Children.Add(Action(AstraOS.Core.UiText.T("Esporta scena 3D JSON…"),async(_,_)=>await ExportScientificAsync(true)));panel.Children.Add(exports);
        choose.SelectedIndex=0;_simulationCard=Card(panel);WorkspacePanel.Children.Add(_simulationCard);
    }
    private async Task ExportScientificAsync(bool scene)
    {
        try
        {
            if(scene?_studioScene is null:_dataset is null){StatusText.Text=AstraOS.Core.UiText.T("Esegui o importa prima un risultato.");return;}
            var picker=new FileSavePicker{SuggestedFileName=scene?"astra-scene":"astra-results"};picker.FileTypeChoices.Add(scene?"Scena ASTRA":"CSV",new[]{scene?".json":".csv"});WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSaveFileAsync();if(file is null||_windowClosed)return;
            string content=scene?System.Text.Json.JsonSerializer.Serialize(new{schemaVersion=1,title=_studioScene!.Title,axes=new{x=_studioScene.XLabel,y=_studioScene.YLabel,z=_studioScene.ZLabel},series=_studioScene.Series.Select(s=>new{name=s.Name,kind=s.Kind,points=s.Points.Select(p=>new[]{p.X,p.Y,p.Z}),rows=s.Rows,columns=s.Columns,color=s.Color})}):string.Join(",",_dataset!.Columns.Select(x=>"\""+x.Replace("\"","\"\"")+"\""))+"\n"+string.Join("\n",_dataset.Rows.Select(r=>string.Join(",",r.Select(v=>v?.ToString("G17",System.Globalization.CultureInfo.InvariantCulture)??""))));
            await File.WriteAllTextAsync(file.Path,content,_windowLifetime.Token);StatusText.Text=AstraOS.Core.UiText.T("Esportato: ")+file.Path;
        }catch(OperationCanceledException){}catch(Exception ex){StatusText.Text=AstraOS.Core.UiText.T("Esportazione: ")+ex.Message;}
    }
}
