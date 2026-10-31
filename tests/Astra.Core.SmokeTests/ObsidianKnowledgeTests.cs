using System.IO.Compression;
using System.Text;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;

internal static class ObsidianKnowledgeTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        var defaults = new AgentRuntimeSettings();
        check(defaults.PreferFreeCloud && !defaults.FallbackToGpu && !defaults.UseGpu && !defaults.SemanticSearch && defaults.OutputTokens == 0 && defaults.ContextSize == 1000000, "new agent settings use NVIDIA cloud without local inference or an app output cap");
        string vault = Path.Combine(root, "knowledge-vault"); Directory.CreateDirectory(Path.Combine(vault, "02-Study"));
        File.WriteAllText(Path.Combine(vault, "02-Study", "Orbit.md"), "# Orbital control\nReference frames are explicit.\nQuaternion damping must preserve units.\nSource: course notes.\n");
        File.WriteAllText(Path.Combine(vault, "Controller.py"), "# quaternion damping fixture\ngain = 0.25\n# This code has not been executed.\n");
        foreach (string excluded in new[] { ".obsidian", ".venv", "node_modules", ".toolchain" })
        {
            Directory.CreateDirectory(Path.Combine(vault, excluded));
            File.WriteAllText(Path.Combine(vault, excluded, "Quaternion-secret.md"), "quaternion SHOULD_NOT_LEAK");
        }
        File.WriteAllText(Path.Combine(vault, ".env"), "quaternion SHOULD_NOT_LEAK");
        Directory.CreateDirectory(Path.Combine(vault,"90 Templates"));
        File.WriteAllText(Path.Combine(vault,"90 Templates","Quaternion template.md"),"# Generic quaternion template\nFill in your project assumptions.\n");
        var passages = ObsidianKnowledge.Search(vault, "quaternion damping", 20);
        check(passages.Count == 2 && passages.All(p => !p.Text.Contains("SHOULD_NOT_LEAK")) && passages.Any(p => p.Location.Contains("Controller.py")), "vault retrieval reads relevant code and notes while excluding credentials and dependency folders");
        check(passages.All(p => p.Id.StartsWith("note-vault-", StringComparison.Ordinal) && p.Location.Contains(":L") && p.Text.Contains("not a full-file reading")), "vault evidence retains stable citation IDs, source paths, lines and explicit excerpt status");
        check(passages.All(p=>!p.Location.Contains("90 Templates"))&&ObsidianKnowledge.Search(vault,"quaternion template",20).Any(p=>p.Location.Contains("90 Templates")&&p.Text.Contains("Generic editable scaffold; not factual")),"generic templates are excluded from factual retrieval and explicitly labeled when requested as scaffolds");
        File.WriteAllText(Path.Combine(vault, "Connections.md"), "# Knowledge map\n[[Orbit]]\n[[02-Study/Orbit|Orbital notes]]\n");
        File.WriteAllText(Path.Combine(vault,"Path Alias Only.md"),"[[02-Study/Orbit#Reference|Alias unrelated to target name]]");
        Directory.CreateDirectory(Path.Combine(vault,"02-Study","Nested"));
        File.WriteAllText(Path.Combine(vault,"02-Study","Nested","Relative Only.md"),"[[../Orbit#Reference|Different display label]]");
        var graphNotes = ObsidianKnowledge.GraphNotes(vault);
        check(graphNotes.Any(n => n.Title == "Connections" && n.Content.Contains("[[Orbit]]") && n.Content.Contains("Obsidian relative path: Connections.md")) && graphNotes.All(n => !n.Content.Contains("SHOULD_NOT_LEAK")), "vault graph records Markdown links and file locators while excluding dependency and credential folders");
        check(graphNotes.Any(n => n.Title == "Controller.py" && n.Content.Contains("Graph metadata only") && !n.Content.Contains("gain = 0.25")) && passages.All(p => graphNotes.Any(n => "note-" + n.Id == p.Id)), "non-Markdown graph nodes contain metadata only and share file identity with retrieved evidence");
        var linkedGraph=BrainGraphBuilder.Build(graphNotes,Array.Empty<BrainDocument>(),Array.Empty<AgentJob>());
        string orbitNode="note-"+graphNotes.Single(n=>n.Title=="Orbit").Id;
        check(new[]{"Path Alias Only","Relative Only"}.All(title=>linkedGraph.Edges.Any(e=>e.Source=="note-"+graphNotes.Single(n=>n.Title==title).Id&&e.Target==orbitNode&&e.Relation=="wiki link")),"knowledge graph resolves aliased wiki targets, heading anchors and ../ links using the original vault path");
        check(ObsidianKnowledge.Search(vault, "unmatched-nebula-term").Count == 0, "retrieval does not supply irrelevant notes when no query term matches");
        bool relativeRejected = false; try { ObsidianKnowledge.ValidateVaultPath("relative-vault"); } catch (ArgumentException) { relativeRejected = true; }
        check(relativeRejected, "vault selection rejects ambiguous relative paths");

        string docx = Path.Combine(vault, "Flight.docx");
        Archive(docx, new() { ["word/document.xml"] = "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p><w:r><w:t>Quaternion flight document</w:t></w:r></w:p></w:body></w:document>" });
        var word = DocumentTextReader.Read(docx);
        check(word.Status == "read" && word.Text.Contains("Quaternion flight document"), "DOCX adapter extracts paragraph text without executing embedded content");
        string slides = Path.Combine(vault, "Brief.pptx");
        Archive(slides, new() { ["ppt/slides/slide2.xml"] = Slide("Second slide"), ["ppt/slides/slide1.xml"] = Slide("First slide") });
        var ppt = DocumentTextReader.Read(slides);
        check(ppt.Sections[0].Label == "Slide 1" && ppt.Sections[1].Text.Contains("Second slide"), "PPTX adapter preserves numbered slide references");
        string sheet = Path.Combine(vault, "Trade.xlsx");
        Archive(sheet, new() { ["xl/sharedStrings.xml"] = "<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><si><t>Orbital state</t></si></sst>", ["xl/worksheets/sheet1.xml"] = "<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData><row r='1'><c r='A1' t='s'><v>0</v></c><c r='B1'><f>2+2</f><v>4</v></c></row></sheetData></worksheet>" });
        var spreadsheet = DocumentTextReader.Read(sheet);
        check(spreadsheet.Text.Contains("A1: Orbital state") && spreadsheet.Text.Contains("B1: 4") && spreadsheet.Text.Contains("not recalculated"), "XLSX adapter resolves shared strings and labels formula values as cached");
        foreach (string extension in new[] { ".odt", ".odp" })
        {
            string path = Path.Combine(vault, "OpenDocument" + extension);
            Archive(path, new() { ["content.xml"] = "<office:document-content xmlns:office='urn:oasis:names:tc:opendocument:xmlns:office:1.0' xmlns:text='urn:oasis:names:tc:opendocument:xmlns:text:1.0'><text:p>Open engineering text</text:p></office:document-content>" });
            check(DocumentTextReader.Read(path).Text.Contains("Open engineering text"), extension + " adapter reads OpenDocument paragraphs");
        }
        string ods = Path.Combine(vault, "OpenSheet.ods");
        Archive(ods, new() { ["content.xml"] = "<office:document-content xmlns:office='urn:oasis:names:tc:opendocument:xmlns:office:1.0' xmlns:table='urn:oasis:names:tc:opendocument:xmlns:table:1.0'><table:table table:name='Mass budget'><table:table-row><table:table-cell office:value='42'/></table:table-row></table:table></office:document-content>" });
        check(DocumentTextReader.Read(ods).Text.Contains("Mass budget") && DocumentTextReader.Read(ods).Text.Contains("42"), "ODS adapter extracts sheet names and stored cell values");
        string epub = Path.Combine(vault, "Book.epub"); Archive(epub, new() { ["chapter1.xhtml"] = "<html xmlns='http://www.w3.org/1999/xhtml'><body><p>Quaternion textbook passage</p></body></html>" });
        var book = DocumentTextReader.Read(epub);
        check(book.Status == "partial" && book.Text.Contains("Quaternion textbook passage") && book.Detail.Contains("spine order"), "EPUB text extraction reports its reading-order limitation explicitly");
        string notebook = Path.Combine(vault, "Analysis.ipynb");
        File.WriteAllText(notebook, "{\"cells\":[{\"cell_type\":\"code\",\"source\":[\"print(42)\"],\"outputs\":[{\"text\":[\"42\\n\"]},{\"data\":{\"image/png\":\"NOT_IMAGE_TEXT\"}}]}]}");
        var nb = DocumentTextReader.Read(notebook);
        check(nb.Text.Contains("print(42)") && nb.Text.Contains("saved text output") && !nb.Text.Contains("NOT_IMAGE_TEXT") && nb.Detail.Contains("not executed"), "notebook adapter reads source and saved text while excluding binary image payloads");
        string pdf = Path.Combine(vault, "Reference.pdf"); File.WriteAllBytes(pdf, Pdf());
        var paper = DocumentTextReader.Read(pdf);
        check(paper.Status == "read" && paper.Text.Contains("Orbital quaternion control") && paper.Sections[0].Label == "Page 1", "PDF adapter extracts selectable text with page references");
        check(ObsidianKnowledge.Search(vault, "quaternion", 30).Any(p => p.Location.Contains("Reference.pdf") && p.Location.Contains("Page 1")), "vault retrieval searches PDF passages and retains page provenance");
        string binary = Path.Combine(vault, "Telemetry.bin"); File.WriteAllBytes(binary, [0, 255, 20, 0]);
        var unsupported = DocumentTextReader.Read(binary);
        check(unsupported.Status == "metadata-only" && unsupported.Sections.Length == 0, "unsupported binaries are explicitly metadata-only rather than fabricated semantic content");
        string longText = Path.Combine(root, "retained-long-source.txt"); string longSource = new string('x', 2_100_000)+"FULL_INPUT_TAIL_MARKER"; File.WriteAllText(longText, longSource);
        var excerpt = DocumentTextReader.Read(longText);
        check(excerpt.Status == "read" && excerpt.Sections.Single().Text == longSource && excerpt.Text.EndsWith("FULL_INPUT_TAIL_MARKER",StringComparison.Ordinal) && File.ReadAllText(longText).Length == longSource.Length, "text extraction retains full multi-million-character input and its tail without an ASTRA size or character cutoff");
        string expanded = Path.Combine(root, "expanded.docx"); Archive(expanded, new() { ["word/document.xml"] = "<document>" + new string('x', 1_500_000) + "</document>" });
        check(DocumentTextReader.Read(expanded).Status == "resource-limit", "compressed Office XML expansion guard reports its limit before constructing a huge document tree");
        string hostile = Path.Combine(root, "hostile.docx");
        Archive(hostile, new() { ["word/document.xml"] = "<!DOCTYPE document [<!ENTITY external SYSTEM 'file:///not-read'>]><document>&external;</document>" });
        check(DocumentTextReader.Read(hostile).Status == "metadata-only", "Office XML adapter rejects DTD/external-entity content without opening external resources");

        var job = new AgentJob { Id = "../not-a-path", Goal = "Quaternion study deliverable", Status = "Completed", QualityNeedsReview = true, Steps = [new AgentStep { AgentId = "study", Output = "UNVERIFIED study draft", Status = "Completed" }], Evidence = passages.ToList() };
        string exported = ObsidianKnowledge.ExportDeliverable(vault, job);
        string content = File.ReadAllText(exported);
        check(Path.GetRelativePath(vault, exported).StartsWith(Path.Combine("ASTRA", "Deliverables"), StringComparison.Ordinal) && content.Contains("review: needs-review") && content.Contains("[[02-Study/Orbit.md|Orbit]]"), "completed deliverables export to ASTRA's own folder with original-note wiki links and review state");
        File.AppendAllText(exported, "\nUser revision to preserve."); ObsidianKnowledge.ExportDeliverable(vault, job);
        check(File.ReadAllText(exported).Contains("User revision to preserve."), "repeated vault export preserves user edits instead of overwriting an existing result");
        var graph = BrainGraphBuilder.Build(Array.Empty<BrainNote>(), Array.Empty<BrainDocument>(), [job]);
        check(passages.All(p => graph.Nodes.Any(n => n.Id == p.Id)) && graph.Edges.Any(e => e.Relation == "used source"), "Obsidian passages participate in the ASTRA evidence graph");
        try
        {
            string outside = Path.Combine(root, "outside-vault"); Directory.CreateDirectory(outside); File.WriteAllText(Path.Combine(outside, "Secret.md"), "quaternion SHOULD_NOT_LEAK");
            string link = Path.Combine(vault, "Linked-outside"); Directory.CreateSymbolicLink(link, outside);
            check(ObsidianKnowledge.Search(vault, "quaternion", 30).All(p => !p.Text.Contains("SHOULD_NOT_LEAK")), "vault traversal excludes linked directories outside the selected root");
            Directory.Delete(link);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or PlatformNotSupportedException or IOException) { Console.WriteLine("SKIP: symlink fixture unavailable on this host; no links were followed."); }
    }
    private static string Slide(string text) => "<p:sld xmlns:p='http://schemas.openxmlformats.org/presentationml/2006/main' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></p:sld>";
    private static void Archive(string path, Dictionary<string, string> parts)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var part in parts) { using var stream = zip.CreateEntry(part.Key).Open(); using var writer = new StreamWriter(stream, new UTF8Encoding(false)); writer.Write(part.Value); }
    }
    private static byte[] Pdf()
    {
        string content = "BT /F1 12 Tf 72 720 Td (Orbital quaternion control) Tj ET";
        string[] objects = ["<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", "<< /Length " + content.Length + " >>\nstream\n" + content + "\nendstream"];
        var body = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (int i = 0; i < objects.Length; i++) { offsets.Add(body.Length); body.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n"); }
        int xref = body.Length; body.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (int offset in offsets.Skip(1)) body.Append(offset.ToString("D10")).Append(" 00000 n \n");
        body.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(body.ToString());
    }
}
