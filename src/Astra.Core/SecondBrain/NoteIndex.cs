using System.Text;
using System.Text.RegularExpressions;

namespace AstraOS.Core.SecondBrain;

public sealed record BrainNote(string Id, string Title, string Content, DateTimeOffset CreatedUtc)
{
    public string[] Tags { get; init; } = Array.Empty<string>();
}
public sealed record NotePassage(string NoteId, string Title, string Text, double Score);

/// <summary>Bounded offline passage retrieval with BM25-style lexical ranking. No embeddings or network.</summary>
public static class NoteIndex
{
    private static readonly Regex Tokens = new(@"[\p{L}\p{N}]{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static IEnumerable<string> Terms(string s) => Tokens.Matches(s.ToLowerInvariant()).Select(m => m.Value);

    // Each passage is <= 1,250 chars; overlaps avoid losing boundary information.
    private static IEnumerable<string> Chunks(string body)
    {
        for (var pos = 0; pos < body.Length; pos += 1050)
        {
            yield return body.Substring(pos, Math.Min(1250, body.Length - pos));
            if (pos + 1250 >= body.Length) break;
        }
    }

    public static IReadOnlyList<NotePassage> RetrievePassages(IEnumerable<BrainNote> notes, string question, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(question)) return Array.Empty<NotePassage>();
        var query = Terms(question).Distinct().Take(32).ToArray();
        if (query.Length == 0) return Array.Empty<NotePassage>();
        var corpus = notes.Where(n => !string.IsNullOrWhiteSpace(n.Content))
            .SelectMany(n => Chunks(n.Content).Select(chunk => (n.Id, n.Title, chunk,
                terms: Terms(n.Title + " " + string.Join(" ", n.Tags ?? Array.Empty<string>()) + " " + chunk).Take(900).ToArray(),
                titleTerms: Terms(n.Title).ToHashSet(StringComparer.Ordinal))))
            .ToArray();
        if (corpus.Length == 0) return Array.Empty<NotePassage>();
        var avgLength = corpus.Average(x => (double)Math.Max(1, x.terms.Length));
        var df = query.ToDictionary(t => t, t => corpus.Count(p => p.terms.Contains(t, StringComparer.Ordinal)));
        var ranked = corpus.Select(p =>
        {
            var frequencies = p.terms.GroupBy(t => t, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            double score = 0;
            foreach (var term in query)
            {
                if (!frequencies.TryGetValue(term, out var tf)) continue;
                var idf = Math.Log(1.0 + (corpus.Length - df[term] + .5) / (df[term] + .5));
                score += idf * tf * 2.2 / (tf + 1.2 * (.25 + .75 * p.terms.Length / avgLength));
                if (p.titleTerms.Contains(term)) score += idf * 1.5;
            }
            return new NotePassage(p.Id, p.Title, p.chunk, score);
        });
        return ranked.Where(x => x.Score > 0).OrderByDescending(x => x.Score)
            .Take(Math.Clamp(limit, 1, 6)).ToArray();
    }

    public static IReadOnlyList<BrainNote> Retrieve(IEnumerable<BrainNote> notes, string question, int limit = 4)
    {
        var materialized = notes.ToArray();
        var ids = RetrievePassages(materialized, question, limit).Select(p => p.NoteId).Distinct().ToHashSet();
        return materialized.Where(n => ids.Contains(n.Id)).Take(limit).ToArray();
    }

    // [NOTA: ...] and [/NOTA] are *trusted* delimiters produced by our code.
    // Never run the note-content sanitizer on the whole assembled context:
    // doing so destroys these delimiters and breaks both citations and tests.
    public static string BuildContext(IEnumerable<NotePassage> passages)
    {
        const int maxContextChars = 8000;
        var result = new StringBuilder();
        foreach (var passage in passages.Take(6))
        {
            // Titles and IDs are untrusted as well. Keep them on one line so
            // neither can inject a forged note boundary into a header.
            var id = SanitizeData(passage.NoteId ?? "", 70).Replace('\r', ' ').Replace('\n', ' ');
            var title = SanitizeData(passage.Title ?? "", 90).Replace('\r', ' ').Replace('\n', ' ');
            var header = $"[NOTA: {id} | titolo: {title}]\n";
            const string footer = "\n[/NOTA]\n";
            var available = maxContextChars - result.Length - header.Length - footer.Length;
            if (available <= 0) break;

            // Clip data before appending the *trusted* wrapper. This maintains
            // intact delimiters and stays within the total context budget.
            var data = SanitizeData(passage.Text ?? "", Math.Min(1250, available));
            result.Append(header).Append(data).Append(footer);
        }
        return result.ToString();
    }
    public static string BuildContext(IEnumerable<BrainNote> notes) => BuildContext(notes.Take(6)
        .Select(n => new NotePassage(n.Id, n.Title, n.Content, 1)));

    private static string SanitizeData(string input, int maximum)
    {
        // Untrusted text may contain fake note delimiters, in any letter case.
        // Defang them without touching the authentic wrapper added above.
        // Do not allocate proportional to arbitrarily long imported notes.
        var bounded = input[..Math.Min(input.Length, maximum)];
        var s = bounded.Replace("\0", "")
            .Replace("[/NOTA]", "[fine blocco]", StringComparison.OrdinalIgnoreCase)
            .Replace("[NOTA:", "[citazione:", StringComparison.OrdinalIgnoreCase);
        return s.Length <= maximum ? s : s[..maximum];
    }
}
