using System.Runtime.InteropServices;
using System.Text;
namespace AstraOS.Desktop;

internal sealed class AssistantHotkey:IDisposable
{
    private delegate nint Subclass(nint hwnd,uint message,nuint wparam,nint lparam,nuint id,nuint data);
    [DllImport("user32.dll")]private static extern bool RegisterHotKey(nint hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]private static extern bool UnregisterHotKey(nint hwnd,int id);
    [DllImport("comctl32.dll")]private static extern bool SetWindowSubclass(nint hwnd,Subclass callback,nuint id,nuint data);
    [DllImport("comctl32.dll")]private static extern bool RemoveWindowSubclass(nint hwnd,Subclass callback,nuint id);
    [DllImport("comctl32.dll")]private static extern nint DefSubclassProc(nint hwnd,uint message,nuint wparam,nint lparam);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetWindowText(nint hwnd,StringBuilder text,int length);
    private readonly nint _window;private readonly Subclass _callback;private const int Id=0xA571;
    public bool Registered{get;}
    public AssistantHotkey(nint window,Action<string> invoke)
    {
        _window=window;_callback=(hwnd,message,wparam,lparam,id,data)=>{if(message==0x0312&&(int)wparam==Id){try{var title=new StringBuilder(512);GetWindowText(GetForegroundWindow(),title,512);invoke(title.ToString());}catch(Exception ex){StartupDiagnostics.Record("Assistant.hotkey.failed",ex.Message);}return 0;}return DefSubclassProc(hwnd,message,wparam,lparam);};
        if(SetWindowSubclass(window,_callback,Id,0))Registered=RegisterHotKey(window,Id,0x4000|0x2|0x1,0x20);
    }
    public void Dispose(){if(Registered)UnregisterHotKey(_window,Id);RemoveWindowSubclass(_window,_callback,Id);}
}
