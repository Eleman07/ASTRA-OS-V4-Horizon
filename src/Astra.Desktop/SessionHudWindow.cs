using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace AstraOS.Desktop;
internal sealed class SessionHudWindow:Window
{
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(1)};
    public SessionHudWindow(Func<string> status,AstraPalette palette)
    {
        Title=AstraOS.Core.UiText.T("ASTRA · Session HUD");var text=new TextBlock{TextWrapping=TextWrapping.Wrap,FontSize=13,Foreground=new SolidColorBrush(MainWindow.ColorFromHex(AstraPalette.ContrastingText(palette.Navy)))};
        var p=new StackPanel{Padding=new Thickness(12),Spacing=6};p.Children.Add(new TextBlock{Text=UiText.T("ASTRA / SESSION HUD"),FontSize=15,Foreground=text.Foreground});p.Children.Add(text);Content=new Border{Background=new SolidColorBrush(MainWindow.ColorFromHex(palette.Navy)),Child=p};
        AppWindow.Resize(new Windows.Graphics.SizeInt32(330,190));if(AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter){presenter.IsAlwaysOnTop=true;presenter.IsMaximizable=false;}
        text.Text=status();_timer.Tick+=(_,_)=>text.Text=status();_timer.Start();Closed+=(_,_)=>_timer.Stop();try{AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory,"Assets","astra.ico"));}catch(Exception){}
    }
}
