using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AstraOS.Core.Agents;

internal static class AgentArtifactExporterTests
{
    public static async Task RunAsync(string root, Action<bool, string> check)
    {
        var utf8 = new UTF8Encoding(false, true);
        string parent = Path.Combine(root, "agent-artifacts");
        AgentJob Completed(string output) => new() { Status = "Completed", Goal = "Produce a Python software project", Steps = [new() { AgentId = "coordinator", Status = "Completed", Output = output }] };
        bool Rejected(string output, string suffix)
        {
            try { AgentArtifactExporter.Export(Path.Combine(root, "invalid-artifact-" + suffix), Completed(output)); return false; }
            catch (Exception ex) when (ex is InvalidDataException or ArgumentException or IOException) { return true; }
        }
        string untouched = Path.Combine(root, "no-artifact-fences");
        check(AgentArtifactExporter.Export(untouched, Completed("A plan and a normal code block:\n```python\nprint('proposal')\n```\n")) is null && !Directory.Exists(untouched), "ordinary prose and language code fences do not cause any file export");
        string nestedSample = "````markdown\nExample syntax only:\n```file:not-a-real-artifact.py\nprint('sample')\n```\n````\n";
        check(AgentArtifactExporter.Export(untouched, Completed(nestedSample)) is null, "file-fence examples inside an ordinary Markdown code block are not treated as files");
        string longBody = "# Complete long source π\r\n" + new string('x', 180_000) + "\r\nARTIFACT_TAIL\r\n";
        string readme = "# Proposed project\n\n```python\nprint('example')\n```\n";
        string executionMarker = Path.Combine(root, "artifact-not-executed.marker");
        string script = "from pathlib import Path\nPath(" + JsonSerializer.Serialize(executionMarker) + ").write_text('wrong')\n";
        string output = "```file:src/long.py\r\n" + longBody + "```\r\n\n````file:README.md\n" + readme + "````\n\n```file:run.py\n" + script + "```\n\n```file:src/__init__.py\n```\n";
        var job = Completed(output);
        var export = AgentArtifactExporter.Export(parent, job)!;
        check(export.Files.Count == 4 && Directory.Exists(export.Directory) && File.Exists(export.ManifestPath) && export.Directory.StartsWith(Path.GetFullPath(parent) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "explicit file fences create a concrete project directory and provenance manifest");
        check(File.ReadAllBytes(Path.Combine(export.Directory, "src", "long.py")).SequenceEqual(utf8.GetBytes(longBody)) && File.ReadAllText(Path.Combine(export.Directory, "README.md"), utf8) == readme && new FileInfo(Path.Combine(export.Directory, "src", "__init__.py")).Length == 0, "complete long UTF-8 content, original line endings, nested Markdown and empty files survive export");
        check(export.Files.All(file => { byte[] bytes = File.ReadAllBytes(Path.Combine(export.Directory, file.Path.Replace('/', Path.DirectorySeparatorChar))); return file.Bytes == bytes.LongLength && file.Sha256 == Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(); }), "every artifact reports the SHA-256 and byte count of its actual file");
        using (var manifest = JsonDocument.Parse(File.ReadAllText(export.ManifestPath)))
            check(!manifest.RootElement.GetProperty("executedByExporter").GetBoolean() && manifest.RootElement.GetProperty("kind").GetString() == "agent-proposed-text-files", "the artifact manifest explicitly distinguishes exported code proposals from execution evidence");
        check(!File.Exists(executionMarker) && job.Status == "Completed" && job.ArtifactDirectory.Length == 0, "the exporter never executes code or mutates job metadata");
        string originalHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(export.Directory, "run.py"))));
        var second = AgentArtifactExporter.Export(parent, job)!;
        check(second.Directory != export.Directory && originalHash == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(export.Directory, "run.py")))), "re-export always creates a new directory and preserves prior artifacts without overwriting");
        string[] unsafePaths = ["../escape.py", "src/../../escape.py", "C:/absolute.py", "/absolute.py", "\\\\server\\share\\file.py", "src\\..\\escape.py", "code.py:secret", "src//file.py", "NUL.txt", "src/trailing.\u0020", "src/CON.py"];
        for (int index = 0; index < unsafePaths.Length; index++)
            check(Rejected("```file:" + unsafePaths[index] + "\ntext\n```\n", "path-" + index), "artifact path rejects unsafe form " + index);
        check(!File.Exists(Path.Combine(root, "escape.py")) && !Directory.Exists(Path.Combine(root, "invalid-artifact-path-0")), "invalid artifact paths are rejected before any destination is created");
        check(Rejected("```file:src/a.py\none\n```\n```file:src/A.py\ntwo\n```\n", "duplicate"), "case-insensitive duplicate artifact paths are rejected");
        check(Rejected("```file:src\nfile\n```\n```file:src/a.py\nother\n```\n", "file-directory"), "an artifact cannot simultaneously be a file and a parent directory");
        check(Rejected("```file:binary.dat\nbefore\0after\n```\n", "nul") && Rejected("```file:invalid.txt\n\ud800\n```\n", "unicode"), "binary NUL and invalid UTF-8 source strings are rejected without replacement");
        check(Rejected("```file:incomplete.py\nnot closed", "unclosed") && Rejected("```file:ASTRA-ARTIFACTS.json\n{}\n```\n", "manifest"), "unclosed file fences and collisions with the provenance manifest are rejected");
        string parentFile = Path.Combine(root, "artifact-parent-file.txt"); File.WriteAllText(parentFile, "PRESERVE");
        try { AgentArtifactExporter.Export(parentFile, job); check(false, "file destination parent rejected"); }
        catch (IOException) { check(File.ReadAllText(parentFile) == "PRESERVE", "an existing file cannot be replaced by an artifact destination directory"); }
        string outside = Path.Combine(root, "artifact-link-outside"), link = Path.Combine(root, "artifact-linked-parent");
        Directory.CreateDirectory(outside); File.WriteAllText(Path.Combine(outside, "sentinel.txt"), "LINK_TARGET_PRESERVED");
        try
        {
            Directory.CreateSymbolicLink(link, outside);
            bool rejected = false; try { AgentArtifactExporter.Export(Path.Combine(link, "new-project"), job); } catch (IOException) { rejected = true; }
            check(rejected && Directory.Exists(link) && File.ReadAllText(Path.Combine(outside, "sentinel.txt")) == "LINK_TARGET_PRESERVED" && !Directory.Exists(Path.Combine(outside, "new-project")), "linked destination ancestors are rejected and both the link and its target remain untouched");
            Directory.Delete(link);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException) { Console.WriteLine("SKIP: artifact symlink fixture unavailable; exporter link checks remain enabled."); }

        var store = new AgentJobStore(Path.Combine(root, "artifact-contract-engine")); var software = store.Enqueue("Build a Python software package with tests", ["engineering"]); bool contractSeen = false;
        var engine = new AgentEngine(store, new FixtureTools(), (system, _, _) => { if (system.Contains("final synthesis agent")) contractSeen = system.Contains("file:relative/path.py") && system.Contains("deterministic ASTRA tool") && system.Contains("not executed code"); return Task.FromResult(system.Contains("FINAL_EVIDENCE_AUDIT") ? "{\"status\":\"PASS\",\"issues\":[]}" : "```file:README.md\nProposed code; not executed.\n```\n"); });
        await engine.RunAsync(software.Id, default);
        check(contractSeen && AgentArtifactExporter.Export(Path.Combine(root, "artifact-engine-result"), store.Records.Get(software.Id)!) is { Files.Count: 1 }, "software synthesis requests complete files and preserves deterministic-tool authorship and execution boundaries");
    }

    private sealed class FixtureTools : IAgentTools
    {
        public Task<IReadOnlyList<AgentEvidence>> GatherAsync(Specialist agent, AgentJob job, CancellationToken ct) => Task.FromResult<IReadOnlyList<AgentEvidence>>([]);
    }
}
