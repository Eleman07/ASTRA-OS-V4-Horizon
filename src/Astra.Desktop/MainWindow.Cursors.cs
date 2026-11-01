using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private StackPanel? _cursorPreview;
    private ComboBox? _cursorSize;
    private TextBlock? _cursorStatus;
    private void InitializeThemeCursors()
    {
        var p=new StackPanel{Spacing=9};p.Children.Add(Label(AstraOS.Core.UiText.T("PUNTATORI COORDINATI AL TEMA"),19));p.Children.Add(Label(AstraOS.Core.UiText.T("Il set usa i colori del tema e include freccia, link, testo, attesa e ridimensionamento. Applica il set al tuo account Windows, anche nelle altre app. Il backup consente di tornare ai puntatori precedenti. Finché il set ASTRA è attivo, segue i cambi di palette."),12));
        _cursorSize=new ComboBox{Header=UiText.T("Dimensione dei puntatori"),ItemsSource=new[]{"32 px","48 px","64 px"},SelectedIndex=_preferences.CursorSize switch{32=>0,64=>2,_=>1}};_cursorSize.SelectionChanged+=(_,_)=>UpdateCursorPreview();p.Children.Add(_cursorSize);_cursorPreview=new StackPanel{Orientation=Orientation.Horizontal,Spacing=18};p.Children.Add(_cursorPreview);
        var apply=Action(AstraOS.Core.UiText.T("Applica puntatori Windows del tema"),(_,_)=>{try{_preferences.CursorSize=new[]{32,48,64}[_cursorSize.SelectedIndex];ThemeCursorService.Apply(_preferences.Palette,_preferences.CursorSize);_preferences.ThemeCursorEnabled=true;_preferenceStore.Save(_preferences);_cursorStatus!.Text=UiText.T("Puntatori ASTRA applicati al tuo account Windows.");}catch(Exception ex){_cursorStatus!.Text=ex.Message;}});
        var restore=Action(AstraOS.Core.UiText.T("Ripristina i puntatori precedenti"),(_,_)=>{try{ThemeCursorService.Restore();_preferences.ThemeCursorEnabled=false;_preferenceStore.Save(_preferences);_cursorStatus!.Text=UiText.T("Puntatori precedenti ripristinati.");}catch(Exception ex){_cursorStatus!.Text=ex.Message;}});
        if(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY"))){apply.IsEnabled=false;restore.IsEnabled=false;}
        p.Children.Add(apply);p.Children.Add(restore);_cursorStatus=Label(ThemeCursorService.IsActive()?AstraOS.Core.UiText.T("Il set ASTRA è attivo."):AstraOS.Core.UiText.T("Anteprima: premi Applica per cambiare i puntatori Windows."),12);p.Children.Add(_cursorStatus);ThemePanel.Children.Add(Card(p));UpdateCursorPreview();
    }
    private void UpdateCursorPreview()
    {
        if(_cursorPreview is null)return;_cursorPreview.Children.Clear();int size=new[]{32,48,64}[Math.Max(0,_cursorSize?.SelectedIndex??1)];foreach(var role in new[]{"Arrow","Hand","IBeam","Wait","SizeAll"})
        {
            var cursor=ThemeCursors.Draw(role,_preferences.Palette,size);var bitmap=new WriteableBitmap(size,size);using(var stream=bitmap.PixelBuffer.AsStream())stream.Write(cursor.Bgra);bitmap.Invalidate();_cursorPreview.Children.Add(new Image{Source=bitmap,Width=size,Height=size});
        }
    }
    private void RefreshThemeCursors()
    {
        UpdateCursorPreview();if(!_preferences.ThemeCursorEnabled||!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY"))||!ThemeCursorService.IsActive())return;
        try{ThemeCursorService.Apply(_preferences.Palette,_preferences.CursorSize);}catch(Exception ex){if(_cursorStatus is not null)_cursorStatus.Text=ex.Message;StartupDiagnostics.Record("Cursor.theme.failed",ex.Message);}
    }
}
