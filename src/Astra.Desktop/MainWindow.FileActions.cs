using System.Text;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private bool _fileMutationBusy;
    private CancellationTokenSource? _fileOperation;
    private (string Before,string After)? _fileUndo;
    private void InitializeFileActions()
    {
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        row.Children.Add(Action(UiText.T("Anteprima / Quick Look"),async(_,_)=>await FileActionAsync("preview")));
        var menu=new MenuFlyout();
        foreach(var (name,action) in new[]{("Nuova cartella","folder"),("Copia negli appunti","clipboard-copy"),("Taglia","clipboard-cut"),("Incolla qui","paste"),("Copia in…","copy"),("Sposta in…","move"),("Rinomina…","rename"),("Comprimi in ZIP","zip"),("Estrai ZIP","extract"),("Sposta nel Cestino…","trash"),("Annulla ultimo spostamento o rinomina","undo"),("Analizza con l'assistente","assistant")})
        {var item=new MenuFlyoutItem{Text=UiText.T(name)};item.Click+=async(_,_)=>await FileActionAsync(action);menu.Items.Add(item);}
        row.Children.Add(new DropDownButton{Content=UiText.T("Azioni sui file"),Flyout=menu});
        row.Children.Add(Action(UiText.T("Cerca anche nelle sottocartelle"),async(_,_)=>await SearchFileTreeAsync()));
        row.Children.Add(Action(UiText.T("Annulla operazione"),(_,_)=>_fileOperation?.Cancel()));FilesPanel.Children.Insert(0,row);
        _filesList!.KeyDown+=async(_,e)=>{if(e.Key==Windows.System.VirtualKey.Space){e.Handled=true;await FileActionAsync("preview");}};
        foreach(var (key,action) in new[]{(Windows.System.VirtualKey.C,"clipboard-copy"),(Windows.System.VirtualKey.X,"clipboard-cut"),(Windows.System.VirtualKey.V,"paste")})
        {
            var accelerator=new Microsoft.UI.Xaml.Input.KeyboardAccelerator{Key=key,Modifiers=Windows.System.VirtualKeyModifiers.Control};accelerator.Invoked+=async(_,e)=>{e.Handled=true;await FileActionAsync(action);};_filesList.KeyboardAccelerators.Add(accelerator);
        }
        Closed+=(_,_)=>_fileOperation?.Cancel();
    }
    private async Task<string?> FileNameDialogAsync(string title,string value)
    {
        var input=new TextBox{Text=value,MaxLength=160};var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T(title),Content=input,PrimaryButtonText=UiText.T("Conferma"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};
        return await dialog.ShowAsync()==ContentDialogResult.Primary?input.Text:null;
    }
    private async Task FileActionAsync(string action)
    {
        if(_fileMutationBusy)return;_fileMutationBusy=true;_fileOperation?.Dispose();_fileOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);var ct=_fileOperation.Token;
        try
        {
            if(action=="folder")
            {
                string? name=await FileNameDialogAsync("Nuova cartella","");if(name is null)return;
                await Task.Run(()=>SafeFileOperations.CreateFolder(_filesDirectory,name),ct);_fileUndo=null;
            }
            else if(action=="paste")
            {
                var clipboard=Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                if(!clipboard.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))throw new InvalidDataException(UiText.T("Gli appunti non contengono file o cartelle."));
                var items=await clipboard.GetStorageItemsAsync();if(items.Count>50)throw new InvalidDataException(UiText.T("Massimo 50 elementi per incolla."));
                foreach(var item in items){ct.ThrowIfCancellationRequested();if(clipboard.RequestedOperation==Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move)await Task.Run(()=>SafeFileOperations.Move(item.Path,_filesDirectory),ct);else await Task.Run(()=>SafeFileOperations.CopyAsync(item.Path,_filesDirectory,ct),ct);}
                _fileUndo=null;
            }
            else if(action=="undo")
            {
                if(_fileUndo is not {} undo)throw new InvalidOperationException(UiText.T("Nessuno spostamento o rinomina da annullare."));
                await Task.Run(()=>{SafeFileOperations.NoLinks(undo.After);SafeFileOperations.DirectoryPath(Path.GetDirectoryName(undo.Before)!);if(File.Exists(undo.Before)||Directory.Exists(undo.Before))throw new IOException(UiText.T("La posizione originale è occupata."));if(Directory.Exists(undo.After))Directory.Move(undo.After,undo.Before);else File.Move(undo.After,undo.Before);},ct);_fileUndo=null;
            }
            else
            {
                if(_filesList?.SelectedItem is not ExplorerEntry entry)throw new InvalidOperationException(UiText.T("Seleziona prima un file o una cartella."));
                SafeFileOperations.NoLinks(entry.FullPath);
                switch(action)
                {
                    case "clipboard-copy":case "clipboard-cut":
                        Windows.Storage.IStorageItem storage=entry.IsDirectory?await Windows.Storage.StorageFolder.GetFolderFromPathAsync(entry.FullPath):await Windows.Storage.StorageFile.GetFileFromPathAsync(entry.FullPath);
                        var package=new Windows.ApplicationModel.DataTransfer.DataPackage{RequestedOperation=action=="clipboard-cut"?Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move:Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy};package.SetStorageItems(new[]{storage});Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);_filesStatus!.Text=UiText.T("Selezione negli appunti. Usa Incolla nella cartella di destinazione.");return;
                    case "preview":await PreviewFileAsync(entry);return;
                    case "assistant":
                        if(entry.IsDirectory)throw new InvalidDataException(UiText.T("Seleziona un file da allegare."));
                        var data=await ReadSecondBrainInputAsync(entry.FullPath,ct);OpenAssistant("File · "+entry.Name);_assistantWindow?.SetInput(data.Text);return;
                    case "rename":
                        string? name=await FileNameDialogAsync("Rinomina",entry.Name);if(name is null)return;
                        string renamed=await Task.Run(()=>SafeFileOperations.Rename(entry.FullPath,name),ct);_fileUndo=(entry.FullPath,renamed);break;
                    case "copy":case "move":
                        var picker=new FolderPicker();picker.FileTypeFilter.Add("*");WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var folder=await picker.PickSingleFolderAsync();if(folder is null)return;
                        if(action=="copy"){await Task.Run(()=>SafeFileOperations.CopyAsync(entry.FullPath,folder.Path,ct),ct);_fileUndo=null;}
                        else{string moved=await Task.Run(()=>SafeFileOperations.Move(entry.FullPath,folder.Path),ct);_fileUndo=(entry.FullPath,moved);}break;
                    case "zip":await Task.Run(()=>SafeFileOperations.ZipFileAsync(entry.FullPath,ct),ct);_fileUndo=null;break;
                    case "extract":if(!entry.Name.EndsWith(".zip",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException(UiText.T("Seleziona un archivio ZIP."));await Task.Run(()=>SafeFileOperations.ExtractZipAsync(entry.FullPath,ct),ct);_fileUndo=null;break;
                    case "trash":
                        if(Path.GetPathRoot(entry.FullPath)==entry.FullPath)throw new InvalidDataException(UiText.T("Non è possibile cestinare una radice."));
                        var confirm=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=UiText.T("Spostare nel Cestino?"),Content=entry.Name,PrimaryButtonText=UiText.T("Continua"),CloseButtonText=UiText.T("Annulla"),DefaultButton=ContentDialogButton.Close};if(await confirm.ShowAsync()!=ContentDialogResult.Primary)return;
                        // Native Windows dialogs retain control if recycling is unavailable.
                        if(entry.IsDirectory)Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(entry.FullPath,Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
                        else Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(entry.FullPath,Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
                        _fileUndo=null;break;
                }
            }
            await LoadFilesAsync(_filesDirectory);await RefreshHomeFilesAsync();_filesStatus!.Text=UiText.T("Operazione completata.");
        }catch(OperationCanceledException){_filesStatus!.Text=UiText.T("Operazione annullata.");}catch(Exception ex){_filesStatus!.Text=UiText.T(ex.Message);}
        finally{_fileMutationBusy=false;}
    }
    private async Task PreviewFileAsync(ExplorerEntry entry)
    {
        var p=new StackPanel{Spacing=10};p.Children.Add(Label(entry.FullPath,12));p.Children.Add(Label(entry.Kind+" · "+entry.Size+" · "+entry.Modified,13));
        if(entry.IsDirectory)p.Children.Add(Label(UiText.F("Elementi visibili: {0}",FileExplorerService.ReadDirectory(entry.FullPath).Entries.Count),14));
        else if(entry.Name.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase)&&new FileInfo(entry.FullPath).Length<512L*1024*1024)
        {
            var document=await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(await Windows.Storage.StorageFile.GetFileFromPathAsync(entry.FullPath));
            var image=new Image{MaxHeight=530,MaxWidth=900,Stretch=Microsoft.UI.Xaml.Media.Stretch.Uniform};var pageNumber=new NumberBox{Header=UiText.T("Pagina PDF"),Minimum=1,Maximum=document.PageCount,Value=1,SpinButtonPlacementMode=NumberBoxSpinButtonPlacementMode.Compact};
            async Task RenderPage(){using var page=document.GetPage((uint)pageNumber.Value-1);using var memory=new Windows.Storage.Streams.InMemoryRandomAccessStream();await page.RenderToStreamAsync(memory,new Windows.Data.Pdf.PdfPageRenderOptions{DestinationWidth=1000});var bitmap=new BitmapImage();await bitmap.SetSourceAsync(memory);image.Source=bitmap;}
            await RenderPage();pageNumber.ValueChanged+=async(_,_)=>{if(double.IsFinite(pageNumber.Value)&&pageNumber.Value>=1&&pageNumber.Value<=document.PageCount){try{await RenderPage();}catch(Exception ex){StatusText.Text=UiText.T(ex.Message);}}};p.Children.Add(pageNumber);p.Children.Add(image);
        }
        else if(new[]{".png",".jpg",".jpeg",".bmp"}.Contains(Path.GetExtension(entry.FullPath).ToLowerInvariant())&&new FileInfo(entry.FullPath).Length<20_000_000)
            p.Children.Add(new Image{Source=new BitmapImage(new Uri(entry.FullPath)){DecodePixelWidth=900},MaxWidth=900,MaxHeight=550,Stretch=Microsoft.UI.Xaml.Media.Stretch.Uniform});
        else if(new[]{".txt",".md",".csv",".json",".py",".cs",".log",".srt",".xml",".yaml",".yml"}.Contains(Path.GetExtension(entry.FullPath).ToLowerInvariant()))
        {
            using var stream=File.OpenRead(entry.FullPath);var bytes=new byte[Math.Min(64000,stream.Length)];int read=await stream.ReadAsync(bytes);p.Children.Add(new TextBox{Text=Encoding.UTF8.GetString(bytes,0,read),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MaxHeight=490});if(stream.Length>64000)p.Children.Add(Label(UiText.T("Anteprima limitata ai primi 64 KB."),11));
        }
        var dialog=new ContentDialog{XamlRoot=AppRoot.XamlRoot,Title=entry.Name,Content=new ScrollViewer{Content=p,MaxHeight=610},CloseButtonText=UiText.T("Chiudi"),MaxWidth=980};await dialog.ShowAsync();
    }
    private async Task SearchFileTreeAsync()
    {
        if(_fileMutationBusy)return;string query=_filesSearch?.Text.Trim()??"";if(query.Length<2){_filesStatus!.Text=UiText.T("Inserisci almeno due caratteri nel campo ricerca.");return;}
        _fileMutationBusy=true;_fileOperation?.Dispose();_fileOperation=CancellationTokenSource.CreateLinkedTokenSource(_windowLifetime.Token);var ct=_fileOperation.Token;
        try
        {
            _filesStatus!.Text=UiText.T("Ricerca nelle sottocartelle…");string root=_filesDirectory;
            var results=await Task.Run(()=>
            {
                var found=new List<ExplorerEntry>();var pending=new Queue<string>();pending.Enqueue(root);int visited=0;
                while(pending.TryDequeue(out var directory)&&visited++<5000&&found.Count<500)
                {
                    ct.ThrowIfCancellationRequested();
                    try{SafeFileOperations.NoLinks(directory);foreach(var entry in FileExplorerService.ReadDirectory(directory,ct).Entries){ct.ThrowIfCancellationRequested();if(entry.Name.Contains(query,StringComparison.CurrentCultureIgnoreCase)&&found.Count<500)found.Add(entry);if(entry.IsDirectory&&pending.Count<5000&&(File.GetAttributes(entry.FullPath)&FileAttributes.ReparsePoint)==0)pending.Enqueue(entry.FullPath);}}
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){}
                }
                return found;
            },ct);
            _fileEntries=results;RenderFiles();_filesStatus.Text=UiText.F("{0} risultati · limite 500 risultati / 5.000 cartelle. Le cartelle non leggibili o collegate sono saltate.",results.Count);
        }catch(OperationCanceledException){_filesStatus!.Text=UiText.T("Ricerca annullata.");}catch(Exception ex){_filesStatus!.Text=UiText.T(ex.Message);}finally{_fileMutationBusy=false;}
    }
}
