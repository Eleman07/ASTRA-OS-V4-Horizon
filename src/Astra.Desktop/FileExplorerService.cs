using System.Security.Cryptography;
using System.Text;

namespace AstraOS.Desktop;

public sealed record ExplorerEntry(string Id, string Name, string FullPath, bool IsDirectory,
    long? Bytes, DateTime LastModified, string Location)
{
    public string Glyph => IsDirectory ? "\uE8B7" : "\uE8A5";
    public string Kind => IsDirectory ? "Cartella" : string.IsNullOrEmpty(Path.GetExtension(Name)) ? "File" : Path.GetExtension(Name).TrimStart('.').ToUpperInvariant();
    public string Size => Bytes is null ? "—" : Bytes >= 1048576 ? $"{Bytes/1048576.0:F1} MB" : Bytes >= 1024 ? $"{Bytes/1024.0:F1} KB" : $"{Bytes} B";
    public string Modified => LastModified.ToString("dd/MM/yyyy HH:mm");
}
public sealed record DirectorySnapshot(string Path, IReadOnlyList<ExplorerEntry> Entries, bool Truncated, int Unreadable);

public static class FileExplorerService
{
    public const int Limit = 2000;
    public static IReadOnlyList<string> DesktopRoots => new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
    }.Where(p=>!string.IsNullOrWhiteSpace(p)&&Directory.Exists(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static DirectorySnapshot ReadDirectory(string directory, CancellationToken ct = default)
    {
        directory = System.IO.Path.GetFullPath(directory);
        var entries = new List<ExplorerEntry>(); int unreadable=0; bool truncated=false;
        foreach (var path in Directory.EnumerateFileSystemEntries(directory, "*", new EnumerationOptions
        { RecurseSubdirectories=false, IgnoreInaccessible=false, AttributesToSkip=FileAttributes.Hidden|FileAttributes.System }))
        {
            ct.ThrowIfCancellationRequested();
            if(entries.Count>=Limit){truncated=true;break;}
            try
            {
                var attributes=File.GetAttributes(path); bool folder=(attributes&FileAttributes.Directory)!=0;
                var info=new FileInfo(path);
                entries.Add(new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpperInvariant()))),
                    System.IO.Path.GetFileName(path),path,folder,folder?null:info.Length,info.LastWriteTime,directory));
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){unreadable++;}
        }
        return new(directory,entries.OrderByDescending(e=>e.IsDirectory).ThenBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase).ToArray(),truncated,unreadable);
    }
    public static IReadOnlyList<ExplorerEntry> ReadDesktop(CancellationToken ct = default) => DesktopRoots
        .SelectMany(p=>ReadDirectory(p,ct).Entries).DistinctBy(e=>e.FullPath,StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(e=>e.IsDirectory).ThenBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase).Take(500).ToArray();

    public static bool IsCurrentDesktopEntry(ExplorerEntry entry) => DesktopRoots.Any(root =>
        string.Equals(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(entry.FullPath)),System.IO.Path.GetFullPath(root),StringComparison.OrdinalIgnoreCase)) &&
        (entry.IsDirectory?Directory.Exists(entry.FullPath):File.Exists(entry.FullPath));
}
