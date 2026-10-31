using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstraOS.Core.SecondBrain;

public sealed record BrainDocument(string Id,string Name,int Pages,int OcrPages,int EmptyPages,long Characters,string Sha256,DateTimeOffset ImportedUtc);
public sealed record DocumentPage(int Number,string Text);
public sealed record DocumentBatch(int FirstPage,int LastPage,string Text);

/// <summary>Page-by-page encrypted PDF text. Does not copy or modify the source PDF.</summary>
public sealed class EncryptedDocumentStore
{
    public const int MaxPages=10000,MaxPageCharacters=500000;
    public const long MaxCharacters=50_000_000;
    private readonly string _root;
    private static readonly Regex Terms=new(@"[\p{L}\p{N}]{2,}",RegexOptions.Compiled|RegexOptions.CultureInvariant);
    public EncryptedDocumentStore(string? directory=null)=>_root=Path.GetFullPath(directory??Path.Combine(AstraPaths.UserDataRoot,"brain","documents"));
    private string Folder(string id)=>Guid.TryParseExact(id,"N",out _)?Path.Combine(_root,id):throw new ArgumentException("Invalid document ID.");
    private static void Save(string path,object value)
    {
        var clear=JsonSerializer.SerializeToUtf8Bytes(value);
        try{if(clear.Length>4_000_000)throw new InvalidDataException("Document page exceeds the storage limit.");File.WriteAllBytes(path,ProtectedData.Protect(clear,null,DataProtectionScope.CurrentUser));}
        finally{CryptographicOperations.ZeroMemory(clear);}
    }
    private static T Read<T>(string path)
    {
        if(new FileInfo(path).Length>4_100_000)throw new InvalidDataException("Invalid encrypted document size.");
        var clear=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);
        try{return JsonSerializer.Deserialize<T>(clear)??throw new InvalidDataException("Invalid encrypted document.");}
        finally{CryptographicOperations.ZeroMemory(clear);}
    }
    public IReadOnlyList<BrainDocument> List()
    {
        if(!Directory.Exists(_root))return Array.Empty<BrainDocument>();
        var result=new List<BrainDocument>();
        foreach(var folder in Directory.EnumerateDirectories(_root).Where(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _)))
        {
            if(result.Count>=256)throw new InvalidDataException("Document library exceeds 256 PDFs.");
            var document=Read<BrainDocument>(Path.Combine(folder,"manifest.dpapi"));
            if(document.Id!=Path.GetFileName(folder)||document.Pages is <1 or >MaxPages||document.Name.Length>180||document.Characters<0||document.Characters>MaxCharacters)throw new InvalidDataException("Invalid document metadata.");
            result.Add(document);
        }
        return result.OrderByDescending(d=>d.ImportedUtc).ToArray();
    }
    public ImportSession Begin(string name,int pages,string hash)=>new(this,name,pages,hash);
    public IEnumerable<DocumentPage> ReadPages(string id,CancellationToken ct=default)
    {
        var folder=Folder(id);var document=Read<BrainDocument>(Path.Combine(folder,"manifest.dpapi"));
        if(document.Pages is <1 or >MaxPages)throw new InvalidDataException("Invalid page count.");
        for(int i=1;i<=document.Pages;i++)
        {
            ct.ThrowIfCancellationRequested();var page=Read<DocumentPage>(Path.Combine(folder,$"{i:D5}.dpapi"));
            if(page.Number!=i||page.Text.Length>MaxPageCharacters)throw new InvalidDataException("Invalid document page.");
            yield return page;
        }
    }
    public IReadOnlyList<NotePassage> Search(string query,string? documentId=null,int limit=4,CancellationToken ct=default)
    {
        if(query.Length>2500)query=query[..2500];
        var stop=new HashSet<string>("the and for from with what which this that are is to of in on di il lo la le gli un una per con come cosa quale che sono del della delle dei da nel nella".Split(' '),StringComparer.Ordinal);
        var terms=Terms.Matches(query.ToLowerInvariant()).Select(m=>m.Value).Where(t=>!stop.Contains(t)).Distinct().Take(32).ToArray();
        if(terms.Length==0)return Array.Empty<NotePassage>();
        var best=new List<NotePassage>();limit=Math.Clamp(limit,1,6);
        foreach(var doc in List().Where(d=>documentId is null||d.Id==documentId))
        foreach(var page in ReadPages(doc.Id,ct))
        for(int offset=0;offset<page.Text.Length;offset+=1050)
        {
            ct.ThrowIfCancellationRequested();string passage=page.Text.Substring(offset,Math.Min(1250,page.Text.Length-offset));
            var words=Terms.Matches(passage.ToLowerInvariant()).Select(m=>m.Value).GroupBy(x=>x).ToDictionary(g=>g.Key,g=>g.Count(),StringComparer.Ordinal);
            double score=terms.Sum(term=>words.TryGetValue(term,out int count)?2+Math.Log(1+count):0);
            if(score<=0)continue;
            best.Add(new NotePassage($"pdf-{doc.Id}-p{page.Number}",$"PDF p. {page.Number} · {doc.Name}",passage,score));
            best=best.OrderByDescending(p=>p.Score).Take(limit).ToList();
        }
        return best;
    }
    public IEnumerable<DocumentBatch> Batches(string id,int maximum=10000,CancellationToken ct=default)
    {
        if(maximum is <1500 or >12000)throw new ArgumentOutOfRangeException(nameof(maximum));
        var buffer=new StringBuilder();int first=0,last=0;
        foreach(var page in ReadPages(id,ct))
        {
            if(string.IsNullOrWhiteSpace(page.Text))continue;
            for(int offset=0;offset<page.Text.Length;)
            {
                string header=$"\n[PDF PAGE {page.Number}]\n";
                if(buffer.Length+header.Length+1>maximum){yield return new(first,last,buffer.ToString());buffer.Clear();first=0;}
                if(first==0)first=page.Number;last=page.Number;
                buffer.Append(header);int take=Math.Min(page.Text.Length-offset,maximum-buffer.Length);buffer.Append(page.Text,offset,take);offset+=take;
                if(buffer.Length==maximum){yield return new(first,last,buffer.ToString());buffer.Clear();first=0;}
            }
        }
        if(buffer.Length>0)yield return new(first,last,buffer.ToString());
    }
    public void Delete(string id){var path=Folder(id);if(Directory.Exists(path))Directory.Delete(path,true);}
    public sealed class ImportSession:IDisposable
    {
        private readonly EncryptedDocumentStore _store;
        private readonly string _stage,_id=Guid.NewGuid().ToString("N"),_name,_hash;
        private readonly int _pages;
        private int _written,_empty;
        private long _characters;
        private bool _committed;
        internal ImportSession(EncryptedDocumentStore store,string name,int pages,string hash)
        {
            _store=store;_name=Path.GetFileName(name);_hash=hash;_pages=pages;
            if(_name.Length is <1 or >180||pages is <1 or >MaxPages||hash.Length!=64)throw new InvalidDataException("Invalid PDF metadata or document limit exceeded.");
            if(store.List().Count>=256)throw new InvalidDataException("Maximum 256 PDF documents.");
            Directory.CreateDirectory(store._root);_stage=Path.Combine(store._root,".import-"+_id);Directory.CreateDirectory(_stage);
        }
        public void Add(int page,string text,CancellationToken ct=default)
        {
            ct.ThrowIfCancellationRequested();if(_committed||page!=_written+1||page>_pages||text.Length>MaxPageCharacters||_characters+text.Length>MaxCharacters)throw new InvalidDataException("PDF extraction exceeds a page or document limit.");
            Save(Path.Combine(_stage,$"{page:D5}.dpapi"),new DocumentPage(page,text));_written++;_characters+=text.Length;if(string.IsNullOrWhiteSpace(text))_empty++;
        }
        public BrainDocument Complete(int ocrPages)
        {
            if(_written!=_pages||_characters==0)throw new InvalidDataException("The PDF contains no readable text or extraction is incomplete.");
            var doc=new BrainDocument(_id,_name,_pages,ocrPages,_empty,_characters,_hash,DateTimeOffset.UtcNow);
            Save(Path.Combine(_stage,"manifest.dpapi"),doc);Directory.Move(_stage,_store.Folder(_id));_committed=true;return doc;
        }
        public void Dispose()
        {
            // This exact generated staging folder is the only recursive cleanup target.
            if(!_committed&&Path.GetDirectoryName(_stage)==_store._root&&Path.GetFileName(_stage)==".import-"+_id&&Directory.Exists(_stage))Directory.Delete(_stage,true);
        }
    }
}
