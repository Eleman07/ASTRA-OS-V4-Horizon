using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core;
public sealed record ProposedFileEdit(string Path,string Content);
public sealed record PreparedFileEdit(string Path,string Content,string? ExpectedSha256,string? Original);
public sealed record PreparedChangePlan(string Root,IReadOnlyList<PreparedFileEdit> Edits);
public sealed record WorkspaceBackupFile(string Path,string Backup,bool Existed,string AfterSha256);
public sealed record WorkspaceBackup(string Root,IReadOnlyList<WorkspaceBackupFile> Files);
public static class WorkspaceChangePlan
{
    public static string Resolve(string root,string relative)
    {
        root=SafeFileOperations.DirectoryPath(root);
        if(string.IsNullOrWhiteSpace(relative)||relative.Length>240||Path.IsPathRooted(relative)||relative.Contains(':')||relative.Split('/','\\').Any(p=>p is ".." or "."))throw new InvalidDataException("Percorso progetto non valido.");
        string path=Path.GetFullPath(Path.Combine(root,relative));if(!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("File fuori dalla cartella autorizzata.");SafeFileOperations.NoLinks(path);return path;
    }
    public static PreparedChangePlan Prepare(string root,IReadOnlyList<ProposedFileEdit> edits)
    {
        if(edits.Count is <1 or >12)throw new InvalidDataException("Una proposta richiede da 1 a 12 file.");
        long total=0;var prepared=new List<PreparedFileEdit>();
        foreach(var edit in edits)
        {
            string path=Resolve(root,edit.Path);if(edit.Content is null||edit.Content.Length>200000||(total+=edit.Content.Length)>1000000||Directory.Exists(path))throw new InvalidDataException("Contenuto modifica non valido o troppo grande.");
            string? original=null,hash=null;
            if(File.Exists(path)){if(new FileInfo(path).Length>1000000)throw new InvalidDataException("File originale oltre 1 MB.");var bytes=File.ReadAllBytes(path);hash=Convert.ToHexString(SHA256.HashData(bytes));original=new UTF8Encoding(false,true).GetString(bytes).TrimStart('\uFEFF');}
            prepared.Add(new(edit.Path,edit.Content,hash,original));
        }
        if(prepared.Select(e=>Resolve(root,e.Path)).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=prepared.Count)throw new InvalidDataException("Proposta con file duplicati.");return new(Path.GetFullPath(root),prepared);
    }
    public static string Preview(PreparedChangePlan plan)=>string.Join("\n\n",plan.Edits.Select(e=>$"--- {e.Path}\n+++ {e.Path}\n"+string.Join("\n",(e.Original??"").Split('\n').Select(l=>"- "+l))+"\n"+string.Join("\n",e.Content.Split('\n').Select(l=>"+ "+l))));
    // Calling Apply is reserved for the UI's explicit per-proposal approval path.
    public static string Apply(PreparedChangePlan plan,string backupRoot)
    {
        foreach(var edit in plan.Edits)
        {
            string path=Resolve(plan.Root,edit.Path);string? current=File.Exists(path)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))):null;
            if(current!=edit.ExpectedSha256)throw new IOException("Un file è cambiato dopo l'anteprima. Rigenera la proposta.");
        }
        string backup=Path.Combine(backupRoot,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);var originals=new Dictionary<string,byte[]?>();var written=new List<string>();
        try
        {
            foreach(var edit in plan.Edits)
            {
                string path=Resolve(plan.Root,edit.Path);byte[]? before=File.Exists(path)?File.ReadAllBytes(path):null;originals[path]=before;
                if(before is not null)File.WriteAllBytes(Path.Combine(backup,originals.Count+".dpapi"),ProtectedData.Protect(before,null,DataProtectionScope.CurrentUser));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);string tmp=path+".astra-"+Guid.NewGuid().ToString("N")+".tmp";
                try{File.WriteAllText(tmp,edit.Content,new UTF8Encoding(false));File.Move(tmp,path,true);written.Add(path);}finally{if(File.Exists(tmp))File.Delete(tmp);}
            }
            var manifest=new WorkspaceBackup(plan.Root,originals.Keys.Select((p,i)=>new WorkspaceBackupFile(Path.GetRelativePath(plan.Root,p),(i+1)+".dpapi",originals[p] is not null,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))))).ToArray());
            var metadata=JsonSerializer.SerializeToUtf8Bytes(manifest);try{File.WriteAllBytes(Path.Combine(backup,"manifest.dpapi"),ProtectedData.Protect(metadata,null,DataProtectionScope.CurrentUser));}finally{CryptographicOperations.ZeroMemory(metadata);}return backup;
        }
        catch{foreach(var path in written){Resolve(plan.Root,Path.GetRelativePath(plan.Root,path));if(originals[path] is {} bytes)File.WriteAllBytes(path,bytes);else File.Delete(path);}throw;}
    }
    public static void Restore(string root,string backup)
    {
        root=SafeFileOperations.DirectoryPath(root);backup=SafeFileOperations.DirectoryPath(backup);var metadata=ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(backup,"manifest.dpapi")),null,DataProtectionScope.CurrentUser);WorkspaceBackup manifest;
        try{manifest=JsonSerializer.Deserialize<WorkspaceBackup>(metadata)??throw new InvalidDataException("Backup non leggibile.");}finally{CryptographicOperations.ZeroMemory(metadata);}
        if(!root.Equals(manifest.Root,StringComparison.OrdinalIgnoreCase)||manifest.Files.Count is <1 or >12)throw new InvalidDataException("Il backup non corrisponde al progetto.");
        var changes=new List<(string Path,byte[]? Original,byte[] Current)>();
        foreach(var file in manifest.Files)
        {
            string path=Resolve(root,file.Path);byte[] current=File.ReadAllBytes(path);if(Convert.ToHexString(SHA256.HashData(current))!=file.AfterSha256||Path.GetFileName(file.Backup)!=file.Backup)throw new IOException("Un file è cambiato dopo la modifica; ripristino annullato.");
            byte[]? original=file.Existed?ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(backup,file.Backup)),null,DataProtectionScope.CurrentUser):null;changes.Add((path,original,current));
        }
        int restored=0;
        try{foreach(var change in changes){if(change.Original is null)File.Delete(change.Path);else File.WriteAllBytes(change.Path,change.Original);restored++;}}
        catch{foreach(var change in changes.Take(restored))File.WriteAllBytes(change.Path,change.Current);throw;}
        finally{foreach(var change in changes){if(change.Original is not null)CryptographicOperations.ZeroMemory(change.Original);CryptographicOperations.ZeroMemory(change.Current);}}
    }
}
