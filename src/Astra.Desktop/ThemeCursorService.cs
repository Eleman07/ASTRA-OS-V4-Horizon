using System.Runtime.InteropServices;
using System.Text.Json;
using AstraOS.Core;
using Microsoft.Win32;
namespace AstraOS.Desktop;
internal sealed record CursorRegistryValue(RegistryValueKind Kind,string Data);
internal static class ThemeCursorService
{
    private const string KeyPath=@"Control Panel\Cursors";
    private const string Scheme="ASTRA OS · Theme";
    private static string Backup=>Path.Combine(AstraPaths.UserDataRoot,"personalization","cursor-backup.json");
    [DllImport("user32.dll",SetLastError=true)]private static extern bool SystemParametersInfo(uint action,uint parameter,nint value,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]private static extern nint LoadImage(nint instance,string file,uint type,int width,int height,uint flags);
    [DllImport("user32.dll")]private static extern bool DestroyCursor(nint cursor);
    public static bool IsActive(){using var key=Registry.CurrentUser.OpenSubKey(KeyPath);return key?.GetValue("")?.ToString()==Scheme;}
    public static bool CanRestore=>File.Exists(Backup);
    public static IReadOnlyDictionary<string,string> Generate(AstraPalette palette,int size)
    {
        string dir=Path.Combine(AstraPaths.UserDataRoot,"personalization","cursors");Directory.CreateDirectory(dir);var paths=new Dictionary<string,string>();foreach(var role in ThemeCursors.Roles){string path=Path.Combine(dir,"astra-"+role+".cur");File.WriteAllBytes(path,ThemeCursors.Encode(ThemeCursors.Draw(role,palette,size)));paths[role]=path;}return paths;
    }
    public static bool VerifyCursorFile(string path){nint cursor=LoadImage(0,path,2,0,0,0x10);if(cursor==0)return false;DestroyCursor(cursor);return true;}
    public static void Apply(AstraPalette palette,int size)
    {
        var paths=Generate(palette,size);if(paths.Values.Any(p=>!VerifyCursorFile(p)))throw new InvalidDataException(AstraOS.Core.UiText.T("File puntatore non riconosciuto da Windows."));using var key=Registry.CurrentUser.CreateSubKey(KeyPath,true);
        if(!File.Exists(Backup)||!IsActive())
        {
            var backup=new Dictionary<string,CursorRegistryValue?>();foreach(var name in ThemeCursors.Roles.Concat(new[]{"","Scheme Source"}))backup[name]=key.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames) is {} value?new(key.GetValueKind(name),value.ToString()??""):null;
            File.WriteAllText(Backup,JsonSerializer.Serialize(backup));
        }
        foreach(var (role,path) in paths)key.SetValue(role,path,RegistryValueKind.ExpandString);key.SetValue("",Scheme);key.SetValue("Scheme Source",2,RegistryValueKind.DWord);Reload();
    }
    public static void Restore()
    {
        if(!File.Exists(Backup))throw new InvalidOperationException(AstraOS.Core.UiText.T("Non è disponibile un backup dei puntatori precedenti."));var backup=JsonSerializer.Deserialize<Dictionary<string,CursorRegistryValue?>>(File.ReadAllText(Backup))??throw new InvalidDataException(AstraOS.Core.UiText.T("Backup puntatori non valido."));using var key=Registry.CurrentUser.CreateSubKey(KeyPath,true);
        foreach(var (name,value) in backup){if(!ThemeCursors.Roles.Contains(name)&&name is not("" or "Scheme Source"))throw new InvalidDataException(AstraOS.Core.UiText.T("Voce backup non valida."));if(value is null)key.DeleteValue(name,false);else key.SetValue(name,value.Kind==RegistryValueKind.DWord?int.Parse(value.Data):value.Data,value.Kind);}Reload();
    }
    private static void Reload(){if(!SystemParametersInfo(0x57,0,0,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
}
