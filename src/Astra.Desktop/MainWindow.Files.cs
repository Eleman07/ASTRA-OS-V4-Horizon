using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.DataTransfer;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private string _filesDirectory=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private IReadOnlyList<ExplorerEntry> _fileEntries=Array.Empty<ExplorerEntry>();
    private TextBox? _filesAddress,_filesSearch;
    private ListView? _filesList;
    private ComboBox? _filesSort;
    private TextBlock? _filesStatus;
    private int _filesRevision;
    private bool _openingFile;
    private GridView? _homeFiles;
    private bool _homeFilesLoading;
    private void InitializeFiles()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(AstraOS.Core.UiText.T("FILE EXPLORER"),21));
        var shortcuts=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        foreach(var (name,path) in new[]{("Desktop",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),("Documenti",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),("Download",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")),("Home utente",Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))})
            shortcuts.Children.Add(Action(name,async(_,_)=>await LoadFilesAsync(path)));
        panel.Children.Add(shortcuts);
        _filesAddress=new TextBox{Header=AstraOS.Core.UiText.T("Cartella"),Text=_filesDirectory};
        _filesAddress.KeyDown+=async(_,e)=>{if(e.Key==Windows.System.VirtualKey.Enter){e.Handled=true;await LoadFilesAsync(_filesAddress.Text);}};
        panel.Children.Add(_filesAddress);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        actions.Children.Add(Action(AstraOS.Core.UiText.T("↑ Cartella superiore"),async(_,_)=>await LoadFilesAsync(Directory.GetParent(_filesDirectory)?.FullName??_filesDirectory)));
        actions.Children.Add(Action(AstraOS.Core.UiText.T("Vai / aggiorna"),async(_,_)=>await LoadFilesAsync(_filesAddress.Text)));
        actions.Children.Add(Action(AstraOS.Core.UiText.T("Explorer Windows"),(_,_)=>OpenWindowsExplorer(_filesDirectory)));panel.Children.Add(actions);
        _filesSearch=new TextBox{PlaceholderText=AstraOS.Core.UiText.T("Cerca in questa cartella…"),MaxLength=120};_filesSearch.TextChanged+=(_,_)=>RenderFiles();panel.Children.Add(_filesSearch);
        _filesSort=new ComboBox{Header=AstraOS.Core.UiText.T("Ordina per"),ItemsSource=new[]{"Nome","Modifica recente","Tipo","Dimensione"},SelectedIndex=0};_filesSort.SelectionChanged+=(_,_)=>RenderFiles();panel.Children.Add(_filesSort);
        _filesStatus=Label(AstraOS.Core.UiText.T("Scegli una cartella."),12);panel.Children.Add(_filesStatus);
        _filesList=new ListView{SelectionMode=ListViewSelectionMode.Single,MaxHeight=470,HorizontalContentAlignment=HorizontalAlignment.Stretch};
        _filesList.ItemTemplate=(DataTemplate)XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><Grid Padding="6,9" ColumnSpacing="12"><Grid.ColumnDefinitions><ColumnDefinition Width="26"/><ColumnDefinition Width="*"/><ColumnDefinition Width="80"/><ColumnDefinition Width="90"/><ColumnDefinition Width="140"/></Grid.ColumnDefinitions><FontIcon FontFamily="Segoe Fluent Icons" Glyph="{Binding Glyph}" FontSize="20"/><TextBlock Grid.Column="1" Text="{Binding Name}" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/><TextBlock Grid.Column="2" Text="{Binding Kind}" VerticalAlignment="Center"/><TextBlock Grid.Column="3" Text="{Binding Size}" VerticalAlignment="Center"/><TextBlock Grid.Column="4" Text="{Binding Modified}" VerticalAlignment="Center"/></Grid></DataTemplate>
            """);
        _filesList.DoubleTapped+=async(_,_)=>await OpenSelectedFileAsync();
        _filesList.KeyDown+=async(_,e)=>{if(e.Key==Windows.System.VirtualKey.Enter){e.Handled=true;await OpenSelectedFileAsync();}};panel.Children.Add(_filesList);
        var selected=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        selected.Children.Add(Action(AstraOS.Core.UiText.T("Apri selezione"),async(_,_)=>await OpenSelectedFileAsync()));
        selected.Children.Add(Action(AstraOS.Core.UiText.T("Copia percorso"),(_,_)=>{if(_filesList.SelectedItem is ExplorerEntry entry){var data=new DataPackage();data.SetText(entry.FullPath);Clipboard.SetContent(data);_filesStatus.Text=AstraOS.Core.UiText.T("Percorso copiato.");}}));
        panel.Children.Add(selected);FilesPanel.Children.Add(Card(panel));
        _homeFiles = new GridView { SelectionMode = ListViewSelectionMode.Single, MaxHeight = 430, IsItemClickEnabled = false };
        _homeFiles.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><ItemsWrapGrid Orientation=\"Horizontal\" ItemWidth=\"112\" ItemHeight=\"114\"/></ItemsPanelTemplate>");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_homeFiles,AstraOS.Core.UiText.T("Desktop"));
        _homeFiles.DoubleTapped += async (_,_) => { if(_homeFiles.SelectedItem is GridViewItem { Tag: ExplorerEntry entry }) await OpenEntryAsync(entry); };
        _homeFiles.KeyDown += async (_,e) => { if(e.Key==Windows.System.VirtualKey.Enter && _homeFiles.SelectedItem is GridViewItem { Tag: ExplorerEntry entry }) { e.Handled=true;await OpenEntryAsync(entry); } };
        var menu=new MenuFlyout();var refresh=new MenuFlyoutItem{Text=AstraOS.Core.UiText.T("Aggiorna Desktop")};refresh.Click+=async(_,_)=>await RefreshHomeFilesAsync();menu.Items.Add(refresh);
        var explorer=new MenuFlyoutItem{Text=AstraOS.Core.UiText.T("Apri File Explorer")};explorer.Click+=FilesNav_Click;menu.Items.Add(explorer);_homeFiles.ContextFlyout=menu;
        HomePanel.Children.Add(_homeFiles);
    }
    private async Task RefreshHomeFilesAsync()
    {
        if(_homeFiles is null||_homeFilesLoading)return;_homeFilesLoading=true;
        try
        {
            var entries=await Task.Run(()=>FileExplorerService.ReadDesktop(_windowLifetime.Token),_windowLifetime.Token);if(_windowClosed)return;
            _homeFiles.Items.Clear();
            var images=new List<(ExplorerEntry Entry,Grid Icon)>();
            foreach(var entry in entries)
            {
                var icon=new Grid{Width=52,Height=52};icon.Children.Add(new FontIcon{FontFamily=new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),Glyph=entry.Glyph,FontSize=38,Foreground=Token("AstraTextLight")});
                var tile=new StackPanel{Spacing=7,Width=98,Padding=new Thickness(3)};tile.Children.Add(icon);
                tile.Children.Add(new TextBlock{Text=entry.Name,MaxLines=2,TextWrapping=TextWrapping.Wrap,TextTrimming=TextTrimming.CharacterEllipsis,TextAlignment=TextAlignment.Center,FontSize=12,Foreground=Token("AstraTextLight")});
                var item=new GridViewItem{Content=tile,Tag=entry,Padding=new Thickness(3),HorizontalContentAlignment=HorizontalAlignment.Center};ToolTipService.SetToolTip(item,entry.FullPath);Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item,entry.Name);_homeFiles.Items.Add(item);images.Add((entry,icon));
            }
            foreach(var group in images.Chunk(6))await Task.WhenAll(group.Select(async pair=>{var source=await DesktopIconService.GetAsync(pair.Entry);if(source is null||_windowClosed)return;pair.Icon.Children.Clear();pair.Icon.Children.Add(new Image{Source=source,Width=48,Height=48});}));
        }
        catch(OperationCanceledException){}catch(Exception ex){if(!_windowClosed)StatusText.Text=AstraOS.Core.UiText.T("Desktop: ")+ex.Message;}finally{_homeFilesLoading=false;}
    }
    private async Task LoadFilesAsync(string directory)
    {
        if(_filesStatus is null||_filesAddress is null)return;int revision=++_filesRevision;_filesStatus.Text=AstraOS.Core.UiText.T("Caricamento cartella…");
        try
        {
            if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException(AstraOS.Core.UiText.T("Inserisci o scegli una cartella."));
            var path=Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory.Trim().Trim('"')),_filesDirectory);
            var result=await Task.Run(()=>FileExplorerService.ReadDirectory(path,_windowLifetime.Token),_windowLifetime.Token);
            if(_windowClosed||revision!=_filesRevision)return;
            _filesDirectory=result.Path;_fileEntries=result.Entries;_filesAddress.Text=result.Path;RenderFiles();
            if(result.Truncated)_filesStatus.Text+=AstraOS.Core.UiText.T(" · prime 2.000 voci");
            if(result.Unreadable>0)_filesStatus.Text+=AstraOS.Core.UiText.F(" · {0} voci non leggibili", result.Unreadable);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!_windowClosed&&revision==_filesRevision)_filesStatus.Text=AstraOS.Core.UiText.T("Cartella non disponibile: ")+ex.Message;}
    }
    private void RenderFiles()
    {
        if(_filesList is null||_filesStatus is null)return;
        IEnumerable<ExplorerEntry> entries=_fileEntries.Where(e=>e.Name.Contains(_filesSearch?.Text??"",StringComparison.CurrentCultureIgnoreCase));
        entries=(_filesSort?.SelectedIndex??0) switch{1=>entries.OrderByDescending(e=>e.LastModified),2=>entries.OrderBy(e=>e.Kind).ThenBy(e=>e.Name),3=>entries.OrderByDescending(e=>e.Bytes),_=>entries.OrderByDescending(e=>e.IsDirectory).ThenBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase)};
        var visible=entries.ToArray();_filesList.ItemsSource=visible;_filesStatus.Text=AstraOS.Core.UiText.F("{0} voci · {1} cartelle · {2} file", visible.Length, visible.Count(e=>e.IsDirectory), visible.Count(e=>!e.IsDirectory));
    }
    private async Task OpenSelectedFileAsync(){if(_filesList?.SelectedItem is not ExplorerEntry entry)return;if(entry.IsDirectory)await LoadFilesAsync(entry.FullPath);else await OpenEntryAsync(entry);}
    private async Task OpenEntryAsync(ExplorerEntry entry)
    {
        if(_openingFile)return;_openingFile=true;
        try
        {
            if(entry.IsDirectory){Navigate("files");await LoadFilesAsync(entry.FullPath);return;}
            if(!File.Exists(entry.FullPath))throw new FileNotFoundException(AstraOS.Core.UiText.T("File non più disponibile; aggiorna la cartella."));
            if(new[]{".exe",".msi",".cmd",".bat",".ps1",".vbs",".js",".lnk",".url",".scr",".com"}.Contains(Path.GetExtension(entry.FullPath),StringComparer.OrdinalIgnoreCase))
            {var confirm=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=AstraOS.Core.UiText.T("Aprire programma o collegamento?"),Content=entry.FullPath,PrimaryButtonText=AstraOS.Core.UiText.T("Apri"),CloseButtonText=AstraOS.Core.UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};if(await confirm.ShowAsync()!=ContentDialogResult.Primary)return;}
            Process.Start(new ProcessStartInfo(entry.FullPath){UseShellExecute=true});StatusText.Text=AstraOS.Core.UiText.T("Apertura richiesta: ")+entry.Name;
        }
        catch(Exception ex){if(!_windowClosed)StatusText.Text=AstraOS.Core.UiText.T("Impossibile aprire: ")+ex.Message;}finally{_openingFile=false;}
    }
    private void OpenWindowsExplorer(string path){try{var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")){UseShellExecute=true};start.ArgumentList.Add(path);Process.Start(start);}catch(Exception ex){StatusText.Text=AstraOS.Core.UiText.T("Explorer Windows: ")+ex.Message;}}
    private void FilesNav_Click(object sender,RoutedEventArgs e)=>Navigate("files");
    private void OpenChrome_Click(object sender,RoutedEventArgs e){try{ChromeLauncher.Open();}catch(Exception ex){StatusText.Text=ex.Message;}}
}
