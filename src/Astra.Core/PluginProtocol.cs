using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AstraOS.Core;

public sealed record PluginManifest(
    [property: JsonPropertyName("apiVersion")] int ApiVersion,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("entryPoint")] string EntryPoint,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("capabilities")] string[] Capabilities);

public sealed record PluginRequest([property: JsonPropertyName("apiVersion")] int ApiVersion,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("text")] string Text);
public sealed record PluginResponse([property: JsonPropertyName("text")] string Text);

public static class PluginProtocol
{
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9.-]{2,63}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ExePattern = new("^[a-zA-Z0-9._-]{1,80}\\.exe$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ShaPattern = new("^[A-Fa-f0-9]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static PluginManifest Validate(string json, string folderName)
    {
        // Plugin manifests are untrusted input. Reject malformed JSON with the same
        // documented exception type as a structurally invalid plugin manifest.
        if (string.IsNullOrWhiteSpace(json) || json.Length > 16_384)
            throw new InvalidDataException("Manifesto plugin vuoto o troppo grande.");

        PluginManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<PluginManifest>(json)
                ?? throw new InvalidDataException("Manifesto plugin nullo.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Manifesto plugin JSON non valido.", ex);
        }

        // Deserialization can produce null properties even when the record declares
        // non-nullable C# strings. Check them before calling regex or .Length.
        if (manifest.ApiVersion != 2 || manifest.Id is null || !IdPattern.IsMatch(manifest.Id) ||
            !string.Equals(manifest.Id, folderName, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 90 ||
            manifest.Description is null || manifest.Description.Length > 400 ||
            manifest.EntryPoint is null || !ExePattern.IsMatch(manifest.EntryPoint) ||
            manifest.EntryPoint.Contains("..", StringComparison.Ordinal) ||
            manifest.Sha256 is null || !ShaPattern.IsMatch(manifest.Sha256) ||
            manifest.Capabilities is null || manifest.Capabilities.Length != 1 ||
            manifest.Capabilities[0] != "text.transform")
            throw new InvalidDataException("Plugin non valido: protocollo v2 richiede hash SHA-256 e capacità text.transform.");

        return manifest;
    }
}
