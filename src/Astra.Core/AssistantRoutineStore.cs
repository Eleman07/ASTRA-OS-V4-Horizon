using System.Security.Cryptography;
using System.Text.Json;

namespace AstraOS.Core;

public sealed class AssistantRoutineStore
{
    private readonly string _path;
    public AssistantRoutineStore(string? directory=null)=>_path=Path.Combine(directory??Path.Combine(AstraPaths.UserDataRoot,"assistant"),"routines.dpapi");
    public IReadOnlyList<AssistantRoutine> Load()
    {
        if(!File.Exists(_path))return Array.Empty<AssistantRoutine>();
        if(new FileInfo(_path).Length>100000)throw new InvalidDataException("Archivio routine troppo grande.");
        byte[] bytes=ProtectedData.Unprotect(File.ReadAllBytes(_path),null,DataProtectionScope.CurrentUser);
        try{var list=JsonSerializer.Deserialize<AssistantRoutine[]>(bytes)??Array.Empty<AssistantRoutine>();Validate(list);return list;}
        finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public static void Validate(IReadOnlyList<AssistantRoutine> list)
    {
        if(list.Count>16||list.Select(r=>r.Id).Distinct().Count()!=list.Count)throw new InvalidDataException("Massimo 16 routine personali univoche.");
        foreach(var r in list)
        {
            if(!Guid.TryParseExact(r.Id,"N",out _)||string.IsNullOrWhiteSpace(r.Name)||r.Name.Length>80||r.Description is null||r.Description.Length>500||r.Steps is null)throw new InvalidDataException("Routine personale non valida.");
            AssistantWorkspace.Validate(r.Steps);
        }
    }
    public void Save(IReadOnlyList<AssistantRoutine> list)
    {
        Validate(list);byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(list);
        string temp=_path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{Directory.CreateDirectory(Path.GetDirectoryName(_path)!);File.WriteAllBytes(temp,ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser));File.Move(temp,_path,true);}
        finally{CryptographicOperations.ZeroMemory(bytes);if(File.Exists(temp))File.Delete(temp);}
    }
}
