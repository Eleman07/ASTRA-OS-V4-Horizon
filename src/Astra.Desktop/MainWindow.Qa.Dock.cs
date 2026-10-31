using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using AstraOS.Core;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AstraOS.Desktop;
public sealed partial class MainWindow
{
    private bool DockQaEnabled=>(Environment.GetEnvironmentVariable("ASTRA_RUN_DOCK_SMOKE")=="1"||Environment.GetEnvironmentVariable("ASTRA_RUN_ASSISTANT_SYSTEM_SMOKE")=="1")&&!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY"));
    private int _dockQaFixtureProcessId;
    private TaskCompletionSource<bool>? _dockQaClick;
    [DllImport("user32.dll",EntryPoint="ShowWindowAsync")]private static extern bool DockQaShow(nint window,int command);
    [DllImport("user32.dll",EntryPoint="IsZoomed")]private static extern bool DockQaZoomed(nint window);
    [DllImport("user32.dll")]private static extern uint GetGuiResources(nint process,uint flags);
    private async Task RunDockSmokeAsync()
    {
        var checks=new List<object>();Process? fixture=null;bool failed=false;
        void Check(string name,bool value){checks.Add(new{name,status=value?"PASS":"FAIL"});failed|=!value;}
        try
        {
            string dll=Environment.GetEnvironmentVariable("ASTRA_DOCK_FIXTURE_DLL")??"";
            if(!File.Exists(dll))throw new FileNotFoundException("Dock QA fixture DLL is missing.");
            var start=new ProcessStartInfo("dotnet"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add(dll);start.ArgumentList.Add("--dock-fixture");
            fixture=Process.Start(start)??throw new InvalidOperationException("Dock fixture failed to start");
            var first=await fixture.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            using var json=JsonDocument.Parse(first??"{}");long handle=json.RootElement.GetProperty("handle").GetInt64();int pid=json.RootElement.GetProperty("processId").GetInt32();
            Check("Fixture belongs to the process launched by QA",pid==fixture.Id);
            _dockQaFixtureProcessId=pid;
            for(int i=0;i<25&&(!_runningAppButtons.ContainsKey(handle)||_refreshingRunningApps);i++){await RefreshRunningAppsAsync();await Task.Delay(120);}
            Check("External window appears automatically in the dock",_runningAppButtons.ContainsKey(handle));
            Check("ASTRA helper windows are excluded",_runningAppButtons.Values.All(b=>b.Window.ProcessId!=Environment.ProcessId));
            var target=_runningAppButtons[handle].Window;
            var fixtureFolder=Path.Combine(AstraPaths.UserDataRoot,"icon-fixtures");Directory.CreateDirectory(fixtureFolder);
            var textFile=Path.Combine(fixtureFolder,"Fixture.txt");await File.WriteAllTextAsync(textFile,"ASTRA icon rendering fixture");
            var shortcut=Path.Combine(fixtureFolder,"Fixture.lnk");
            dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            try{dynamic link=shell.CreateShortcut(shortcut);try{link.TargetPath=Environment.ProcessPath!;link.IconLocation=Path.Combine(AppContext.BaseDirectory,"Assets","astra.ico");link.Save();}finally{Marshal.FinalReleaseComObject(link);}}finally{Marshal.FinalReleaseComObject(shell);}
            var examples=new[]{("Folder",fixtureFolder,true),("Text",textFile,false),("Application",Environment.ProcessPath!,false),("Shortcut",shortcut,false)};
            var gallery=new Microsoft.UI.Xaml.Controls.StackPanel{Orientation=Microsoft.UI.Xaml.Controls.Orientation.Horizontal,Spacing=20};
            foreach(var (name,path,folder) in examples)
            {
                var entry=new ExplorerEntry("qa-"+name,name,path,folder,null,DateTime.UtcNow,"QA");
                var source=await DesktopIconService.GetAsync(entry);
                Check("Desktop icon has visible pixels: "+name,source is Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap);
                gallery.Children.Add(new Microsoft.UI.Xaml.Controls.Image{Source=source,Width=48,Height=48});
            }
            ThemePanel.Children.Insert(0,gallery);
            var shortcutIcon=await Task.Run(()=>NativeShellIcon.ForFile(shortcut));
            var programIcon=await Task.Run(()=>NativeShellIcon.ForFile(Environment.ProcessPath!));
            Check("Desktop shortcuts resolve their custom associated icon",shortcutIcon?.HasPixels==true&&programIcon?.HasPixels==true&&!shortcutIcon.Bgra.SequenceEqual(programIcon.Bgra));
            var nativeIcon=await Task.Run(()=>NativeShellIcon.ForWindow(handle,target.ExecutablePath));
            var exeIcon=await Task.Run(()=>NativeShellIcon.ForFile(target.ExecutablePath,false,48));
            Check("Dock prefers the real window icon over its host EXE",nativeIcon?.HasPixels==true&&exeIcon?.HasPixels==true&&!nativeIcon.Bgra.SequenceEqual(exeIcon.Bgra));
            for(int i=0;i<25&&!_runningAppButtons[handle].HasNativeIcon;i++)await Task.Delay(100);
            Check("Dock button displays a native bitmap",_runningAppButtons[handle].HasNativeIcon);
            if(ChromeLauncher.Find() is not null)Check("Pinned Chrome button displays its program icon",L10n0214.Content is Microsoft.UI.Xaml.Controls.Image{Source:Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap});
            using(var current=Process.GetCurrentProcess())
            {
                uint before=GetGuiResources(current.Handle,0);
                await Task.Run(()=>{for(int i=0;i<40;i++)NativeShellIcon.ForWindow(handle,target.ExecutablePath);});
                Check("Repeated icon extraction releases GDI handles",GetGuiResources(current.Handle,0)<=before+2);
            }
            ApplyFullScreen(true);Navigate("theme");Activate();await Task.Delay(200);
            var preview=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await preview.RenderAsync(gallery);
            var previewPixels=await preview.GetPixelsAsync();
            Check("Native icons are rendered by XAML",preview.PixelWidth>0&&previewPixels.Length>0);
            using(var output=File.Create(Path.Combine(AstraPaths.UserDataRoot,"native-icons.png")))
            {
                var encoder=await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,output.AsRandomAccessStream());
                encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,(uint)preview.PixelWidth,(uint)preview.PixelHeight,96,96,previewPixels.ToArray());await encoder.FlushAsync();
            }
            if(Environment.GetEnvironmentVariable("ASTRA_DOCK_INTERACTIVE")=="1")
            {
                _dockQaClick=new(TaskCreationOptions.RunContinuationsAsynchronously);
                _runningAppButtons[handle].View.Focus(FocusState.Programmatic);
                await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"dock-ready.json"),JsonSerializer.Serialize(new{handle,button="RunningWindow_"+handle}));
                Check("Real dock click activates the fixture",await _dockQaClick.Task.WaitAsync(TimeSpan.FromMinutes(3)));
            }
            else WindowWorkspace.Activate(target);
            await Task.Delay(400);await RefreshRunningAppsAsync();
            Check("Dock activates the external application",WindowWorkspace.ForegroundHandle==handle);
            Check("No extra ASTRA overlay window appears above apps",WindowWorkspace.List(onlyProcessId:Environment.ProcessId).Count==1);
            WindowWorkspace.Minimize(target);await Task.Delay(250);
            Check("Dock can minimize the selected window",WindowWorkspace.IsMinimized(handle));
            Activate();await Task.Delay(120);WindowWorkspace.Activate(target);await Task.Delay(350);
            Check("Dock restores minimized apps",!WindowWorkspace.IsMinimized(handle));
            DockQaShow((nint)handle,3);await Task.Delay(250);Activate();await Task.Delay(100);WindowWorkspace.Activate(target);await Task.Delay(200);
            Check("Activating a maximized app preserves maximization",DockQaZoomed((nint)handle));
            WindowWorkspace.Activate(new WorkspaceWindow((long)WinRT.Interop.WindowNative.GetWindowHandle(this),Environment.ProcessId,Title,"ASTRA"));await Task.Delay(250);
            Check("Running app icons remain inside ASTRA",HorizonDock.Visibility==Visibility.Visible&&RunningAppsPanel.Children.Count==1);
            fixture.CloseMainWindow();await fixture.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await RefreshRunningAppsAsync();Check("Closed applications disappear from the dock",!_runningAppButtons.ContainsKey(handle));
            Check("Dock fits the ASTRA viewport",HorizonDock.ActualWidth<=AppRoot.ActualWidth);
            if(Environment.GetEnvironmentVariable("ASTRA_RUN_BRAIN_SMOKE")=="1")await RunBrainSmokeAsync(Check);
        }
        catch(Exception ex){failed=true;checks.Add(new{name="Dock QA exception",status="FAIL",detail=ex.ToString()});}
        finally
        {
            if(fixture is not null){if(!fixture.HasExited)fixture.Kill(true);fixture.Dispose();}
            await File.WriteAllTextAsync(Path.Combine(AstraPaths.UserDataRoot,"dock-smoke.json"),JsonSerializer.Serialize(new{passed=!failed,checks},new JsonSerializerOptions{WriteIndented=true}));
            Close();
        }
    }
}
