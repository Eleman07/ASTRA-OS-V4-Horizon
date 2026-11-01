using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstraOS.Core.SecondBrain;

/// <summary>Portable Markdown interoperability; never edits the source vault during import.</summary>
public static class MarkdownVault
{
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    public static string FileName(BrainNote note)
    {
        string safe=new(note.Title.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c).ToArray());
        return safe.Trim().TrimEnd('.')[..Math.Min(80,safe.Trim().TrimEnd('.').Length)]+"-"+Hash(note.Id)[..10]+".md";
    }
    public static int Export(IEnumerable<BrainNote> source,string folder,CancellationToken ct=default)
    {
        folder=SafeFileOperations.DirectoryPath(folder);string root=Path.Combine(folder,"ASTRA Second Brain");Directory.CreateDirectory(root);SafeFileOperations.NoLinks(root);
        string manifest=Path.Combine(root,".astra-export.json");var previous=File.Exists(manifest)?JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(manifest))??new():new Dictionary<string,string>();
        var updated=new Dictionary<string,string>();var notes=source.ToArray();var names=notes.GroupBy(n=>n.Title,StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>FileName(g.First()),StringComparer.OrdinalIgnoreCase);
        foreach(var note in notes)
        {
            ct.ThrowIfCancellationRequested();string name=FileName(note),path=Path.Combine(root,name);
            string body=Regex.Replace(note.Content,@"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]",m=>names.TryGetValue(m.Groups[1].Value,out var target)?"[["+Path.GetFileNameWithoutExtension(target)+"|"+(m.Groups[2].Success?m.Groups[2].Value:m.Groups[1].Value)+"]]":m.Value,RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(250));
            string text="---\ntitle: "+JsonSerializer.Serialize(note.Title)+"\naliases: ["+JsonSerializer.Serialize(note.Title)+"]\ntags: "+JsonSerializer.Serialize(note.Tags)+"\nastra-id: "+JsonSerializer.Serialize(note.Id)+"\n---\n\n# "+note.Title+"\n\n"+body+"\n";
            if(File.Exists(path))
            {
                SafeFileOperations.NoLinks(path);string current=Hash(File.ReadAllText(path));
                if(current!=Hash(text)&&(!previous.TryGetValue(name,out var original)||current!=original))
                {name=Path.GetFileNameWithoutExtension(name)+"-export-"+Guid.NewGuid().ToString("N")[..8]+".md";path=Path.Combine(root,name);}
            }
            string temporary=path+".tmp";File.WriteAllText(temporary,text,new UTF8Encoding(false));File.Move(temporary,path,true);updated[name]=Hash(text);
        }
        File.WriteAllText(manifest,JsonSerializer.Serialize(updated,new JsonSerializerOptions{WriteIndented=true}));return notes.Length;
    }
    public static (int Imported,int Skipped) Import(EncryptedNoteStore store,string folder,CancellationToken ct=default)
    {
        folder=SafeFileOperations.DirectoryPath(folder);var seen=store.Enumerate().Select(n=>Hash(n.Title.Trim()+"\n"+n.Content.Trim())).ToHashSet();int imported=0,skipped=0;
        var pending=new Stack<string>();pending.Push(folder);
        while(pending.TryPop(out var directory))
        foreach(string path in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();var attributes=File.GetAttributes(path);if((attributes&FileAttributes.ReparsePoint)!=0)continue;
            if(Directory.Exists(path)){if(!Path.GetFileName(path).StartsWith('.'))pending.Push(path);continue;}
            if(!Path.GetExtension(path).Equals(".md",StringComparison.OrdinalIgnoreCase))continue;
            string content=File.ReadAllText(path),title=Path.GetFileNameWithoutExtension(path);string[] tags={"obsidian","imported"};
            if(content.StartsWith("---",StringComparison.Ordinal))
            {
                int end=content.IndexOf("\n---",3,StringComparison.Ordinal);
                if(end>0)
                {
                    string header=content[..end];var field=Regex.Match(header,@"(?m)^title:\s*([^\r\n]+)",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
                    if(field.Success){string value=field.Groups[1].Value.Trim();try{title=value.StartsWith('"')?JsonSerializer.Deserialize<string>(value)??title:value;}catch(JsonException){}}
                    var tagField=Regex.Match(header,@"(?m)^tags:\s*(\[[^\r\n]*\])",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
                    if(tagField.Success){try{tags=(JsonSerializer.Deserialize<string[]>(tagField.Groups[1].Value)??Array.Empty<string>()).Append("obsidian").Distinct().ToArray();}catch(JsonException){}}
                    content=content[(end+4)..].TrimStart('\r','\n');
                }
            }
            if(content.StartsWith("# ",StringComparison.Ordinal)){int end=content.IndexOf('\n');if(end>=0){string heading=content[2..end].Trim();if(title==Path.GetFileNameWithoutExtension(path)){title=heading;content=content[(end+1)..].TrimStart();}else if(heading==title)content=content[(end+1)..].TrimStart();}}
            title=title[..Math.Min(140,title.Length)].Trim();content=content.Trim();
            if(title.Length==0||content.Length==0||!seen.Add(Hash(title+"\n"+content))){skipped++;continue;}
            store.Add(title,content,tags);imported++;
        }
        return(imported,skipped);
    }
}
