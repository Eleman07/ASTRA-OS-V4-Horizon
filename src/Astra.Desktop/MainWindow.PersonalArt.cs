using System.Security.Cryptography;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private TextBlock? _artState;
    private void InitializePersonalArt()
    {
        var panel=new StackPanel{Spacing=10};panel.Children.Add(Label(AstraOS.Core.UiText.T("IMMAGINI E SFONDI ANIMATI"),18));
        panel.Children.Add(Label(AstraOS.Core.UiText.T("Scegli un'immagine per il banner o lo sfondo: PNG, JPEG, BMP o GIF animata, fino a 32 MB. Una copia viene conservata nel tuo profilo ASTRA. Eco, movimento ridotto e finestra inattiva fermano la riproduzione."),12));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Scegli immagine del banner…"),async(_,_)=>await PickArtAsync(true)));panel.Children.Add(Action(AstraOS.Core.UiText.T("Scegli sfondo della Home…"),async(_,_)=>await PickArtAsync(false)));
        panel.Children.Add(Action(AstraOS.Core.UiText.T("Ripristina immagini originali"),async(_,_)=>{_preferences.BannerImagePath="";_preferences.WallpaperImagePath="";try{_preferenceStore.Save(_preferences);await LoadPersonalArtAsync();}catch(Exception ex){_artState!.Text=ex.Message;}}));
        _artState=Label(AstraOS.Core.UiText.T("Immagini originali ASTRA."),12);panel.Children.Add(_artState);ThemePanel.Children.Add(Card(panel));
        var browser=new StackPanel{Spacing=10};browser.Children.Add(Label(AstraOS.Core.UiText.T("BROWSER / GOOGLE CHROME"),18));browser.Children.Add(Label(AstraOS.Core.UiText.T("I collegamenti web di ASTRA si aprono in Chrome. Per le associazioni di Windows usa la pagina ufficiale App predefinite."),12));browser.Children.Add(Action(AstraOS.Core.UiText.T("Apri Chrome"),OpenChrome_Click));browser.Children.Add(Action(AstraOS.Core.UiText.T("Chrome nelle App predefinite di Windows"),(_,_)=>OpenSystemSettings("ms-settings:defaultapps?registeredAppMachine=Google%20Chrome")));ThemePanel.Children.Add(Card(browser));
        AppRoot.Loaded+=async(_,_)=>await LoadPersonalArtAsync();Activated+=(_,_)=>UpdateAnimatedImages();Closed+=(_,_)=>UpdateAnimatedImages();
    }
    private async Task PickArtAsync(bool banner)
    {
        try
        {
            var picker=new FileOpenPicker();foreach(var ext in new[]{".png",".jpg",".jpeg",".gif",".bmp"})picker.FileTypeFilter.Add(ext);WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(this));var file=await picker.PickSingleFileAsync();if(file is null||_windowClosed)return;
            var info=new FileInfo(file.Path);if(info.Length>32_000_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Immagine superiore a 32 MB."));
            var bitmap=new BitmapImage{AutoPlay=false,DecodePixelWidth=_lightDevice?960:1920};using(var stream=await file.OpenReadAsync())await bitmap.SetSourceAsync(stream);
            if(bitmap.PixelWidth==0)throw new InvalidDataException(AstraOS.Core.UiText.T("Immagine non decodificabile."));
            string directory=Path.Combine(AstraOS.Core.AstraPaths.UserDataRoot,"personalization");Directory.CreateDirectory(directory);
            using var source=File.OpenRead(file.Path);string hash=Convert.ToHexString(await SHA256.HashDataAsync(source));string target=Path.Combine(directory,hash+Path.GetExtension(file.Path).ToLowerInvariant());if(!File.Exists(target))File.Copy(file.Path,target,false);
            if(banner)_preferences.BannerImagePath=target;else _preferences.WallpaperImagePath=target;_preferenceStore.Save(_preferences);await LoadPersonalArtAsync();
        }catch(Exception ex){if(_artState is not null)_artState.Text=AstraOS.Core.UiText.T("Immagine: ")+ex.Message;}
    }
    private async Task LoadPersonalArtAsync()
    {
        try
        {
            foreach(var (image,path,fallback) in new[]{(BannerArtworkImage,_preferences.BannerImagePath,"banner_art_v4.png"),(V4Wallpaper,_preferences.WallpaperImagePath,"wallpaper_v4.png")})
            {
                string chosen=string.IsNullOrWhiteSpace(path)?Path.Combine(AppContext.BaseDirectory,"Assets",fallback):path;
                if(!File.Exists(chosen)){chosen=Path.Combine(AppContext.BaseDirectory,"Assets",fallback);if(_artState is not null)_artState.Text=AstraOS.Core.UiText.T("Immagine personalizzata assente: ripristino visivo dell'originale.");}
                var bitmap=new BitmapImage{AutoPlay=false,DecodePixelWidth=_lightDevice?960:1920};var file=await StorageFile.GetFileFromPathAsync(chosen);using var stream=await file.OpenReadAsync();await bitmap.SetSourceAsync(stream);if(_windowClosed)return;image.Source=bitmap;
            }
            ApplyArtOpacity();UpdateAnimatedImages();if(_artState is not null)_artState.Text=AstraOS.Core.UiText.T("Banner: ")+(string.IsNullOrEmpty(_preferences.BannerImagePath)?"originale":Path.GetFileName(_preferences.BannerImagePath))+AstraOS.Core.UiText.T(" · Sfondo: ")+(string.IsNullOrEmpty(_preferences.WallpaperImagePath)?"originale":Path.GetFileName(_preferences.WallpaperImagePath));
        }catch(Exception ex){if(_artState is not null)_artState.Text=AstraOS.Core.UiText.T("Caricamento immagine: ")+ex.Message;}
    }
    private void ApplyArtOpacity()
    {
        V4Wallpaper.Opacity=!string.IsNullOrEmpty(_preferences.WallpaperImagePath)?.48:_preferences.Palette.IsDefault?.48:0;
        BannerArtworkImage.Opacity=!string.IsNullOrEmpty(_preferences.BannerImagePath)?.9:_preferences.Palette.IsDefault?.9:.08;
    }
    private void UpdateAnimatedImages()
    {
        bool play=MotionAllowed;foreach(var image in new[]{BannerArtworkImage,V4Wallpaper})if(image.Source is BitmapImage bitmap&&bitmap.IsAnimatedBitmap){if(play&&(image==V4Wallpaper||_v4LastPage=="home"))bitmap.Play();else bitmap.Stop();}
    }
}
