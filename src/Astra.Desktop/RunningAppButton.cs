using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;

internal sealed class RunningAppButton
{
    private readonly Grid _icon=new(){Width=27,Height=27,Margin=new Thickness(0,0,0,4)};
    private readonly Border _indicator=new(){Height=3,CornerRadius=new CornerRadius(2),VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,2)};
    private readonly SolidColorBrush _accent;
    public Button View { get; }
    public WorkspaceWindow Window { get; private set; }
    internal bool HasNativeIcon { get; private set; }
    public RunningAppButton(WorkspaceWindow window,SolidColorBrush text,SolidColorBrush accent,Action<WorkspaceWindow> activate,Action<WorkspaceWindow> minimize)
    {
        Window=window;_accent=accent;
        var grid=new Grid{Width=36,Height=38};grid.Children.Add(_icon);grid.Children.Add(_indicator);
        _icon.Children.Add(new SymbolIcon{Symbol=Symbol.Document,Foreground=text});
        View=new Button{Width=44,Height=44,Padding=new Thickness(4),CornerRadius=new CornerRadius(12),Background=new SolidColorBrush(),BorderThickness=new Thickness(0),Content=grid,Foreground=text};
        AutomationProperties.SetAutomationId(View,"RunningWindow_"+window.Handle);
        View.Click+=(_,_)=>activate(Window);
        var menu=new MenuFlyout();var show=new MenuFlyoutItem{Text=UiText.T("Attiva finestra")};show.Click+=(_,_)=>activate(Window);menu.Items.Add(show);
        var hide=new MenuFlyoutItem{Text=UiText.T("Riduci a icona")};hide.Click+=(_,_)=>minimize(Window);menu.Items.Add(hide);View.ContextFlyout=menu;
        Update(window,WindowWorkspace.ForegroundHandle);
        _=LoadIconAsync();
    }
    public void Update(WorkspaceWindow window,long foreground)
    {
        Window=window;var name=window.ProcessName+" · "+window.Title;
        ToolTipService.SetToolTip(View,name);AutomationProperties.SetName(View,name);
        bool active=foreground==window.Handle;
        _indicator.Width=active?18:6;_indicator.Background=_accent;
        _indicator.Opacity=WindowWorkspace.IsMinimized(window.Handle)?.45:1;
        View.BorderThickness=new Thickness(active?1:0);View.BorderBrush=_accent;
    }
    private async Task LoadIconAsync()
    {
        try
        {
            var window=Window;
            var pixels=await Task.Run(()=>NativeShellIcon.ForWindow(window.Handle,window.ExecutablePath));
            var image=ShellIconBitmap.Create(pixels);
            if(image is not null){_icon.Children.Clear();_icon.Children.Add(new Image{Source=image,Width=27,Height=27});HasNativeIcon=true;}
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or ArgumentException)
        { /* Protected/packaged executables keep the generic icon; switching still works. */ }
    }
}
