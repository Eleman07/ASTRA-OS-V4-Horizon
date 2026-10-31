using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace AstraOS.Core;

public sealed record WorkspaceSession(string Id,string Name,string Workspace,string StudyDomain,string EngineeringTrack,string Layout,IReadOnlyList<string> Apps,IReadOnlyList<string> Folders);
public sealed class WorkspaceSessionStore
{
    private readonly string _path;
    public WorkspaceSessionStore(string? directory=null)=>_path=Path.Combine(directory??Path.Combine(AstraPaths.UserDataRoot,"workspaces"),"sessions.dpapi");
    public IReadOnlyList<WorkspaceSession> Load()
    {
        if(!File.Exists(_path))return Array.Empty<WorkspaceSession>();if(new FileInfo(_path).Length>160000)throw new InvalidDataException(AstraOS.Core.UiText.T("Archivio sessioni troppo grande."));var bytes=ProtectedData.Unprotect(File.ReadAllBytes(_path),null,DataProtectionScope.CurrentUser);
        try{var sessions=JsonSerializer.Deserialize<List<WorkspaceSession>>(bytes)??new();Validate(sessions);return sessions;}finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public void Save(IReadOnlyList<WorkspaceSession> sessions)
    {
        Validate(sessions);Directory.CreateDirectory(Path.GetDirectoryName(_path)!);var bytes=JsonSerializer.SerializeToUtf8Bytes(sessions);
        try{string temp=_path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllBytes(temp,ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser));File.Move(temp,_path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}finally{CryptographicOperations.ZeroMemory(bytes);}
    }
    public static void Validate(IReadOnlyList<WorkspaceSession> sessions)
    {
        if(sessions.Count>24||sessions.Any(s=>s is null)||sessions.Select(s=>s.Id).Distinct().Count()!=sessions.Count)throw new InvalidDataException(AstraOS.Core.UiText.T("Massimo 24 sessioni."));foreach(var s in sessions)
        {
            if(!Guid.TryParseExact(s.Id,"N",out _)||string.IsNullOrWhiteSpace(s.Name)||s.Name.Length>80||!CuratedCatalog.ProfileIds.Contains(s.Workspace)||!StudyCatalog.Domains.Any(d=>d.Id==s.StudyDomain)||!StudyCatalog.Engineering.Any(t=>t.Id==s.EngineeringTrack)||s.Layout is not("two" or "three" or "grid")||s.Apps is null||s.Apps.Count>6||s.Apps.Any(a=>a is null||a.Length!=64||!a.All(Uri.IsHexDigit))||s.Folders is null||s.Folders.Count>6||s.Folders.Any(p=>p is null||!Path.IsPathFullyQualified(p)||p.Length>2048))throw new InvalidDataException(AstraOS.Core.UiText.T("Sessione di lavoro non valida."));
        }
    }
}
public sealed record TileRectangle(int X,int Y,int Width,int Height);
public static class MultitaskLayouts
{
    public static IReadOnlyList<TileRectangle> Arrange(int count,int x,int y,int width,int height,string layout)
    {
        if(count is <1 or >8||width<400||height<300||layout is not("two" or "three" or "grid"))throw new ArgumentException(AstraOS.Core.UiText.T("Layout non valido."));
        int columns=layout=="three"?Math.Min(3,count):layout=="two"?Math.Min(2,count):(int)Math.Ceiling(Math.Sqrt(count));int rows=(int)Math.Ceiling(count/(double)columns);var result=new List<TileRectangle>();
        for(int i=0;i<count;i++){int col=i%columns,row=i/columns,left=x+col*width/columns,top=y+row*height/rows,right=x+(col+1)*width/columns,bottom=y+(row+1)*height/rows;result.Add(new(left+4,top+4,Math.Max(1,right-left-8),Math.Max(1,bottom-top-8)));}return result;
    }
}
