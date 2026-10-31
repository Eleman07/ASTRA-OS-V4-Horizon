using System.Security.Cryptography;
using System.Text.Json;

namespace AstraOS.Core.SecondBrain;

/// <summary>Notes are encrypted at rest with Windows DPAPI CurrentUser, not sent to ASTRA servers.</summary>
internal sealed class LegacyEncryptedNoteStore
{
    private readonly string _path;
    private readonly object _gate = new();
    public LegacyEncryptedNoteStore(string? directory = null)
    {
        string root = directory ?? Path.Combine(AstraPaths.UserDataRoot, "brain");
        _path = Path.Combine(root, "notes.dpapi");
    }
    public IReadOnlyList<BrainNote> Load()
    {
        lock (_gate) return LoadCore();
    }
    private IReadOnlyList<BrainNote> LoadCore()
    {
        if (!File.Exists(_path)) return Array.Empty<BrainNote>();
        // Do not silently overwrite irrecoverable ciphertext: wrong Windows account / corrupted store.
        if (new FileInfo(_path).Length > 16_000_000) throw new InvalidDataException("Archivio note troppo grande.");
        var protectedData = File.ReadAllBytes(_path);
        if (protectedData.Length > 16_000_000) throw new InvalidDataException("Archivio note troppo grande.");
        var plaintext = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
        try
        {
            var notes = JsonSerializer.Deserialize<List<BrainNote>>(plaintext) ?? new();
            if (notes.Any(n => n is null || string.IsNullOrWhiteSpace(n.Id) || n.Id.Length > 160 ||
                string.IsNullOrWhiteSpace(n.Title) || n.Title.Length > 140 || n.Content is null || n.Content.Length > 16_000))
                throw new InvalidDataException("Archivio note: metadati non validi.");
            notes = notes.Select(n => n with { Tags = NormalizeTags(n.Tags) }).ToList();
            return notes;
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
    public void Add(string title, string content, IEnumerable<string>? tags = null)
    {
        title = title.Trim(); content = content.Trim();
        if (title.Length is < 1 or > 140 || content.Length is < 1 or > 16_000)
            throw new ArgumentException("Titolo o nota fuori dai limiti consentiti.");
        lock (_gate)
        {
            var notes = LoadCore().ToList();
            notes.Add(new BrainNote(Guid.NewGuid().ToString("N"), title, content, DateTimeOffset.UtcNow) { Tags = NormalizeTags(tags) });
            Save(notes);
        }
    }
    private static string[] NormalizeTags(IEnumerable<string>? tags) => (tags ?? Array.Empty<string>())
        .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim())
        .Where(t => t.Length <= 40 && !t.Any(char.IsControl)).Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToArray();

    public int ImportArchive(string json)
    {
        if (json.Length > 16_000_000) throw new InvalidDataException("Archivio importato troppo grande.");
        NoteArchive archive;
        try { archive = JsonSerializer.Deserialize<NoteArchive>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 8 }) ?? throw new InvalidDataException("Archivio vuoto."); }
        catch (JsonException ex) { throw new InvalidDataException("JSON note non valido.", ex); }
        if (archive.SchemaVersion != 1 || archive.Notes is null)
            throw new InvalidDataException("Versione o numero note non supportati.");
        var imported = new List<BrainNote>();
        foreach (var note in archive.Notes)
        {
            if (note is null || string.IsNullOrWhiteSpace(note.Title) || note.Title.Length > 140 ||
                string.IsNullOrWhiteSpace(note.Content) || note.Content.Length > 16_000)
                throw new InvalidDataException("Metadati nota non validi. Nessuna nota importata.");
            // Imported IDs are never used as file paths or trusted identifiers.
            imported.Add(new BrainNote(Guid.NewGuid().ToString("N"), note.Title.Trim(), note.Content.Trim(), note.CreatedUtc)
            { Tags = NormalizeTags(note.Tags) });
        }
        lock (_gate)
        {
            var notes = LoadCore().ToList();
            notes.AddRange(imported); Save(notes);
        }
        return imported.Count;
    }
    private sealed record NoteArchive(int SchemaVersion, BrainNote[] Notes);
    public void Delete(string id)
    {
        lock (_gate)
        {
            var notes = LoadCore().ToList();
            notes.RemoveAll(n => n.Id == id);
            Save(notes);
        }
    }
    /// <summary>Delete this account's notes only after explicit UI confirmation.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            if (File.Exists(_path)) File.Delete(_path);
        }
    }

    private void Save(List<BrainNote> notes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(notes);
        try
        {
            if (plaintext.Length > 15_900_000) throw new InvalidDataException("Dimensione massima archivio raggiunta.");
            var protectedData = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
            string temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temp, protectedData); File.Move(temp, _path, overwrite: true); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
}
