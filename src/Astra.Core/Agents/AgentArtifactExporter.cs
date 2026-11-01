using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AstraOS.Core.Agents;

public sealed record AgentArtifactFile(string Path, string Sha256, long Bytes);
public sealed record AgentArtifactExport(string Directory, IReadOnlyList<AgentArtifactFile> Files, string ManifestPath);

/// <summary>Export explicit UTF-8 file proposals into a new directory; never execute model-generated code.</summary>
public static class AgentArtifactExporter
{
    private const string ManifestName = "ASTRA-ARTIFACTS.json";
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private sealed record ProposedArtifact(string RelativePath, byte[] Bytes);

    public const string SoftwareDeliverableContract = "For a requested software project, deliver actual complete file contents, not just a plan. Use fenced file blocks: ```file:relative/path.py followed by a newline, the complete UTF-8 file content, and a closing ``` line. Include a README, dependency specification and runnable tests where relevant. Use four or more backticks around a file containing triple-backtick Markdown fences. Never use absolute paths, traversal, placeholders for omitted code or binary content. These files are proposed code, not executed code: claim execution or test passes only when an original tool record proves them. If a deterministic ASTRA tool has already created and tested project files, cite that tool evidence and its actual artifact paths; do not claim that you generated or tested those files yourself.";

    public static bool IsSoftwareRequest(string goal) => new[] { "python", "software", "code", "codice", "script", "repository", "matlab", "c++", "c#", "javascript", "typescript", "unit test", "pytest" }
        .Any(term => goal.Contains(term, StringComparison.OrdinalIgnoreCase));

    /// <summary>Require concrete files for explicit software-project delivery, not conceptual coding questions or tutoring.</summary>
    public static bool RequireFileArtifacts(string goal)
    {
        string text=goal.Trim().ToLowerInvariant();
        bool Has(params string[] words)=>words.Any(text.Contains);
        if(Has("without files","without source files","without creating files","do not create files","senza file","non creare file","senza creare file"))return false;
        if(new[]{"how ","how do ","what ","why ","come ","perché ","perche ","cos'è "}.Any(text.StartsWith))return false;
        bool explanation=new[]{"explain ","describe ","spiega ","spiegami ","descrivi "}.Any(text.StartsWith);
        if(explanation&&!Has("and deliver","then deliver","e consegna","poi consegna","and create the files","and write the files"))return false;
        return Has("project","progetto","simulator","simulatore","simulazione","repository")
            &&Has("software","python","matlab","c++","c#","javascript","typescript","code","codice","programma","application","applicazione")
            &&Has("file","deliver","consegna","artifact","artefatt")
            &&Has("build","create","generate","produce","develop","implement","write","deliver","realizza","sviluppa","costruisci","crea","genera","produci","consegna","scrivi");
    }

    /// <summary>Validate explicit file proposals with the exact exporter parser, without creating files or directories.</summary>
    public static int ProposedFileCount(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return Parse(output).Count;
    }

    public static AgentArtifactExport? Export(string destinationParent, AgentJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.Status != "Completed") throw new InvalidOperationException("Only a completed agent deliverable can be exported as proposed files.");
        string output = job.Steps.LastOrDefault()?.Output ?? "";
        var proposed = Parse(output);
        if (proposed.Count == 0) return null;
        string parent = Path.GetFullPath(destinationParent); SafeFileOperations.NoLinks(parent);
        if (File.Exists(parent)) throw new IOException("The artifact destination parent is a file.");
        System.IO.Directory.CreateDirectory(parent); SafeFileOperations.NoLinks(parent);
        string stage = Path.Combine(parent, ".astra-artifacts-stage-" + Guid.NewGuid().ToString("N"));
        string final = Path.Combine(parent, "ASTRA-Project-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        if (File.Exists(stage) || System.IO.Directory.Exists(stage) || File.Exists(final) || System.IO.Directory.Exists(final)) throw new IOException("The unique artifact destination already exists.");
        var written = new List<string>(); var directories = new List<string>(); var files = new List<AgentArtifactFile>();
        System.IO.Directory.CreateDirectory(stage); SafeFileOperations.NoLinks(stage);
        try
        {
            foreach (var artifact in proposed)
            {
                string target = Resolve(stage, artifact.RelativePath);
                string relativeDirectory = Path.GetDirectoryName(artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar)) ?? "";
                string current = stage;
                foreach (string segment in relativeDirectory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, segment); SafeFileOperations.NoLinks(current);
                    if (!System.IO.Directory.Exists(current)) { System.IO.Directory.CreateDirectory(current); directories.Add(current); }
                    SafeFileOperations.NoLinks(current);
                }
                SafeFileOperations.NoLinks(target);
                using (var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(artifact.Bytes);
                written.Add(target);
                files.Add(new(artifact.RelativePath, Convert.ToHexString(SHA256.HashData(artifact.Bytes)).ToLowerInvariant(), artifact.Bytes.LongLength));
            }
            string manifest = Resolve(stage, ManifestName);
            byte[] metadata = Utf8.GetBytes(JsonSerializer.Serialize(new { schemaVersion = 1, jobId = job.Id, kind = "agent-proposed-text-files", executedByExporter = false, evidenceNeedsReview = job.QualityNeedsReview, createdUtc = DateTimeOffset.UtcNow, files }, new JsonSerializerOptions { WriteIndented = true }));
            using (var stream = new FileStream(manifest, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(metadata);
            written.Add(manifest); SafeFileOperations.NoLinks(stage); SafeFileOperations.NoLinks(parent);
            System.IO.Directory.Move(stage, final);
            return new(final, files.ToArray(), Path.Combine(final, ManifestName));
        }
        catch
        {
            // Remove only our exact new files/empty directories; unexpected or linked entries are preserved.
            foreach (string file in written.AsEnumerable().Reverse()) { try { SafeFileOperations.NoLinks(file); if (File.Exists(file)) File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
            foreach (string directory in directories.AsEnumerable().Reverse().Append(stage)) { try { SafeFileOperations.NoLinks(directory); if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, false); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
            throw;
        }
    }

    private static IReadOnlyList<ProposedArtifact> Parse(string text)
    {
        var artifacts = new List<ProposedArtifact>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int position = 0;
        while (TryLine(text, position, out string line, out int next))
        {
            position = next;
            string trimmed = line.TrimStart(' '); int ticks = 0; while (ticks < trimmed.Length && trimmed[ticks] == '`') ticks++;
            if (ticks < 3) continue;
            string fence = new('`', ticks); string info = trimmed[ticks..];
            bool explicitFile = info.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
            if (!explicitFile && string.IsNullOrWhiteSpace(info)) continue;
            int bodyStart = position, closing = -1;
            while (TryLine(text, position, out string bodyLine, out int afterLine))
            {
                string candidate = bodyLine.Trim();
                if (candidate == fence) { closing = position; position = afterLine; break; }
                position = afterLine;
            }
            if (!explicitFile) continue;
            if (closing < 0) throw new InvalidDataException("An explicit file fence was not closed; no artifacts were exported.");
            string path = ValidatePath(info[5..].Trim());
            if (path.Equals(ManifestName, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The generated artifact manifest name is reserved.");
            if (!names.Add(path)) throw new InvalidDataException("Duplicate artifact path: " + path);
            string content = text[bodyStart..closing];
            if (content.Contains('\0')) throw new InvalidDataException("Artifact files must be text; NUL/binary content is not allowed.");
            byte[] bytes;
            try { bytes = Utf8.GetBytes(content); } catch (EncoderFallbackException ex) { throw new InvalidDataException("Artifact content is not valid UTF-8 text.", ex); }
            artifacts.Add(new(path, bytes));
        }
        foreach (string name in names)
            if (names.Any(other => other.Length > name.Length && other.StartsWith(name + "/", StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("An artifact path is also used as a directory: " + name);
        return artifacts;
    }

    private static bool TryLine(string text, int start, out string line, out int next)
    {
        if (start >= text.Length) { line = ""; next = text.Length; return false; }
        int end = text.IndexOf('\n', start); if (end < 0) end = text.Length;
        int visibleEnd = end > start && text[end - 1] == '\r' ? end - 1 : end;
        line = text[start..visibleEnd]; next = end < text.Length ? end + 1 : end; return true;
    }

    private static string ValidatePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith('/') || Path.IsPathRooted(normalized) || normalized.Contains(':')) throw new InvalidDataException("Artifact paths must be relative and cannot contain drives or alternate data streams.");
        foreach (string segment in normalized.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') || segment.Any(character => char.IsControl(character) || "<>:\"|?*".Contains(character))) throw new InvalidDataException("Unsafe artifact path: " + path);
            string device = segment.Split('.')[0].ToUpperInvariant();
            if (device is "CON" or "PRN" or "AUX" or "NUL" || device.Length == 4 && (device.StartsWith("COM") || device.StartsWith("LPT")) && device[3] is >= '0' and <= '9') throw new InvalidDataException("Windows device names are not allowed as artifact paths.");
        }
        return normalized;
    }

    private static string Resolve(string directory, string relative)
    {
        string prefix = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Artifact path escapes its new directory.");
        return full;
    }
}
