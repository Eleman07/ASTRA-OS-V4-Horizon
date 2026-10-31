using AstraOS.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AstraOS.Desktop;

public sealed partial class MainWindow
{
    private readonly DispatcherTimer _runningAppsTimer=new(){Interval=TimeSpan.FromSeconds(2)};
    private readonly Dictionary<long,RunningAppButton> _runningAppButtons=new();
    private bool _refreshingRunningApps;
    private void InitializeRunningAppsDock()
    {
        var options=new StackPanel{Spacing=8};options.Children.Add(Label(UiText.T("APP APERTE NEL DOCK"),19));
        options.Children.Add(Label(UiText.T("Le finestre aperte compaiono automaticamente nel dock. Passa il mouse su un'icona per leggere il titolo e clicca per riprendere l'app, anche se è ridotta a icona."),12));
        ThemePanel.Children.Add(Card(options));
        _runningAppsTimer.Tick+=async(_,_)=>await RefreshRunningAppsAsync();
        AppRoot.Loaded+=async(_,_)=>{_runningAppsTimer.Start();await LoadPinnedProgramIconsAsync();await RefreshRunningAppsAsync();
            if(Environment.GetEnvironmentVariable("ASTRA_RUN_DOCK_SMOKE")=="1"&&!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY")))await RunDockSmokeAsync();
            else if(Environment.GetEnvironmentVariable("ASTRA_RUN_ASSISTANT_SYSTEM_SMOKE")=="1"&&!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY")))await RunAssistantSystemSmokeAsync();};
        Activated+=(_,_)=>DispatcherQueue.TryEnqueue(async()=>await RefreshRunningAppsAsync());
        Closed+=(_,_)=>{_runningAppsTimer.Stop();_runningAppButtons.Clear();};
    }
    private async Task RefreshRunningAppsAsync()
    {
        if(_windowClosed||_refreshingRunningApps)return;
        _runningAppsTimer.Interval=TimeSpan.FromSeconds(_preferences.EcoMode?4:2);
        var mainHandle=(long)WinRT.Interop.WindowNative.GetWindowHandle(this);
        if(!_preferences.ShowDock||WindowWorkspace.IsMinimized(mainHandle))return;
        _refreshingRunningApps=true;
        try
        {
            var windows=await Task.Run(()=>WindowWorkspace.List(forDock:true,onlyProcessId:DockQaEnabled?_dockQaFixtureProcessId:null),_windowLifetime.Token);
            if(_windowClosed)return;
            long foreground=WindowWorkspace.ForegroundHandle;
            var live=windows.Select(w=>w.Handle).ToHashSet();
            foreach(var handle in _runningAppButtons.Keys.Where(h=>!live.Contains(h)).ToArray())
            {var button=_runningAppButtons[handle].View;RunningAppsPanel.Children.Remove(button);UnregisterDockMotion(button);_runningAppButtons.Remove(handle);}
            foreach(var w in windows)
            {
                if(!_runningAppButtons.TryGetValue(w.Handle,out var item))
                {item=new RunningAppButton(w,Token("AstraDeepText"),Token("AstraIce"),ActivateDockApp,MinimizeDockApp);_runningAppButtons.Add(w.Handle,item);RunningAppsPanel.Children.Add(item.View);RegisterDockMotion(item.View);}
                item.Update(w,foreground);
            }
            RunningAppsDivider.Visibility=windows.Count>0?Visibility.Visible:Visibility.Collapsed;
            HorizonDock.MaxWidth=Math.Max(120,AppRoot.ActualWidth-32);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!_windowClosed)StartupDiagnostics.Record("Dock.refresh.failed",ex.GetType().Name);}
        finally{_refreshingRunningApps=false;}
    }
    private void ActivateDockApp(WorkspaceWindow window)
    {
        try{WindowWorkspace.Activate(window);if(DockQaEnabled&&window.ProcessId==_dockQaFixtureProcessId)_dockQaClick?.TrySetResult(true);}
        catch(Exception ex){StatusText.Text=ex.Message;if(DockQaEnabled)_dockQaClick?.TrySetResult(false);}
        _=RefreshRunningAppsAsync();
    }
    private void MinimizeDockApp(WorkspaceWindow window){WindowWorkspace.Minimize(window);_=RefreshRunningAppsAsync();}
    private async Task LoadPinnedProgramIconsAsync()
    {
        var chrome=ChromeLauncher.Find();if(string.IsNullOrWhiteSpace(chrome))return;
        var pixels=await Task.Run(()=>NativeShellIcon.ForFile(chrome,false,48));
        if(_windowClosed)return;var image=ShellIconBitmap.Create(pixels);
        if(image is not null)L10n0214.Content=new Image{Source=image,Width=24,Height=24};
    }
}
