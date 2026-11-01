using System.Security.Cryptography;
using System.Text;
using AstraOS.Core.SecondBrain;

namespace AstraOS.Core.Agents;

public sealed record MemoryVector(float[] Values);
/// <summary>Local model embeddings, encrypted content-addressed cache, streaming top-k ranking.</summary>
public sealed class SemanticMemory
{
    private readonly EncryptedRecords<MemoryVector> _cache;
    public SemanticMemory(string? directory = null) => _cache = new(directory ?? Path.Combine(AstraPaths.UserDataRoot, "brain", "vectors"));
    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += (double)a[i] * b[i]; na += (double)a[i] * a[i]; nb += (double)b[i] * b[i]; }
        double result = na > 0 && nb > 0 ? dot / Math.Sqrt(na * nb) : 0;
        return double.IsFinite(result) ? result : 0;
    }
    public async Task<IReadOnlyList<NotePassage>> SearchAsync(IEnumerable<BrainNote> notes, string query, string modelIdentity, Func<string, CancellationToken, Task<float[]>> embed, CancellationToken ct)
    {
        float[] question = await embed("search_query: "+query[..Math.Min(1200, query.Length)], ct);
        var best = new List<NotePassage>();
        foreach (var note in notes)
        for (int offset = 0; offset < note.Content.Length; offset += 1050)
        {
            ct.ThrowIfCancellationRequested();
            string body = note.Content.Substring(offset, Math.Min(1250, note.Content.Length - offset));
            string input = "search_document: "+note.Title + "\n" + body;
            string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(modelIdentity + "\n" + input)));
            var stored = _cache.Get(key);
            if (stored is null) { stored = new(await embed(input, ct)); _cache.Put(key, stored); }
            double score = Cosine(question, stored.Values);
            best.Add(new(note.Id, note.Title, body, score));
            best = best.OrderByDescending(p => p.Score).Take(6).ToList();
        }
        return best;
    }
}
