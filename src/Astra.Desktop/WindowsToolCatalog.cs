using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace AstraOS.Desktop;

public sealed record WindowsTool(string Id,string Name,string Category,string Target,string Kind,string Origin);
public static class WindowsToolCatalog
{
    public static readonly string[] Categories={"Produttività","Utilità e strumenti","Creatività","Intrattenimento","Altro","Strumenti di sviluppo","Accessibilità"};
    private static readonly Regex AppId=new(@"\A[A-Za-z0-9_.-]+![A-Za-z0-9_.-]+\z");
    public static async Task<IReadOnlyList<WindowsTool>> DiscoverAsync(IReadOnlyList<InstalledApplication> installed,CancellationToken ct)
    {
        var results=installed.Select(a=>Make(a.Name,Category(a.Name),a.Shortcut,"shortcut","Menu Start")).ToList();
        string system=Environment.GetFolderPath(Environment.SpecialFolder.System),windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var builtins=new (string Name,string File,string Category)[]{
            (AstraOS.Core.UiText.T("Blocco note"),"notepad.exe",Categories[0]),(AstraOS.Core.UiText.T("Calcolatrice"),"calc.exe",Categories[0]),(AstraOS.Core.UiText.T("Paint"),"mspaint.exe",Categories[2]),(AstraOS.Core.UiText.T("Strumento di cattura"),"SnippingTool.exe",Categories[0]),(AstraOS.Core.UiText.T("Mappa caratteri"),"charmap.exe",Categories[0]),(AstraOS.Core.UiText.T("Registrazione azioni"),"psr.exe",Categories[0]),
            (AstraOS.Core.UiText.T("Gestione attività"),"Taskmgr.exe",Categories[1]),(AstraOS.Core.UiText.T("Informazioni di sistema"),"msinfo32.exe",Categories[1]),(AstraOS.Core.UiText.T("Diagnostica DirectX"),"dxdiag.exe",Categories[1]),(AstraOS.Core.UiText.T("Pulizia disco"),"cleanmgr.exe",Categories[1]),(AstraOS.Core.UiText.T("Ottimizza unità"),"dfrgui.exe",Categories[1]),(AstraOS.Core.UiText.T("Monitoraggio risorse"),"resmon.exe",Categories[1]),(AstraOS.Core.UiText.T("Monitoraggio prestazioni"),"perfmon.exe",Categories[1]),(AstraOS.Core.UiText.T("Pannello di controllo"),"control.exe",Categories[1]),(AstraOS.Core.UiText.T("Gestione computer"),"compmgmt.msc",Categories[1]),(AstraOS.Core.UiText.T("Gestione dispositivi"),"devmgmt.msc",Categories[1]),(AstraOS.Core.UiText.T("Gestione disco"),"diskmgmt.msc",Categories[1]),(AstraOS.Core.UiText.T("Visualizzatore eventi"),"eventvwr.msc",Categories[1]),(AstraOS.Core.UiText.T("Utilità di pianificazione"),"taskschd.msc",Categories[1]),(AstraOS.Core.UiText.T("Editor del Registro di sistema"),"regedit.exe",Categories[1]),(AstraOS.Core.UiText.T("Assistenza rapida"),"QuickAssist.exe",Categories[1]),(AstraOS.Core.UiText.T("Connessione Desktop remoto"),"mstsc.exe",Categories[1]),(AstraOS.Core.UiText.T("Connessioni di rete"),"ncpa.cpl",Categories[1]),
            (AstraOS.Core.UiText.T("Prompt dei comandi"),"cmd.exe",Categories[5]),(AstraOS.Core.UiText.T("Windows PowerShell"),@"WindowsPowerShell\v1.0\powershell.exe",Categories[5]),(AstraOS.Core.UiText.T("Ubuntu / WSL"),"wsl.exe",Categories[5]),(AstraOS.Core.UiText.T("Editor ODBC 64 bit"),"odbcad32.exe",Categories[5]),(AstraOS.Core.UiText.T("Editor caratteri personali"),"eudcedit.exe",Categories[2]),(AstraOS.Core.UiText.T("Windows Media Player"),"wmplayer.exe",Categories[3]),
            (AstraOS.Core.UiText.T("Assistente vocale"),"Narrator.exe",Categories[6]),(AstraOS.Core.UiText.T("Lente di ingrandimento"),"Magnify.exe",Categories[6]),(AstraOS.Core.UiText.T("Tastiera su schermo"),"osk.exe",Categories[6])};
        foreach(var item in builtins)
        {
            string? path=new[]{Path.Combine(system,item.File),Path.Combine(windows,item.File),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Windows Media Player",item.File)}.FirstOrDefault(File.Exists);
            if(path is not null)results.Add(Make(AstraOS.Core.UiText.T(item.Name),item.Category,path,"file","Windows"));
        }
        foreach(var (name,uri,category) in new[]{(AstraOS.Core.UiText.T("Accessibilità"),"ms-settings:easeofaccess",Categories[6]),(AstraOS.Core.UiText.T("Dimensioni testo"),"ms-settings:easeofaccess-display",Categories[6]),(AstraOS.Core.UiText.T("Filtri colore"),"ms-settings:easeofaccess-colorfilter",Categories[6]),(AstraOS.Core.UiText.T("Contrasto elevato"),"ms-settings:easeofaccess-highcontrast",Categories[6]),(AstraOS.Core.UiText.T("Puntatore e tocco"),"ms-settings:easeofaccess-mousepointer",Categories[6]),(AstraOS.Core.UiText.T("Sottotitoli"),"ms-settings:easeofaccess-closedcaptioning",Categories[6]),(AstraOS.Core.UiText.T("Audio e accessibilità"),"ms-settings:easeofaccess-audio",Categories[6]),(AstraOS.Core.UiText.T("Multitasking Windows"),"ms-settings:multitasking",Categories[0]),(AstraOS.Core.UiText.T("App di avvio"),"ms-settings:startupapps",Categories[1]),(AstraOS.Core.UiText.T("App predefinite"),"ms-settings:defaultapps",Categories[1]),(AstraOS.Core.UiText.T("Archiviazione"),"ms-settings:storagesense",Categories[1]),(AstraOS.Core.UiText.T("Dispositivi Bluetooth"),"ms-settings:bluetooth",Categories[1]),(AstraOS.Core.UiText.T("Modalità gioco"),"ms-settings:gaming-gamemode",Categories[3]),(AstraOS.Core.UiText.T("Lingua e area geografica"),"ms-settings:regionlanguage",Categories[4])})
            results.Add(Make(AstraOS.Core.UiText.T(name),category,uri,"uri","Impostazioni Windows"));
        try
        {
            var start=new ProcessStartInfo(Path.Combine(system,@"WindowsPowerShell\v1.0\powershell.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};foreach(var arg in new[]{"-NoProfile","-NonInteractive","-Command","[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new(); Get-StartApps | Select-Object Name,AppID | ConvertTo-Json -Compress"})start.ArgumentList.Add(arg);
            using var process=Process.Start(start);if(process is not null)
            {
                using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(12));var output=process.StandardOutput.ReadToEndAsync(deadline.Token);var errors=process.StandardError.ReadToEndAsync(deadline.Token);
                try{await process.WaitForExitAsync(deadline.Token);var json=await output;_=await errors;if(json.Length>0&&json.Length<2_000_000){using var doc=JsonDocument.Parse(json);foreach(var app in doc.RootElement.ValueKind==JsonValueKind.Array?doc.RootElement.EnumerateArray().ToArray():new[]{doc.RootElement}){string? id=app.GetProperty("AppID").GetString(),name=app.GetProperty("Name").GetString();if(id is not null&&id.Length<250&&AppId.IsMatch(id)&&!string.IsNullOrWhiteSpace(name)&&name.Length<200)results.Add(Make(name,Category(name),id,"packaged","App Windows installata"));}}}
                catch(OperationCanceledException){if(!process.HasExited)process.Kill(true);ct.ThrowIfCancellationRequested();}
            }
        }
        catch(Exception ex)when(ex is IOException or System.ComponentModel.Win32Exception or JsonException){/* The executable catalog remains available when Get-StartApps is unavailable. */}
        return results.DistinctBy(t=>Normalize(t.Name)).DistinctBy(t=>t.Target,StringComparer.OrdinalIgnoreCase).OrderBy(t=>t.Category).ThenBy(t=>t.Name,StringComparer.CurrentCultureIgnoreCase).Take(1000).ToArray();
    }
    private static WindowsTool Make(string name,string category,string target,string kind,string origin)=>new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kind+target.ToUpperInvariant()))),name,category,target,kind,origin);
    private static string Normalize(string name)=>Regex.Replace(name.ToLowerInvariant().Replace("microsoft ",""),@"[\s®™&]","");
    public static string Category(string name)
    {
        string s=name.ToLowerInvariant();
        bool Has(params string[] terms)=>terms.Any(s.Contains);
        return Has("narrator","magnif","accessib","assistente vocale","ingrandimento","tastiera su")?Categories[6]:
            Has("visual studio","python","git","terminal","powershell","prompt","wsl","ubuntu","jupyter","code","cmake","docker","sdk","odbc")?Categories[5]:
            Has("paint","foto","photo","clipchamp","blender","gimp","adobe","designer","obs","video editor","camera","fotocamera")?Categories[2]:
            Has("xbox","game","gioch","spotify","steam","media player","film","music","musica","netflix","vlc")?Categories[3]:
            Has("office","word","excel","powerpoint","onenote","outlook","teams","calendar","calendario","notepad","blocco note","calculator","calcolatrice","clock","orologio","snipping","cattura","todo","to do","edge","chrome","browser")?Categories[0]:
            Has("settings","impostaz","manager","gestione","monitor","disk","disco","system","sistema","control","controllo","utility","utilità","sicurezza","security","defender")?Categories[1]:Categories[4];
    }
    public static void Open(WindowsTool tool)
    {
        if(tool.Kind=="packaged")
        {
            if(!AppId.IsMatch(tool.Target))throw new InvalidDataException(AstraOS.Core.UiText.T("Identificatore app non valido."));var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")){UseShellExecute=true};start.ArgumentList.Add("shell:AppsFolder\\"+tool.Target);Process.Start(start);return;
        }
        if(tool.Kind=="uri"&&!tool.Target.StartsWith("ms-settings:",StringComparison.Ordinal))throw new InvalidDataException(AstraOS.Core.UiText.T("Collegamento non valido."));
        if(tool.Kind!="uri"&&!File.Exists(tool.Target))throw new FileNotFoundException(AstraOS.Core.UiText.T("Strumento non più disponibile."));Process.Start(new ProcessStartInfo(tool.Target){UseShellExecute=true});
    }
}
