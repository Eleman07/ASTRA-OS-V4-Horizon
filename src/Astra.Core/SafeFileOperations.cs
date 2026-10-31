using System.IO.Compression;

namespace AstraOS.Core;

/// <summary>Explicit user file operations; never overwrite, traverse links or extract outside a new destination.</summary>
public static class SafeFileOperations
{
    public static string Child(string directory,string name)
    {
        ValidateLeaf(name);
        directory=DirectoryPath(directory);return Path.Combine(directory,name);
    }
    private static void ValidateLeaf(string name)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>160||name!=name.Trim()||name.EndsWith('.')||name is "." or ".."||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name.Contains(':'))throw new ArgumentException("Nome file o cartella non valido.");
        string stem=Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
        if(stem is "CON" or "PRN" or "AUX" or "NUL"||RegexDevice(stem))throw new ArgumentException("Nome riservato da Windows.");
    }
    private static bool RegexDevice(string s)=>s.Length==4&&(s.StartsWith("COM")||s.StartsWith("LPT"))&&s[3] is >= '0' and <= '9';
    public static string DirectoryPath(string path){path=Path.GetFullPath(path);if(!Directory.Exists(path))throw new DirectoryNotFoundException(path);NoLinks(path);return path;}
    public static void NoLinks(string path)
    {
        path=Path.GetFullPath(path);
        for(string? current=path;current is not null;current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("Operazione non disponibile su collegamenti simbolici o junction.");
    }
    public static string Rename(string source,string name)
    {
        source=Path.GetFullPath(source);NoLinks(source);string target=Child(Path.GetDirectoryName(source)!,name);
        if(File.Exists(target)||Directory.Exists(target))throw new IOException("La destinazione esiste già.");
        if(Directory.Exists(source))Directory.Move(source,target);else File.Move(source,target);return target;
    }
    public static async Task<string> CopyFileAsync(string source,string directory,CancellationToken ct=default)
    {
        directory=DirectoryPath(directory);
        source=Path.GetFullPath(source);NoLinks(source);string target=Child(directory,Path.GetFileName(source));
        string partial=Path.Combine(directory,".astra-copy-"+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,65536,true))
            using(var output=new FileStream(partial,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))await input.CopyToAsync(output,ct);
            ct.ThrowIfCancellationRequested();File.Move(partial,target,false);return target;
        }
        finally{if(File.Exists(partial))File.Delete(partial);}
    }
    public static async Task<string> CopyAsync(string source,string directory,CancellationToken ct=default)
    {
        directory=DirectoryPath(directory);
        if(!Directory.Exists(source))return await CopyFileAsync(source,directory,ct);
        source=DirectoryPath(source);string target=Child(directory,Path.GetFileName(source));
        if(target.StartsWith(source.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("La destinazione non può trovarsi dentro la cartella originale.");
        if(Directory.Exists(target)||File.Exists(target))throw new IOException("La destinazione esiste già.");
        string stage=Path.Combine(directory,".astra-copy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        try
        {
            int count=0;long bytes=0;var pending=new Stack<string>();pending.Push(source);
            while(pending.TryPop(out var current))
            {
                ct.ThrowIfCancellationRequested();NoLinks(current);Directory.CreateDirectory(Path.Combine(stage,Path.GetRelativePath(source,current)));
                foreach(var path in Directory.EnumerateFileSystemEntries(current))
                {
                    ct.ThrowIfCancellationRequested();if(++count>10000)throw new IOException("Copia oltre 10.000 elementi.");NoLinks(path);
                    if(Directory.Exists(path)){pending.Push(path);continue;}
                    bytes=checked(bytes+new FileInfo(path).Length);if(bytes>8L*1024*1024*1024)throw new IOException("Copia cartella oltre 8 GB.");
                    string destination=Path.Combine(stage,Path.GetRelativePath(source,path));
                    using var input=File.OpenRead(path);using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None);await input.CopyToAsync(output,ct);
                }
            }
            ct.ThrowIfCancellationRequested();Directory.Move(stage,target);return target;
        }
        finally{if(Directory.Exists(stage)&&Path.GetDirectoryName(stage)==Path.GetFullPath(directory)){NoLinks(stage);Directory.Delete(stage,true);}}
    }
    public static string Move(string source,string directory)
    {
        source=Path.GetFullPath(source);NoLinks(source);string target=Child(directory,Path.GetFileName(source));
        if(target.StartsWith(source.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Una cartella non può contenere se stessa.");
        if(File.Exists(target)||Directory.Exists(target))throw new IOException("La destinazione esiste già.");
        if(Directory.Exists(source))Directory.Move(source,target);else File.Move(source,target);return target;
    }
    public static string CreateFolder(string directory,string name)
    {
        string target=Child(directory,name);if(Directory.Exists(target)||File.Exists(target))throw new IOException("La destinazione esiste già.");Directory.CreateDirectory(target);return target;
    }
    public static async Task<string> ZipFileAsync(string source,CancellationToken ct=default)
    {
        source=Path.GetFullPath(source);NoLinks(source);string target=source+".zip";
        bool folder=Directory.Exists(source);var paths=new List<string>();long bytes=0;
        if(folder)
        {
            var pending=new Stack<string>();pending.Push(source);
            while(pending.TryPop(out var current))foreach(var path in Directory.EnumerateFileSystemEntries(current))
            {
                ct.ThrowIfCancellationRequested();NoLinks(path);if(Directory.Exists(path)){pending.Push(path);continue;}
                paths.Add(path);bytes=checked(bytes+new FileInfo(path).Length);if(paths.Count>5000||bytes>2L*1024*1024*1024)throw new IOException("ZIP oltre 5.000 file o 2 GB.");
            }
        }
        else{if(new FileInfo(source).Length>2L*1024*1024*1024)throw new IOException("Limite ZIP: 2 GB.");paths.Add(source);}
        using var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        try
        {
            using var zip=new ZipArchive(output,ZipArchiveMode.Create,true);
            if(paths.Count==0)zip.CreateEntry(Path.GetFileName(source)+"/");
            foreach(var path in paths)
            {
                ct.ThrowIfCancellationRequested();var entry=zip.CreateEntry(folder?Path.GetRelativePath(source,path).Replace('\\','/'):Path.GetFileName(path),CompressionLevel.Fastest);using var dest=entry.Open();using var input=File.OpenRead(path);await input.CopyToAsync(dest,ct);
            }
            return target;
        }
        catch{output.Dispose();File.Delete(target);throw;}
    }
    public static async Task<string> ExtractZipAsync(string source,CancellationToken ct=default)
    {
        source=Path.GetFullPath(source);NoLinks(source);string target=Child(Path.GetDirectoryName(source)!,Path.GetFileNameWithoutExtension(source)+"-extracted");
        if(File.Exists(target)||Directory.Exists(target))throw new IOException("La destinazione esiste già.");
        using var zip=ZipFile.OpenRead(source);long total=0;
        if(zip.Entries.Count>5000)throw new InvalidDataException("Archivio oltre 5.000 voci.");
        foreach(var entry in zip.Entries)
        {
            string name=entry.FullName.Replace('\\','/');string path=Path.GetFullPath(Path.Combine(target,name));
            if(!path.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||name.Contains(':')||name.Split('/').Any(p=>p is "." or "..")||((entry.ExternalAttributes>>16)&0xF000)==0xA000)throw new InvalidDataException("Percorso archivio non sicuro.");
            foreach(var segment in name.Split('/',StringSplitOptions.RemoveEmptyEntries))ValidateLeaf(segment);
            total=checked(total+entry.Length);if(total>2L*1024*1024*1024)throw new InvalidDataException("Estrazione oltre 2 GB.");
        }
        Directory.CreateDirectory(target);
        try
        {
            foreach(var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();string path=Path.GetFullPath(Path.Combine(target,entry.FullName.Replace('\\','/')));
                if(entry.FullName.EndsWith('/')||entry.FullName.EndsWith('\\')){Directory.CreateDirectory(path);continue;}
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);using var input=entry.Open();using var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);await input.CopyToAsync(output,ct);
            }
            return target;
        }
        catch
        {
            // This directory was created exclusively by this operation and validated above.
            if(Directory.Exists(target)&&Path.GetDirectoryName(target)==Path.GetDirectoryName(source)){NoLinks(target);Directory.Delete(target,true);}throw;
        }
    }
}
