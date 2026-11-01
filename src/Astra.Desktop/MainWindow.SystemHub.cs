using System.Diagnostics;
using System.Runtime.InteropServices;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private StackPanel? _systemHubPanel,_clipboardShelfView;
    private readonly List<string> _clipboardShelf=new();
    [DllImport("user32.dll")]private static extern bool LockWorkStation();
    private void InitializeSystemHub()
    {
        _systemHubPanel=SystemHubPanel;
        SystemHubNavLabel.Text=UiText.T("Centro di controllo");
        var top=new StackPanel{Spacing=10};top.Children.Add(Label(UiText.T("Il tuo centro di controllo"),25));
        top.Children.Add(Label(UiText.T("File e archivi, finestre, applicazioni, appunti e strumenti del dispositivo. Le impostazioni hardware vengono aperte nel pannello Windows originale."),14));
        top.Children.Add(new Expander{Header=UiText.T("Windows, macOS e Ubuntu: cosa è disponibile"),Content=Label(UiText.T("ASTRA è un ambiente desktop per Windows 11. Offre equivalenti di Spotlight, Quick Look, Mission Control, dock e workspace Linux. Driver, account, rete, aggiornamenti e protezioni appartengono al sistema Windows. Le applicazioni macOS non vengono emulate; Linux richiede una distribuzione WSL installata."),12)});
        _systemHubPanel.Children.Add(Card(top));
        var search=new TextBox{Header=UiText.T("Cerca una funzione di sistema"),MaxLength=100};_systemHubPanel.Children.Add(search);
        var list=new StackPanel{Spacing=8};_systemHubPanel.Children.Add(new ScrollViewer{Content=list,MaxHeight=640,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        void Render()
        {
            list.Children.Clear();
            foreach(var group in DesktopCapabilities.All.Where(c=>(c.Name+" "+c.Category+" "+c.Description+" "+UiText.T(c.Name)+" "+UiText.T(c.Description)).Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)).GroupBy(c=>c.Category))
            {
                var p=new StackPanel{Spacing=7};p.Children.Add(Label(UiText.T(group.Key),18));
                foreach(var capability in group)
                {
                    var row=new StackPanel{Spacing=3};row.Children.Add(Action(UiText.T(capability.Name),(_,_)=>
                    {
                        try
                        {
                            if(capability.Mode=="settings")OpenSystemSettings(capability.Target);
                            else if(capability.Target=="search")HorizonSearchBox.Focus(FocusState.Programmatic);
                            else Navigate(capability.Target);
                        }catch(Exception ex){StatusText.Text=ex.Message;}
                    }));row.Children.Add(Label(UiText.T(capability.Description),12));p.Children.Add(row);
                }
                list.Children.Add(Card(p));
            }
        }
        search.TextChanged+=(_,_)=>Render();Render();
        var shelf=new StackPanel{Spacing=8};shelf.Children.Add(Label(UiText.T("Scaffale appunti della sessione"),20));shelf.Children.Add(Label(UiText.T("Acquisisci soltanto il testo che scegli. Fino a 12 elementi, conservati in memoria per questa sessione; nessuna lettura continua degli appunti."),12));
        shelf.Children.Add(Action(UiText.T("Acquisisci testo dagli appunti"),async(_,_)=>
        {
            try{var data=Clipboard.GetContent();if(!data.Contains(StandardDataFormats.Text))return;string text=await data.GetTextAsync();if(text.Length>20000)throw new InvalidDataException(UiText.T("Appunti oltre 20.000 caratteri."));if(string.IsNullOrWhiteSpace(text))return;_clipboardShelf.Remove(text);_clipboardShelf.Insert(0,text);if(_clipboardShelf.Count>12)_clipboardShelf.RemoveAt(12);RenderClipboardShelf();}catch(Exception ex){StatusText.Text=ex.Message;}
        }));
        shelf.Children.Add(Action(UiText.T("Svuota scaffale della sessione"),(_,_)=>{_clipboardShelf.Clear();RenderClipboardShelf();}));_clipboardShelfView=new StackPanel{Spacing=7};shelf.Children.Add(_clipboardShelfView);_systemHubPanel.Children.Insert(1,Card(shelf));
        var power=new StackPanel{Spacing=8};power.Children.Add(Label(UiText.T("Sessione del dispositivo"),18));
        power.Children.Add(Action(UiText.T("Blocca il PC"),(_,_)=>LockWorkStation()));
        power.Children.Add(Action(UiText.T("Riavvia Windows…"),async(_,_)=>await RequestPowerAsync(true)));
        power.Children.Add(Action(UiText.T("Arresta Windows…"),async(_,_)=>await RequestPowerAsync(false)));
        power.Children.Add(Action(UiText.T("Apri Cestino"),(_,_)=>{var psi=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")){UseShellExecute=true};psi.ArgumentList.Add("shell:RecycleBinFolder");Process.Start(psi);}));_systemHubPanel.Children.Add(Card(power));
    }
    private void RenderClipboardShelf()
    {
        _clipboardShelfView!.Children.Clear();
        foreach(var text in _clipboardShelf)
        {
            var row=new StackPanel{Spacing=5};row.Children.Add(Label(text[..Math.Min(text.Length,500)],12));
            row.Children.Add(Action(UiText.T("Copia questo elemento"),(_,_)=>{var data=new DataPackage();data.SetText(text);Clipboard.SetContent(data);}));
            row.Children.Add(Action(UiText.T("Usa nella conversazione"),(_,_)=>{OpenAssistant();_assistantWindow?.SetInput(text);}));_clipboardShelfView.Children.Add(Card(row));
        }
    }
    private async Task RequestPowerAsync(bool restart)
    {
        var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T(restart?"Riavviare Windows?":"Arrestare Windows?"),Content=UiText.T("Salva il lavoro nelle applicazioni aperte prima di continuare."),PrimaryButtonText=UiText.T(restart?"Riavvia":"Arresta"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};
        if(await dialog.ShowAsync()!=ContentDialogResult.Primary)return;
        var psi=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"shutdown.exe")){UseShellExecute=false,CreateNoWindow=true};psi.ArgumentList.Add(restart?"/r":"/s");psi.ArgumentList.Add("/t");psi.ArgumentList.Add("0");Process.Start(psi);
    }
}
