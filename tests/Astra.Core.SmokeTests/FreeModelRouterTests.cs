using System.Text.Json;
using AstraOS.Core.Agents;

internal static class FreeModelRouterTests
{
    public static void Run(Action<bool, string> check)
    {
        const string ultra = "nvidia/nemotron-3-ultra-550b-a55b:free";
        const string inkling = "thinking-machines/inkling:free";
        string Catalog(params Dictionary<string, object?>[] models) => JsonSerializer.Serialize(new { data = models });
        var exact = Model(ultra, 100); exact["top_provider"] = new { context_length = 1000000, max_completion_tokens = 65536 };
        var route = FreeModelRouter.Resolve(Catalog(exact, Model("nvidia/nemotron-4-ultra:free", 200)), ultra);
        check(route.Id == ultra && route.ContextTokens == 1000000 && route.MaxOutputTokens == 65536, "free router honors an available preferred endpoint and its advertised context/output maximum");
        route = FreeModelRouter.Resolve(Catalog(Model("nvidia/nemotron-4-ultra:free", 200), Model("nvidia/nemotron-5-ultra:free", 300), Model(inkling, 400)), ultra);
        check(route.Id == "nvidia/nemotron-5-ultra:free" && route.Reason.Contains("same vendor/family"), "removed model routes to the newest verified free compatible family member");
        var paid = Model(ultra, 500); paid["pricing"] = new { prompt = "0.001", completion = "0" };
        route = FreeModelRouter.Resolve(Catalog(paid, Model(inkling, 100)), ultra);
        check(route.Id == inkling && route.Reason.Contains("no longer verified as zero-price"), "an exact model that starts charging is rejected before a free substitute is selected");
        route = FreeModelRouter.Resolve(Catalog(Model(ultra, 500), Model("nvidia/nemotron-3.5-super-vl:free", 100, ["text", "image"])), ultra, "image");
        check(route.Id.Contains("super-vl") && route.Reason.Contains("capability"), "text-only exact endpoints cannot satisfy image requests");
        var imagePrice = Model("nvidia/nemotron-super-vl:free", 500, ["text", "image"]); imagePrice["pricing"] = new { prompt = "0", completion = "0", image = "0.01" };
        route = FreeModelRouter.Resolve(Catalog(imagePrice, Model("nvidia/nemotron-omni:free", 100, ["text", "image", "audio"])), "nvidia/missing-super-vl:free", "image");
        check(route.Id == "nvidia/nemotron-omni:free", "nonzero auxiliary image pricing disqualifies an otherwise free endpoint");
        var requestPrice = Model(ultra, 100); requestPrice["pricing"] = new { prompt = "0", completion = "0", request = "0.001" };
        route = FreeModelRouter.Resolve(Catalog(requestPrice, Model(inkling, 1)), ultra);
        check(route.Id == inkling, "nonzero per-request pricing cannot slip through free routing");
        var audioPrice = Model("nvidia/nemotron-omni:free", 200, ["text", "audio"]); audioPrice["pricing"] = new { prompt = "0", completion = "0", input_audio = "0.0001" };
        route = FreeModelRouter.Resolve(Catalog(audioPrice, Model("example/audio-text:free", 100, ["audio", "text"])), "nvidia/nemotron-omni:free", "audio");
        check(route.Id == "example/audio-text:free", "nonzero auxiliary audio pricing also prevents a paid route");
        route = FreeModelRouter.Resolve(Catalog(Model("qwen/very-new:free", 1000), Model("nvidia/nemotron-safety-classifier:free", 1100), Model(inkling, 1)), "qwen/very-new:free");
        check(route.Id == inkling, "Qwen endpoints and safety classifiers are excluded even when preferred or newer");
        route = FreeModelRouter.Resolve(Catalog(Model("cohere/north-mini-new:free", 30), Model(inkling, 100)), "cohere/retired-coder:free");
        check(route.Id == "cohere/north-mini-new:free", "Cohere coding preferences favor the free North Mini family when the original endpoint disappears");
        route = FreeModelRouter.Resolve(Catalog(Model(inkling, 1)), ultra);
        check(route.MaxOutputTokens == 0 && route.Reason.Contains("equivalent quality is not assumed"), "an unknown provider output maximum remains unspecified and substitution does not promise equal power");
        bool blocked = false; try { FreeModelRouter.Resolve(Catalog(paid, Model("qwen/excluded:free", 1)), ultra); } catch (InvalidOperationException ex) { blocked = ex.Message.Contains("No paid or local-model fallback"); }
        check(blocked, "routing fails explicitly when no compatible verified free endpoint remains");
        var unknownPrice = Model(ultra, 1); unknownPrice["pricing"] = new { prompt = "0", completion = (string?)null };
        blocked = false; try { FreeModelRouter.Resolve(Catalog(unknownPrice), ultra); } catch (InvalidOperationException) { blocked = true; }
        check(blocked, "missing or nonnumeric price information never counts as proof of a free endpoint");
    }
    private static Dictionary<string, object?> Model(string id, long created, string[]? modalities = null) => new()
    {
        ["id"] = id, ["name"] = id, ["created"] = created, ["context_length"] = 131072,
        ["pricing"] = new { prompt = "0", completion = "0" },
        ["architecture"] = new { input_modalities = modalities ?? ["text"], output_modalities = new[] { "text" } }
    };
}
