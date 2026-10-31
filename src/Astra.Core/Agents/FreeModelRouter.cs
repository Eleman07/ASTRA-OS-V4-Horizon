using System.Globalization;
using System.Text.Json;

namespace AstraOS.Core.Agents;

public sealed record FreeModelRoute(string Id, string Name, long ContextTokens, int MaxOutputTokens, string Reason);

/// <summary>Routes only models that are currently advertised as compatible, zero-price :free endpoints.</summary>
public static class FreeModelRouter
{
    private sealed record Candidate(string Id, string Name, long Created, long Context, int Output, string[] Inputs, string[] Outputs);

    public static FreeModelRoute Resolve(string catalogJson, string preferred, string modality = "text")
    {
        if (modality is not ("text" or "image" or "audio")) throw new ArgumentException("Supported route modalities are text, image and audio.", nameof(modality));
        using var document = JsonDocument.Parse(catalogJson);
        if (!document.RootElement.TryGetProperty("data", out var models) || models.ValueKind != JsonValueKind.Array) throw new InvalidDataException("OpenRouter catalog did not contain a model array; no provider was selected.");
        var candidates = new List<Candidate>();
        bool preferredListed = false, preferredPriced = false;
        foreach (var model in models.EnumerateArray())
        {
            string id = String(model, "id"), name = String(model, "name");
            bool exact = id.Equals(preferred, StringComparison.Ordinal);
            preferredListed |= exact;
            if (!id.EndsWith(":free", StringComparison.Ordinal) || Contains(id + " " + name, "qwen", "safety", "moderation", "classifier", "guard")) continue;
            if (!ZeroPrices(model)) { if (exact) preferredPriced = true; continue; }
            string[] inputs = [], outputs = [];
            if (model.TryGetProperty("architecture", out var architecture))
            {
                inputs = Strings(architecture, "input_modalities"); outputs = Strings(architecture, "output_modalities");
                string described = String(architecture, "modality");
                string[] sides = described.Split("->", StringSplitOptions.TrimEntries);
                if (inputs.Length == 0 && sides.Length > 0) inputs = sides[0].Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (outputs.Length == 0 && sides.Length == 2) outputs = sides[1].Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            if (!inputs.Contains(modality, StringComparer.OrdinalIgnoreCase) || !outputs.Contains("text", StringComparer.OrdinalIgnoreCase)) continue;
            long context = Number(model, "context_length"), output = Number(model, "max_completion_tokens");
            if (model.TryGetProperty("top_provider", out var provider))
            {
                long advertisedContext = Number(provider, "context_length"), advertisedOutput = Number(provider, "max_completion_tokens");
                if (advertisedContext > 0) context = advertisedContext;
                if (advertisedOutput > 0) output = advertisedOutput;
            }
            candidates.Add(new(id, string.IsNullOrWhiteSpace(name) ? id : name, Number(model, "created"), context, output is > 0 and <= int.MaxValue ? (int)output : 0, inputs, outputs));
        }
        var preferredCandidate = candidates.FirstOrDefault(c => c.Id.Equals(preferred, StringComparison.Ordinal));
        if (preferredCandidate is not null) return Route(preferredCandidate, "Preferred compatible zero-price endpoint is currently available.");

        string reason = preferredListed ? preferredPriced ? "Preferred endpoint is no longer verified as zero-price." : "Preferred endpoint is excluded or does not advertise the required input/output capability." : "Preferred endpoint is not listed in the current model catalog.";
        string vendor = preferred.Split('/')[0];
        var family = candidates.Where(c => SameFamily(c.Id, preferred, vendor)).OrderByDescending(c => c.Created).ThenByDescending(c => c.Context).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
        if (family is not null) return Route(family, reason + " Selected the newest compatible zero-price endpoint in the same vendor/family. Model behavior and quality may differ.");
        var fallback = candidates.OrderBy(c => FallbackRank(c.Id, modality, vendor)).ThenByDescending(c => c.Created).ThenByDescending(c => c.Context).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
        if (fallback is not null) return Route(fallback, reason + " Selected another advertised compatible zero-price endpoint. This is a substitution; equivalent quality is not assumed.");
        throw new InvalidOperationException(reason + " No compatible zero-price :free endpoint for " + modality + " input and text output was found. No paid or local-model fallback was selected.");
    }

    private static FreeModelRoute Route(Candidate model, string reason) => new(model.Id, model.Name, model.Context, model.Output, reason);
    private static bool SameFamily(string id, string preferred, string vendor)
    {
        if (!id.StartsWith(vendor + "/", StringComparison.OrdinalIgnoreCase)) return false;
        if (preferred.Contains("ultra", StringComparison.OrdinalIgnoreCase)) return Contains(id, "nemotron") && Contains(id, "ultra");
        if (preferred.Contains("omni", StringComparison.OrdinalIgnoreCase)) return Contains(id, "omni");
        if (preferred.Contains("vl", StringComparison.OrdinalIgnoreCase)) return Contains(id, "super") && Contains(id, "vl");
        if (preferred.Contains("north", StringComparison.OrdinalIgnoreCase)) return Contains(id, "north") && Contains(id, "mini");
        if (preferred.Contains("inkling", StringComparison.OrdinalIgnoreCase)) return Contains(id, "inkling");
        string family = preferred.Split('/').Last().Split(':')[0].Split('-')[0];
        return family.Length > 2 && id.Split('/').Last().StartsWith(family, StringComparison.OrdinalIgnoreCase);
    }
    private static int FallbackRank(string id, string modality, string preferredVendor)
    {
        bool nvidia = id.StartsWith("nvidia/", StringComparison.OrdinalIgnoreCase);
        if (modality == "audio") return nvidia && Contains(id, "omni") ? 0 : 10;
        if (modality == "image") return nvidia && Contains(id, "super") && Contains(id, "vl") ? 0 : nvidia && Contains(id, "omni") ? 1 : 10;
        if (preferredVendor.Equals("cohere", StringComparison.OrdinalIgnoreCase) && id.StartsWith("cohere/", StringComparison.OrdinalIgnoreCase) && Contains(id, "north") && Contains(id, "mini")) return 0;
        if (Contains(id, "inkling")) return 1;
        if (nvidia && Contains(id, "nemotron") && Contains(id, "super") && !Contains(id, "vl")) return 2;
        if (nvidia && Contains(id, "nemotron") && !Contains(id, "omni", "vl")) return 3;
        if (id.StartsWith("cohere/", StringComparison.OrdinalIgnoreCase) && Contains(id, "north") && Contains(id, "mini")) return 4;
        return 10;
    }
    private static bool ZeroPrices(JsonElement model)
    {
        if (!model.TryGetProperty("pricing", out var pricing) || pricing.ValueKind != JsonValueKind.Object || !pricing.TryGetProperty("prompt", out _) || !pricing.TryGetProperty("completion", out _)) return false;
        foreach (var price in pricing.EnumerateObject())
        {
            string value = price.Value.ValueKind == JsonValueKind.String ? price.Value.GetString() ?? "" : price.Value.GetRawText();
            if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal amount) || amount != 0) return false;
        }
        return true;
    }
    private static string String(JsonElement parent, string property) => parent.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    private static long Number(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var value)) return 0;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number)) return Math.Max(0, number);
        return value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? Math.Max(0, number) : 0;
    }
    private static string[] Strings(JsonElement parent, string property) => parent.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString() ?? "").ToArray() : [];
    private static bool Contains(string value, params string[] terms) => terms.Any(t => value.Contains(t, StringComparison.OrdinalIgnoreCase));
}
