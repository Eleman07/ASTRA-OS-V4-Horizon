using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace AstraOS.Core.Agents;

public sealed record DocumentTextSection(string Label, string Text);
public sealed record DocumentTextResult(string Name, string Kind, string Status, DocumentTextSection[] Sections, string Detail)
{
    public string Text => "Extraction status: " + Status + ". " + Detail + "\n\n" + string.Join("\n\n", Sections.Select(s => "[" + s.Label + "]\n" + s.Text));
}

/// <summary>Non-executing document adapters. Unsupported binaries are metadata only, never interpreted as text.</summary>
public static class DocumentTextReader
{
    private const long MaximumXmlBytes = 16_000_000;
    private const long MaximumExpandedXmlBytes = 64_000_000;
    private static readonly HashSet<string> Plain = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown", ".txt", ".log", ".json", ".jsonl", ".ndjson", ".csv", ".tsv", ".yaml", ".yml", ".xml", ".html", ".htm", ".css", ".scss", ".sql", ".toml", ".ini", ".cfg", ".tex", ".bib", ".rst", ".adoc",
        ".py", ".cs", ".c", ".cpp", ".cc", ".cxx", ".h", ".hpp", ".f", ".f90", ".for", ".m", ".r", ".jl", ".rs", ".go", ".java", ".kt", ".swift", ".js", ".ts", ".jsx", ".tsx", ".vue", ".svelte", ".ps1", ".sh", ".bat", ".cmd", ".sln", ".csproj", ".props", ".targets", ".svg"
    };
    private static readonly string[] Containers = [".pdf", ".docx", ".pptx", ".xlsx", ".odt", ".ods", ".odp", ".epub", ".ipynb"];
    public static readonly string[] SupportedExtensions = Plain.Concat(Containers).Order(StringComparer.OrdinalIgnoreCase).ToArray();
    public static bool CanRead(string path) => Plain.Contains(Path.GetExtension(path)) || Containers.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase) || Path.GetFileName(path).Equals("Dockerfile", StringComparison.OrdinalIgnoreCase);

    public static DocumentTextResult Read(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var info = new FileInfo(Path.GetFullPath(path));
        if (!info.Exists) throw new FileNotFoundException("Selected document no longer exists.", info.FullName);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked document files are not followed.");
        string kind = info.Extension.TrimStart('.').ToLowerInvariant();
        DocumentTextResult Result(string status, string detail) => new(info.Name, kind, status, [], detail);
        if (!CanRead(path)) return Result("metadata-only", $"Unsupported semantic format {info.Extension}; {info.Length} bytes, modified {info.LastWriteTimeUtc:O}. File content, images, audio, CAD geometry and embedded objects were not interpreted.");
        var builder = new Sections(info.Name, kind);
        try
        {
            if (kind == "pdf")
            {
                using var pdf = PdfDocument.Open(info.FullName);
                for (int number = 1; number <= pdf.NumberOfPages; number++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!builder.Add("Page " + number, ContentOrderTextExtractor.GetText(pdf.GetPage(number)))) break;
                }
                return builder.Finish("Selectable PDF text only; figures, scans, equations encoded as images and layout semantics require separate visual review.");
            }
            if (kind == "ipynb")
            {
                using var notebook = JsonDocument.Parse(File.ReadAllText(info.FullName), new JsonDocumentOptions { MaxDepth = 128 });
                int number = 0;
                foreach (var cell in notebook.RootElement.GetProperty("cells").EnumerateArray())
                {
                    ct.ThrowIfCancellationRequested(); number++;
                    string source = JsonText(cell.GetProperty("source"));
                    if (!builder.Add("Cell " + number + " (" + cell.GetProperty("cell_type").GetString() + ")", source)) break;
                    if (cell.TryGetProperty("outputs", out var outputs))
                        foreach (var output in outputs.EnumerateArray())
                        {
                            if (output.TryGetProperty("text", out var text)) builder.Add("Cell " + number + " saved text output", JsonText(text));
                            else if (output.TryGetProperty("data", out var data) && data.TryGetProperty("text/plain", out var plain)) builder.Add("Cell " + number + " saved text output", JsonText(plain));
                        }
                }
                return builder.Finish("Notebook source and saved plain-text outputs; code was not executed and output freshness was not verified.");
            }
            if (kind is "docx" or "pptx" or "xlsx" or "odt" or "ods" or "odp" or "epub")
            {
                ReadContainer(info.FullName, kind, builder, ct);
                return builder.Finish(kind == "xlsx" || kind == "ods" ? "Worksheet text and cached values only; formulas were not recalculated. Charts, macros, embedded objects and formatting were not interpreted." : "Document text only; embedded media, macros, annotations and visual layout were not interpreted.");
            }
            using var reader = new StreamReader(info.FullName, new UTF8Encoding(false, true), true);
            string raw = reader.ReadToEnd();
            if (raw.Contains('\0')) return Result("metadata-only", "The file contains NUL bytes and was not treated as plain text. Choose an appropriate binary-format adapter.");
            builder.Add("Text lines", raw);
            return builder.Finish("Text/code/data read as source content; scripts, formulas and markup were not executed.");
        }
        catch (OperationCanceledException) { throw; }
        catch (ResourceLimitException ex) { return builder.Finish(ex.Message, "resource-limit"); }
        catch (Exception ex) when (ex is not (OutOfMemoryException or AccessViolationException))
        {
            return builder.Finish("Extraction could not complete: " + ex.GetType().Name + ". " + ex.Message, builder.Count == 0 ? "metadata-only" : "partial");
        }
    }

    private static string JsonText(JsonElement element) => element.ValueKind == JsonValueKind.Array ? string.Concat(element.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : "")) : element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";
    private sealed class ResourceLimitException(string message) : IOException(message);
    private sealed class Sections(string name, string kind)
    {
        private readonly List<DocumentTextSection> _items = new();
        private string _partial = "";
        public int Count => _items.Count;
        public void Partial(string detail) => _partial = detail;
        public bool Add(string label, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            _items.Add(new(label, text));
            return true;
        }
        public DocumentTextResult Finish(string detail, string? status = null) => new(name, kind, status ?? (_partial.Length > 0 ? "partial" : _items.Count == 0 ? "no-text" : "read"), _items.ToArray(), detail + (_partial.Length > 0 ? " " + _partial : ""));
    }

    private static void ReadContainer(string path, string kind, Sections result, CancellationToken ct)
    {
        using var archive = ZipFile.OpenRead(path);
        if (archive.Entries.Count > 10000) throw new ResourceLimitException("Archive has over 10,000 entries; no unbounded expansion is allowed.");
        long expanded = 0;
        XDocument Xml(ZipArchiveEntry entry)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Length > MaximumXmlBytes || expanded + entry.Length > MaximumExpandedXmlBytes || (entry.Length > 1_000_000 && entry.Length / Math.Max(1, entry.CompressedLength) > 200)) throw new ResourceLimitException("Office/EPUB XML resource guard reached (16 MB per entry, 64 MB total, bounded compression ratio). Remaining parts were not read.");
            expanded += entry.Length;
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumXmlBytes });
            return XDocument.Load(reader);
        }
        XDocument Required(string name) => Xml(archive.GetEntry(name) ?? throw new InvalidDataException("Required document part is missing: " + name));
        if (kind == "docx")
        {
            var doc = Required("word/document.xml");
            XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            result.Add("Document paragraphs", string.Join("\n", doc.Descendants(word + "p").Select(p => string.Concat(p.Descendants(word + "t").Select(t => t.Value)))));
        }
        else if (kind == "pptx")
        {
            foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("ppt/slides/slide", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal) && !e.FullName.Contains("/_rels/", StringComparison.Ordinal)).OrderBy(e => PartNumber(e.FullName)))
            {
                XNamespace drawing = "http://schemas.openxmlformats.org/drawingml/2006/main";
                var doc = Xml(entry);
                if (!result.Add("Slide " + PartNumber(entry.FullName), string.Join("\n", doc.Descendants(drawing + "p").Select(p => string.Concat(p.Descendants(drawing + "t").Select(t => t.Value)))))) break;
            }
        }
        else if (kind == "xlsx")
        {
            XNamespace sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            string[] shared = archive.GetEntry("xl/sharedStrings.xml") is { } sharedEntry ? Xml(sharedEntry).Descendants(sheet + "si").Select(si => string.Concat(si.Descendants(sheet + "t").Select(t => t.Value))).ToArray() : [];
            foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal)).OrderBy(e => PartNumber(e.FullName)))
            {
                var body = new StringBuilder();
                foreach (var row in Xml(entry).Descendants(sheet + "row"))
                {
                    ct.ThrowIfCancellationRequested();
                    foreach (var cell in row.Elements(sheet + "c"))
                    {
                        string type = (string?)cell.Attribute("t") ?? "", value = cell.Element(sheet + "v")?.Value ?? "";
                        if (type == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)) value = index >= 0 && index < shared.Length ? shared[index] : "[invalid shared-string index]";
                        if (type == "inlineStr") value = string.Concat(cell.Descendants(sheet + "t").Select(t => t.Value));
                        string? formula = cell.Element(sheet + "f")?.Value;
                        body.Append((string?)cell.Attribute("r") ?? "cell").Append(": ").Append(value);
                        if (formula is not null) body.Append(" [saved formula: ").Append(formula).Append("; cached value, not recalculated]");
                        body.Append(" | ");
                    }
                    body.AppendLine();
                }
                if (!result.Add("Worksheet part " + PartNumber(entry.FullName), body.ToString())) break;
            }
        }
        else if (kind is "odt" or "ods" or "odp")
        {
            var doc = Required("content.xml");
            XNamespace text = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
            if (kind == "ods")
            {
                XNamespace table = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
                XNamespace office = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
                foreach (var tab in doc.Descendants(table + "table"))
                {
                    var body = new StringBuilder(); int number = 0;
                    foreach (var row in tab.Descendants(table + "table-row"))
                    {
                        ct.ThrowIfCancellationRequested(); body.Append("Row ").Append(++number).Append(": ");
                        foreach (var cell in row.Elements().Where(e => e.Name == table + "table-cell" || e.Name == table + "covered-table-cell"))
                        {
                            string value = string.Join(" ", cell.Descendants(text + "p").Select(p => p.Value));
                            if (value.Length == 0) value = (string?)cell.Attribute(office + "value") ?? (string?)cell.Attribute(office + "string-value") ?? "";
                            body.Append(value); string? formula = (string?)cell.Attribute(table + "formula");
                            if (formula is not null) body.Append(" [saved formula: ").Append(formula).Append(']');
                            string? repeated = (string?)cell.Attribute(table + "number-columns-repeated");
                            if (repeated is not null) body.Append(" [columns repeated ").Append(repeated).Append(']');
                            body.Append(" | ");
                        }
                        body.AppendLine();
                    }
                    if (!result.Add("Sheet " + ((string?)tab.Attribute(table + "name") ?? "unnamed"), body.ToString())) break;
                }
            }
            else result.Add("Document paragraphs", string.Join("\n", doc.Descendants().Where(e => e.Name == text + "p" || e.Name == text + "h").Select(e => e.Value)));
        }
        else if (kind == "epub")
        {
            // Read local XHTML parts only; no external links, scripts, DRM or binary resources are opened.
            foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) || e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || e.FullName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.FullName, StringComparer.Ordinal))
            {
                var doc = Xml(entry);
                string body = string.Join("\n", doc.Descendants().Where(e => new[] { "p", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "li" }.Contains(e.Name.LocalName) && !e.Ancestors().Any(a => a.Name.LocalName is "script" or "style")).Select(e => e.Value));
                if (!result.Add("EPUB part " + entry.FullName, body)) break;
            }
            result.Partial("EPUB XHTML parts were read in filename order; spine order, DRM and embedded media were not interpreted.");
        }
    }

    private static int PartNumber(string name)
    {
        string digits = new(name.Where(char.IsAsciiDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : 0;
    }
}
