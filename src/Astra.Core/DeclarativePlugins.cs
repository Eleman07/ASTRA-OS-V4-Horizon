using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AstraOS.Core;

/// <summary>
/// Public plugin format v3: data-only text operations. No interpreted scripts,
/// shell, file-system, network, native library, or arbitrary executable invocation.
/// </summary>
public sealed record DeclarativePlugin(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("prefix")] string? Prefix)
{
    [JsonPropertyName("category")] public string Category { get; init; } = "Strumenti";
    [JsonPropertyName("example")] public string Example { get; init; } = "";
}

public static class DeclarativePlugins
{
    private static readonly Regex SafeId = new(@"\A[a-z0-9][a-z0-9.-]{2,63}\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> Operations = new(StringComparer.Ordinal)
    { "text.upper", "text.lower", "text.trim", "text.prefix", "text.wordcount", "text.numberlines", "text.todos", "text.outline" };

    public static DeclarativePlugin Validate(string json, string folderName)
    {
        if (json.Length > 8192) throw new InvalidDataException("Manifesto superiore a 8 KB.");
        var plugin = JsonSerializer.Deserialize<DeclarativePlugin>(json,
            new JsonSerializerOptions { MaxDepth = 8 }) ?? throw new InvalidDataException("Manifesto vuoto.");
        if (plugin.SchemaVersion != 3 || string.IsNullOrWhiteSpace(plugin.Id) || !SafeId.IsMatch(plugin.Id) || plugin.Id != folderName ||
            string.IsNullOrWhiteSpace(plugin.Name) || plugin.Name.Length > 80 ||
            plugin.Description is null || plugin.Description.Length > 240 ||
            string.IsNullOrWhiteSpace(plugin.Id) || plugin.Operation is null ||
            (!Operations.Contains(plugin.Operation) && !PluginLibrary.Tools.Any(t => t.Operation == plugin.Operation)) ||
            plugin.Category is null || plugin.Category.Length > 40 || plugin.Example is null || plugin.Example.Length > 2000 ||
            (plugin.Prefix?.Length ?? 0) > 120)
            throw new InvalidDataException("Manifesto dichiarativo non valido.");
        return plugin;
    }

    public static string Transform(DeclarativePlugin plugin, string input)
    {
        if (input is null || input.Length > 4000) throw new ArgumentException("Input fuori dai limiti.");
        var result = plugin.Operation switch
        {
            "text.upper" => input.ToUpperInvariant(),
            "text.lower" => input.ToLowerInvariant(),
            "text.trim" => input.Trim(),
            "text.prefix" => (plugin.Prefix ?? "") + input,
            "text.wordcount" => "Parole: " + input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "text.numberlines" => string.Join("\n", input.Replace("\r\n", "\n").Split('\n').Select((line, index) => $"{index + 1,3} | {line}")),
            "text.todos" => string.Join("\n", input.Replace("\r\n", "\n").Split('\n').Where(line => line.Contains("TODO", StringComparison.OrdinalIgnoreCase) || line.Contains("FIXME", StringComparison.OrdinalIgnoreCase)).Take(30)),
            "text.outline" => string.Join("\n", input.Replace("\r\n", "\n").Split('\n').Where(line => line.TrimStart().StartsWith('#')).Take(40)),
            _ => PluginLibrary.Transform(plugin.Operation, input)
        };
        if (result.Length > 4200) throw new InvalidDataException("Output fuori dai limiti.");
        return result;
    }
}
