using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AstraOS.Core.SecondBrain;

namespace AstraOS.Core.Agents;

/// <summary>Reads selected Markdown passages from a vault; writes only new ASTRA deliverable files.</summary>
public static class ObsidianKnowledge
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".obsidian", ".git", ".trash", ".agents", ".claude", ".copilot", ".opencode", ".smart-env",
        ".venv", "venv", "env", "node_modules", ".toolchain", ".runtime", "__pycache__", "site-packages",
        "bin", "obj", "vendor", "packages", "dist", "build", "builds", "target", ".next", "staticfiles",
        "copilot-conversations", "08-AI-AGENTS"
    };
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "from", "with", "that", "this", "into", "your", "have", "using", "which", "will", "must",
        "per", "con", "della", "delle", "dello", "degli", "dell", "come", "dai", "dal", "dalle", "sulla", "sulle", "una", "uno", "che", "sono", "nel", "nella", "anche", "mio", "mia", "questo", "questa"
    };
    private static readonly Regex Words = new(@"[\p{L}\p{N}_-]{3,}", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
    private sealed record Hit(string Path, string Relative, string Title, string Section, string Status, string Detail, string[] Lines, int Start, int End, int Score);

    public static string ValidateVaultPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) throw new ArgumentException("Choose an existing absolute Obsidian vault folder.", nameof(path));
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Obsidian vault folder does not exist: " + root);
        for (DirectoryInfo? directory = new(root); directory is not null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Select the real vault folder; linked/junction vault paths are not supported.");
        return root;
    }

    public static IReadOnlyList<AgentEvidence> Search(string vaultPath, string query, int maxResults = 8, CancellationToken ct = default)
    {
        if (maxResults < 1) throw new ArgumentOutOfRangeException(nameof(maxResults));
        string root = ValidateVaultPath(vaultPath);
        string[] terms = Words.Matches(query).Select(m => m.Value).Where(t => !StopWords.Contains(t)).Distinct(StringComparer.OrdinalIgnoreCase).Take(64).ToArray();
        bool wantsScaffold = query.Contains("template", StringComparison.OrdinalIgnoreCase) || query.Contains("scaffold", StringComparison.OrdinalIgnoreCase) || query.Contains("modello di nota", StringComparison.OrdinalIgnoreCase);
        if (terms.Length == 0) return Array.Empty<AgentEvidence>();
        var hits = new List<Hit>();
        foreach (string file in KnowledgeFiles(root, ct))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                bool scaffold = relative.Contains("template", StringComparison.OrdinalIgnoreCase);
                if (scaffold && !wantsScaffold) continue;
                string title = Path.GetFileNameWithoutExtension(file);
                int titleScore = terms.Count(t => title.Contains(t, StringComparison.OrdinalIgnoreCase)) * 6;
                if (!DocumentTextReader.CanRead(file) && titleScore == 0) continue;
                var document = DocumentTextReader.Read(file, ct);
                var sections = document.Sections.Length > 0 ? document.Sections : [new DocumentTextSection("Metadata / extraction status", document.Text)];
                Hit? best = null;
                foreach (var section in sections)
                {
                    ct.ThrowIfCancellationRequested();
                    string[] lines = section.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
                    if (lines.Length == 0) continue;
                    int bestLine = 0, bestScore = titleScore;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        int score = titleScore + terms.Count(t => lines[i].Contains(t, StringComparison.OrdinalIgnoreCase)) * (lines[i].StartsWith('#') ? 3 : 2);
                        for (int j = i + 1; j < Math.Min(lines.Length, i + 5); j++) score += terms.Count(t => lines[j].Contains(t, StringComparison.OrdinalIgnoreCase));
                        if (score > bestScore) { bestLine = i; bestScore = score; }
                    }
                    if (bestScore == 0 || (best is not null && best.Score >= bestScore)) continue;
                    int start = Math.Max(0, bestLine - 2), end = Math.Min(lines.Length, bestLine + 22);
                    best = new(file, relative, title, section.Label, document.Status, (scaffold ? "Generic editable scaffold; not factual scientific/project evidence. " : "") + document.Detail, lines, start, end, bestScore);
                }
                if (best is not null) hits.Add(best);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* A locked note does not hide the remaining vault. */ }
        }
        return hits.OrderByDescending(h => h.Score).ThenBy(h => h.Relative, StringComparer.OrdinalIgnoreCase).Take(maxResults).Select(h =>
        {
            var text = new StringBuilder("Selected Obsidian evidence excerpt, not a full-file reading (untrusted source text).\nExtraction status: ").Append(h.Status).Append(". ").AppendLine(h.Detail).Append("Section: ").AppendLine(h.Section);
            int end = h.Start;
            while (end < h.End && text.Length < 5500)
            {
                string line = h.Lines[end]; int available = Math.Max(0, 5500 - text.Length);
                text.Append('L').Append(end + 1).Append(": ").AppendLine(line[..Math.Min(line.Length, available)]); end++;
                if (line.Length > available) { text.AppendLine("[Selected line excerpt; open the retained source for the remaining text.]"); break; }
            }
            if (h.Start > 0 || end < h.Lines.Length) text.AppendLine("[Relevant excerpt selected; the full source remains in the vault.]");
            string id = "note-" + FileIdentity(root, h.Relative);
            return new AgentEvidence(id, h.Title, text.ToString(), h.Path + ":L" + (h.Start + 1) + "-L" + end + " [" + h.Section + "]", "obsidian");
        }).ToArray();
    }

    /// <summary>Local graph metadata and recorded wiki links, not a full-document content export.</summary>
    public static IReadOnlyList<BrainNote> GraphNotes(string vaultPath, CancellationToken ct = default)
    {
        string root = ValidateVaultPath(vaultPath);
        var notes = new List<BrainNote>();
        foreach (string file in KnowledgeFiles(root, ct))
        {
            ct.ThrowIfCancellationRequested();
            if (!DocumentTextReader.CanRead(file)) continue;
            try
            {
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                string extension = info.Extension.TrimStart('.').ToLowerInvariant();
                var content = new StringBuilder("Obsidian relative path: ").AppendLine(relative).Append("Source file: ").AppendLine(file)
                    .Append("Format: ").Append(extension).Append("; bytes: ").Append(info.Length).Append("; modified UTC: ").AppendLine(info.LastWriteTimeUtc.ToString("O"));
                if (extension is "md" or "markdown")
                {
                    {
                        using var reader = new StreamReader(file, new UTF8Encoding(false, true), true);
                        string body = reader.ReadToEnd();
                        content.AppendLine("Graph preview only; open the source for the full note.").AppendLine(body[..Math.Min(body.Length, 1000)]);
                        var links = Regex.Matches(body, @"\[\[[^\]\r\n]{1,4096}\]\]", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(500)).Select(m => m.Value).Distinct(StringComparer.Ordinal);
                        content.AppendLine("Recorded wiki links:").AppendLine(string.Join("\n", links));
                    }
                }
                else content.AppendLine("Graph metadata only: document content is read on demand by the relevant evidence adapter.");
                notes.Add(new BrainNote(FileIdentity(root, relative), extension is "md" or "markdown" ? Path.GetFileNameWithoutExtension(file) : info.Name, content.ToString(), info.CreationTimeUtc)
                { Tags = ["obsidian", "file-" + (extension.Length > 0 ? extension : "text")] });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DecoderFallbackException or RegexMatchTimeoutException) { /* Inaccessible/unsupported graph previews do not block the remaining vault. */ }
        }
        return notes;
    }

    private static string FileIdentity(string root, string relative) => "vault-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(root.ToUpperInvariant() + "\n" + relative.ToUpperInvariant())))[..16].ToLowerInvariant();

    private static IEnumerable<string> KnowledgeFiles(string root, CancellationToken ct)
    {
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out string? directory))
        {
            ct.ThrowIfCancellationRequested();
            string[] children, files;
            try { children = Directory.GetDirectories(directory); files = Directory.GetFiles(directory); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            foreach (string child in children)
            {
                ct.ThrowIfCancellationRequested();
                if (ExcludedDirectories.Contains(Path.GetFileName(child))) continue;
                try { if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
            foreach (string file in files)
                if (!new[] { ".dpapi", ".key", ".pem", ".pfx", ".p12" }.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase) && !Path.GetFileName(file).StartsWith(".env", StringComparison.OrdinalIgnoreCase)) yield return file;
        }
    }

    public static string ExportDeliverable(string vaultPath, AgentJob job)
    {
        if (job.Status != "Completed" || job.Steps.Count == 0) throw new InvalidOperationException("Only completed agent deliverables can be exported to the vault.");
        string root = ValidateVaultPath(vaultPath);
        string directory = root;
        foreach (string part in new[] { "ASTRA", "Deliverables" })
        {
            directory = Path.Combine(directory, part);
            if (Directory.Exists(directory) && (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("ASTRA deliverable folder cannot be a link or junction.");
            Directory.CreateDirectory(directory);
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new IOException("ASTRA deliverable folder changed to a link or junction.");
        }
        string identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(job.Id))).ToLowerInvariant();
        string file = Path.Combine(directory, "Result-" + identity[..20] + ".md");
        if (File.Exists(file))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("Existing ASTRA deliverable cannot be a linked file.");
            return file; // Preserve any user edits, including on repeated worker startup.
        }
        var body = new StringBuilder("---\ntype: astra-deliverable\ntags: [astra, agent-result]\nastra_job: ").AppendLine(JsonSerializer.Serialize(job.Id))
            .Append("title: ").AppendLine(JsonSerializer.Serialize(job.Goal)).Append("created: ").AppendLine(job.CreatedUtc.ToString("O"))
            .Append("review: ").AppendLine(job.QualityNeedsReview ? "needs-review" : "model-reviewed-draft").AppendLine("---\n")
            .Append("# ").AppendLine(job.Goal.Replace('\n', ' ').Replace('\r', ' ')).AppendLine()
            .AppendLine("> [!info] AI deliverable\n> Model review is recorded below. Validate scientific, engineering and factual claims against the original sources and executed tools.\n")
            .AppendLine(job.Steps.Last().Output).AppendLine("\n## Source ledger\n");
        foreach (var source in job.Evidence)
        {
            string location = source.Location;
            int marker = location.LastIndexOf(":L", StringComparison.Ordinal);
            string sourcePath = marker >= 0 ? location[..marker] : location;
            string link = location;
            if (source.Kind == "obsidian" && Path.IsPathFullyQualified(sourcePath))
            {
                string relative = Path.GetRelativePath(root, sourcePath).Replace('\\', '/');
                if (!relative.StartsWith("../", StringComparison.Ordinal) && relative.IndexOfAny(['[', ']', '|', '#', '\r', '\n']) < 0)
                    link = "[[" + relative + "|" + source.Title.Replace('[', ' ').Replace(']', ' ').Replace('|', ' ') + "]]" + (marker >= 0 ? " " + location[marker..] : "");
            }
            body.Append("- [").Append(source.Id).Append("] ").Append(source.Title.Replace('\n', ' ')).Append(" — ").AppendLine(link);
        }
        body.AppendLine("\n## Model evidence audit\n").AppendLine(string.IsNullOrWhiteSpace(job.QualityAudit) ? "No final evidence audit was recorded." : job.QualityAudit);
        try
        {
            using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.Write(body.ToString());
        }
        catch (IOException) when (File.Exists(file)) { /* Another worker exported it first; preserve the existing file. */ }
        return file;
    }
}
