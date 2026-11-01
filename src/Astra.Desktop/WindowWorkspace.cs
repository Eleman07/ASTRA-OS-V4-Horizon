using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AstraOS.Core;
namespace AstraOS.Desktop;

public sealed record WorkspaceWindow(long Handle,int ProcessId,string Title,string ProcessName,string ExecutablePath="")
{
    public string Display=>Title+" · "+ProcessName;
}
internal sealed class WindowWorkspace
{
    [DllImport("user32.dll")]private static extern nint GetShellWindow();
    [DllImport("user32.dll")]private static extern nint GetDesktopWindow();
    private delegate bool EnumWindow(nint hwnd,nint parameter);
    [StructLayout(LayoutKind.Sequential)]private struct Point{public int X,Y;}
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct Placement{public int Length,Flags,Show;public Point Min,Max;public Rect Normal;}
    [DllImport("user32.dll")]private static extern bool EnumWindows(EnumWindow callback,nint parameter);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")]private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(nint hwnd,StringBuilder text,int length);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint hwnd,out uint processId);
    [DllImport("dwmapi.dll")]private static extern int DwmGetWindowAttribute(nint hwnd,int attribute,out int value,int size);
    [DllImport("user32.dll")]private static extern bool ShowWindow(nint hwnd,int command);
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")]private static extern bool ShowWindowAsync(nint hwnd,int command);
    [DllImport("user32.dll")]private static extern nint GetWindow(nint hwnd,uint command);
    [DllImport("user32.dll")]private static extern nint GetLastActivePopup(nint hwnd);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]private static extern nint GetWindowLongPtr(nint hwnd,int index);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(nint hwnd,StringBuilder name,int length);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SetWindowPos(nint hwnd,nint insert,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")]private static extern bool GetWindowPlacement(nint hwnd,ref Placement placement);
    [DllImport("user32.dll")]private static extern bool SetWindowPlacement(nint hwnd,in Placement placement);
    private readonly Dictionary<long,(int Process,Placement Placement)> _restore=new();
    public static IReadOnlyList<WorkspaceWindow> List(bool forDock=false,int? onlyProcessId=null)
    {
        var windows=new List<WorkspaceWindow>();EnumWindows((hwnd,_)=>
        {
            if(hwnd==GetShellWindow()||hwnd==GetDesktopWindow()||!IsWindowVisible(hwnd))return true;
            GetWindowThreadProcessId(hwnd,out var pid);if(onlyProcessId is {} required&&pid!=required)return true;
            var title=new StringBuilder(512);if(GetWindowText(hwnd,title,title.Capacity)==0)return true;
            if(DwmGetWindowAttribute(hwnd,14,out var cloaked,sizeof(int))==0&&cloaked!=0)return true;
            if(forDock){var cls=new StringBuilder(256);GetClassName(hwnd,cls,cls.Capacity);if(!DockWindowRules.Include((int)pid,Environment.ProcessId,(long)GetWindowLongPtr(hwnd,-16),(long)GetWindowLongPtr(hwnd,-20),GetWindow(hwnd,4)!=0,cls.ToString()))return true;}
            try{using var process=Process.GetProcessById((int)pid);if(new[]{"ShellExperienceHost","SearchHost","StartMenuExperienceHost","LockApp","LogonUI","consent","CredentialUIBroker","TextInputHost"}.Contains(process.ProcessName,StringComparer.OrdinalIgnoreCase))return true;
                string path="";if(forDock){try{path=process.MainModule?.FileName??"";}catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
                windows.Add(new((long)hwnd,(int)pid,title.ToString(),process.ProcessName,path));}catch(ArgumentException){}catch(System.ComponentModel.Win32Exception){}return true;
        },0);return windows.Take(100).ToArray();
    }
    private static bool Current(WorkspaceWindow window)=>IsWindow((nint)window.Handle)&&GetWindowThreadProcessId((nint)window.Handle,out var process)!=0&&process==window.ProcessId;
    public static long ForegroundHandle=>(long)GetForegroundWindow();
    public static bool IsMinimized(long handle)=>IsIconic((nint)handle);
    public static void Activate(WorkspaceWindow window)
    {
        if(!Current(window))throw new InvalidOperationException(UiText.T("Finestra non più disponibile."));
        var target=(nint)window.Handle;
        // Restore minimized windows only: maximized apps keep their original size.
        if(IsIconic(target))ShowWindowAsync(target,9);
        var popup=GetLastActivePopup(target);if(popup!=0&&IsWindowVisible(popup))target=popup;
        if(GetForegroundWindow()!=target&&!SetForegroundWindow(target))throw new InvalidOperationException(UiText.T("Windows non ha consentito di attivare la finestra. Riprova dal dock."));
    }
    public static void Minimize(WorkspaceWindow window){if(Current(window))ShowWindowAsync((nint)window.Handle,6);}
    public void Tile(IReadOnlyList<WorkspaceWindow> windows,IReadOnlyList<TileRectangle> rectangles)
    {
        if(windows.Count!=rectangles.Count)throw new ArgumentException(AstraOS.Core.UiText.T("Selezione e layout non corrispondenti."));
        for(int i=0;i<windows.Count;i++)
        {
            var w=windows[i];if(!Current(w))continue;var placement=new Placement{Length=Marshal.SizeOf<Placement>()};if(GetWindowPlacement((nint)w.Handle,ref placement))_restore.TryAdd(w.Handle,(w.ProcessId,placement));
            ShowWindow((nint)w.Handle,9);var r=rectangles[i];if(!SetWindowPos((nint)w.Handle,0,r.X,r.Y,r.Width,r.Height,0x0010|0x0040|0x0200))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    public void Restore()
    {
        foreach(var (handle,saved) in _restore){GetWindowThreadProcessId((nint)handle,out var process);if(process==saved.Process&&IsWindow((nint)handle))SetWindowPlacement((nint)handle,saved.Placement);}_restore.Clear();
    }
}
