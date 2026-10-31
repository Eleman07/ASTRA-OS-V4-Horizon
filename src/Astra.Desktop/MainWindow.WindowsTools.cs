using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private IReadOnlyList<WindowsTool> _windowsTools=Array.Empty<WindowsTool>();
    private TextBox? _toolSearch;private ComboBox? _toolCategory;private ListView? _toolList;private TextBlock? _toolState;private bool _toolsLoading;
    private void InitializeWindowsTools()
    {
        var p=new StackPanel{Spacing=10};p.Children.Add(Label(AstraOS.Core.UiText.T("STRUMENTI WINDOWS / TUTTO AL SUO POSTO"),20));p.Children.Add(Label(AstraOS.Core.UiText.T("App e strumenti realmente disponibili sul PC, inclusi quelli del menu Start. Le voci vengono deduplicate: nessuna reinstallazione per aggiungerle al catalogo."),12));
        _toolSearch=new TextBox{PlaceholderText=AstraOS.Core.UiText.T("Cerca uno strumento…"),MaxLength=120};_toolSearch.TextChanged+=(_,_)=>RenderWindowsTools();p.Children.Add(_toolSearch);_toolCategory=new ComboBox{Header=AstraOS.Core.UiText.T("Categoria"),ItemsSource=new[]{"Tutte"}.Concat(WindowsToolCatalog.Categories).ToArray(),SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};_toolCategory.SelectionChanged+=(_,_)=>RenderWindowsTools();p.Children.Add(_toolCategory);p.Children.Add(Action(AstraOS.Core.UiText.T("Aggiorna strumenti installati"),async(_,_)=>await RefreshWindowsToolsAsync()));_toolState=Label(AstraOS.Core.UiText.T("Rilevamento su richiesta."),12);p.Children.Add(_toolState);_toolList=new ListView{MaxHeight=610,SelectionMode=ListViewSelectionMode.None};p.Children.Add(_toolList);WindowsToolsPanel.Children.Add(Card(p));
        InstalledAppsHost.Children.Insert(1,Action(AstraOS.Core.UiText.T("Strumenti Windows per categoria"),(_,_)=>Navigate("windows-tools")));
    }
    private Task? _toolRefreshTask;
    private Task RefreshWindowsToolsAsync()=>_toolRefreshTask is { IsCompleted:false }?_toolRefreshTask:_toolRefreshTask=RefreshWindowsToolsCoreAsync();
    private async Task RefreshWindowsToolsCoreAsync()
    {
        if(_toolsLoading)return;_toolsLoading=true;_toolState!.Text=AstraOS.Core.UiText.T("Rilevamento app e strumenti…");
        try{await EnsureAppsAsync();_windowsTools=await WindowsToolCatalog.DiscoverAsync(_installedApps,_windowLifetime.Token);if(!_windowClosed)RenderWindowsTools();}catch(Exception ex){if(!_windowClosed)_toolState.Text=AstraOS.Core.UiText.T("Strumenti: ")+ex.Message;}finally{_toolsLoading=false;}
    }
    private void RenderWindowsTools()
    {
        if(_toolList is null||_toolState is null)return;_toolList.Items.Clear();int index=_toolCategory?.SelectedIndex??0;var visible=_windowsTools.Where(t=>(index==0||t.Category==WindowsToolCatalog.Categories[index-1])&&t.Name.Contains(_toolSearch?.Text??"",StringComparison.CurrentCultureIgnoreCase)).ToArray();
        foreach(var tool in visible){var row=new StackPanel{Spacing=4,Margin=new Thickness(0,4,0,6)};row.Children.Add(Action(tool.Name,(_,_)=>{try{WindowsToolCatalog.Open(tool);StatusText.Text=AstraOS.Core.UiText.T("Apertura richiesta: ")+tool.Name;}catch(Exception ex){StatusText.Text=ex.Message;}}));row.Children.Add(Label(tool.Category+" · "+tool.Origin,11));_toolList.Items.Add(row);}_toolState.Text=AstraOS.Core.UiText.F("{0} strumenti visualizzati · {1} disponibili · 7 categorie", visible.Length, _windowsTools.Count);
    }
    private void WindowsToolsNav_Click(object sender,RoutedEventArgs e)=>Navigate("windows-tools");
    private void QuickRoute_Click(object sender,RoutedEventArgs e){if(sender is FrameworkElement{Tag:string route})Navigate(route);}
}
