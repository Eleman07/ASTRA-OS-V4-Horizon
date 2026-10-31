using System.Runtime.InteropServices;
using System.Text.Json;

// Only launched by the opt-in dock QA. No other application is queried or closed.
internal static class DockFixture
{
    private delegate nint WndProc(nint window,uint message,nint wParam,nint lParam);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private struct WindowClass
    {public uint Style;public WndProc Procedure;public int ClassExtra,WindowExtra;public nint Instance,Icon,Cursor,Background;public string? Menu;public string ClassName;}
    [StructLayout(LayoutKind.Sequential)]private struct Message
    {public nint Window;public uint Id;public nuint WParam;public nint LParam;public uint Time;public int X,Y;public uint Private;}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern ushort RegisterClass(ref WindowClass cls);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern nint CreateWindowEx(uint extended,string cls,string title,uint style,int x,int y,int width,int height,nint parent,nint menu,nint instance,nint parameter);
    [DllImport("user32.dll")]private static extern bool ShowWindow(nint window,int command);
    [DllImport("user32.dll")]private static extern int GetMessage(out Message message,nint window,uint min,uint max);
    [DllImport("user32.dll")]private static extern bool TranslateMessage(in Message message);
    [DllImport("user32.dll")]private static extern nint DispatchMessage(in Message message);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern nint DefWindowProc(nint window,uint message,nint wParam,nint lParam);
    [DllImport("user32.dll")]private static extern void PostQuitMessage(int exit);
    [DllImport("user32.dll",EntryPoint="LoadIconW")]private static extern nint LoadIcon(nint instance,nint id);
    private static readonly WndProc Procedure=(window,message,w,l)=>{if(message==2)PostQuitMessage(0);return DefWindowProc(window,message,w,l);};
    public static void Run()
    {
        var cls=new WindowClass{ClassName="ASTRA_DOCK_QA_FIXTURE",Procedure=Procedure,Background=(nint)6,Icon=LoadIcon(0,(nint)32516)};
        if(RegisterClass(ref cls)==0)throw new InvalidOperationException("QA window registration failed");
        var window=CreateWindowEx(0,cls.ClassName,"ASTRA Dock QA — disposable test window",0x00CF0000,120,120,540,320,0,0,0,0);
        if(window==0)throw new InvalidOperationException("QA window creation failed");
        ShowWindow(window,4);
        Console.WriteLine(JsonSerializer.Serialize(new{handle=(long)window,processId=Environment.ProcessId}));Console.Out.Flush();
        while(GetMessage(out var message,0,0,0)>0){TranslateMessage(in message);DispatchMessage(in message);}
    }
}
