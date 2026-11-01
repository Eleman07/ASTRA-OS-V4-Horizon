using System.Diagnostics;
using Microsoft.Win32;

namespace AstraOS.Desktop;

public static class ChromeLauncher
{
    [System.Runtime.InteropServices.DllImport("shlwapi.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,ExactSpelling=true)]
    private static extern int AssocQueryStringW(uint flags,uint kind,string association,string? verb,System.Text.StringBuilder? output,ref uint length);
    public static string? DefaultHandler(string association)
    {
        if(association is not("http" or "https" or ".htm" or ".html"))throw new ArgumentException(AstraOS.Core.UiText.T("Associazione non supportata."));
        uint flags=association.StartsWith('.')?0u:0x1000u,length=0;AssocQueryStringW(flags,2,association,"open",null,ref length);
        if(length is 0 or >4096)return null;var result=new System.Text.StringBuilder((int)length);
        return AssocQueryStringW(flags,2,association,"open",result,ref length)==0?result.ToString():null;
    }
    public static string? Find()
    {
        foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)})
        {var file=Path.Combine(root,"Google","Chrome","Application","chrome.exe");if(File.Exists(file))return file;}
        foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})
        {using var key=hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");if(key?.GetValue(null) is string p&&Path.GetFileName(p).Equals("chrome.exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(p))return p;}
        return null;
    }
    public static void Open(string address="chrome://newtab")
    {
        if(address!="chrome://newtab"&&(!Uri.TryCreate(address,UriKind.Absolute,out var uri)||uri.Scheme is not("https" or "http")))
            throw new ArgumentException(AstraOS.Core.UiText.T("Collegamento web non valido."));
        var chrome=Find()??throw new FileNotFoundException(AstraOS.Core.UiText.T("Google Chrome non è installato. Installa Chrome oppure apri le App predefinite di Windows."));
        var start=new ProcessStartInfo(chrome){UseShellExecute=false};start.ArgumentList.Add(address);Process.Start(start);
    }
}
