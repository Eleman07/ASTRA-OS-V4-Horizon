using System.Security.Cryptography;
using System.Text;
namespace AstraOS.Core;

public sealed class EncryptedWidgetStore
{
    private readonly string _path;
    public EncryptedWidgetStore(string? directory=null)=>_path=Path.Combine(directory??Path.Combine(AstraPaths.UserDataRoot,"widgets"),"widgets.dpapi");
    public List<WidgetSpec> Load()
    {
        if(!File.Exists(_path))return WidgetRules.Defaults().ToList();
        if(new FileInfo(_path).Length>160_000)throw new InvalidDataException(AstraOS.Core.UiText.T("Archivio widget troppo grande."));
        var plain=ProtectedData.Unprotect(File.ReadAllBytes(_path),null,DataProtectionScope.CurrentUser);
        try{return WidgetRules.Import(Encoding.UTF8.GetString(plain)).ToList();}finally{CryptographicOperations.ZeroMemory(plain);}
    }
    public void Save(IEnumerable<WidgetSpec> widgets)
    {
        var plain=Encoding.UTF8.GetBytes(WidgetRules.Export(widgets));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);var encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);string temp=_path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temp,encrypted);File.Move(temp,_path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
        }finally{CryptographicOperations.ZeroMemory(plain);}
    }
}
