using System.Security.Cryptography;
using System.Text.Json;
using AstraOS.Core.Agents;

namespace AstraOS.Core.SecondBrain;

/// <summary>Per-note DPAPI storage with automatic, restartable migration of the original notebook.</summary>
public sealed class EncryptedNoteStore
{
    private readonly string _root;
    private readonly EncryptedRecords<BrainNote> _records;
    private readonly EncryptedRecords<bool> _metadata;
    public EncryptedNoteStore(string? directory = null)
    {
        _root = directory ?? Path.Combine(AstraPaths.UserDataRoot, "brain");
        _records = new(Path.Combine(_root, "notes-v2")); _metadata = new(Path.Combine(_root, "notes-v2-meta"));
    }
    private void Migrate()
    {
        using var guard = _records.Lock();
        if (_metadata.Get("migrated")) return;
        foreach (var note in new LegacyEncryptedNoteStore(_root).Load()) _records.Put(note.Id, note);
        // Retain the legacy ciphertext for recovery; migration never edits original note content.
        if (!File.Exists(Path.Combine(_root, "notes.dpapi"))) File.WriteAllBytes(Path.Combine(_root, "notes.dpapi"), ProtectedData.Protect("[]"u8.ToArray(), null, DataProtectionScope.CurrentUser));
        _metadata.Put("migrated", true);
    }
    public IReadOnlyList<BrainNote> Load() { Migrate(); return _records.Enumerate().OrderBy(n => n.CreatedUtc).ToArray(); }
    public IEnumerable<BrainNote> Enumerate() { Migrate(); return _records.Enumerate(); }
    private static string[] Tags(IEnumerable<string>? tags) => (tags ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Where(t => !t.Any(char.IsControl)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static void Validate(string title, string content)
    { if (string.IsNullOrWhiteSpace(title) || title.Length > 140 || string.IsNullOrWhiteSpace(content)) throw new ArgumentException("A note requires a title (up to 140 characters) and content."); }
    public void Add(string title, string content, IEnumerable<string>? tags = null)
    {
        Validate(title, content); Migrate(); var note = new BrainNote(Guid.NewGuid().ToString("N"), title.Trim(), content.Trim(), DateTimeOffset.UtcNow) { Tags = Tags(tags) };
        using var guard = _records.Lock(); _records.Put(note.Id, note);
    }
    public void Upsert(BrainNote note) { Validate(note.Title, note.Content); Migrate(); using var guard = _records.Lock(); _records.Put(note.Id, note with { Tags = Tags(note.Tags) }); }
    private sealed record NoteArchive(int SchemaVersion, BrainNote[] Notes);
    public int ImportArchive(string json)
    {
        NoteArchive archive;
        try { archive = JsonSerializer.Deserialize<NoteArchive>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 8 }) ?? throw new InvalidDataException("Empty archive."); }
        catch (JsonException ex) { throw new InvalidDataException("Invalid note archive.", ex); }
        if (archive.SchemaVersion != 1 || archive.Notes is null) throw new InvalidDataException("Unsupported note archive.");
        // Validate every note before writing any of them.
        foreach (var note in archive.Notes)
        { if (note is null || string.IsNullOrWhiteSpace(note.Title) || note.Title.Length > 140 || string.IsNullOrWhiteSpace(note.Content)) throw new InvalidDataException("Invalid note metadata; no notes imported."); }
        Migrate(); using var guard = _records.Lock();
        foreach (var source in archive.Notes) { var note = source with { Id = Guid.NewGuid().ToString("N"), Tags = Tags(source.Tags) }; _records.Put(note.Id, note); }
        return archive.Notes.Length;
    }
    public void Delete(string id) { Migrate(); using var guard = _records.Lock(); _records.Delete(id); }
    public void Clear()
    {
        Migrate(); using var guard = _records.Lock();
        foreach (var note in _records.Enumerate()) _records.Delete(note.Id);
        File.Delete(Path.Combine(_root, "notes.dpapi"));
    }
}
