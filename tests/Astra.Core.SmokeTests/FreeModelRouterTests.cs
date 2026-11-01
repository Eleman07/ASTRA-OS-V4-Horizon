using System.Text.Json;
using AstraOS.Core.Agents;

internal static class FreeModelRouterTests
{
    public static void Run(Action<bool, string> check)
    {
        const string ultra = "nvidia/nemotron-3-ultra-550b-a55b:free";
        const string olderUltra = "nvidia/llama-3.1-nemotron-ultra-253b-v1:free";
        const string omni = "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
        const string north = "cohere/north-mini-code:free";
        const string inkling = "thinking-machines/inkling:free";
        string Catalog(params Dictionary<string, object?>[] models) => JsonSerializer.Serialize(new { data = models });
        var exact = Model(ultra, 100); exact["top_provider"] = new { context_length = 1000000, max_completion_tokens = 65536 };
        var route = FreeModelRouter.Resolve(Catalog(exact, Model(olderUltra, 200)), ultra);
        check(route.Id == ultra && route.ContextTokens == 1000000 && route.MaxOutputTokens == 65536, "free router honors an available preferred endpoint and its advertised context/output maximum");
        route = FreeModelRouter.Resolve(Catalog(Model(olderUltra, 200), Model(ultra, 300), Model(inkling, 400)), "nvidia/retired-nemotron-ultra:free");
        check(route.Id == ultra && route.Reason.Contains("same vendor/family"), "removed model routes to the newest already-reviewed free compatible family member");
        var paid = Model(ultra, 500); paid["pricing"] = new { prompt = "0.001", completion = "0" };
        route = FreeModelRouter.Resolve(Catalog(paid, Model(inkling, 100)), ultra);
        check(route.Id == inkling && route.Reason.Contains("no longer verified as zero-price"), "an exact model that starts charging is rejected before a free substitute is selected");
        route = FreeModelRouter.Resolve(Catalog(Model(ultra, 500), Model("nvidia/nemotron-3.5-super-vl:free", 100, ["text", "image"])), ultra, "image");
        check(route.Id.Contains("super-vl") && route.Reason.Contains("capability"), "text-only exact endpoints cannot satisfy image requests");
        var imagePrice = Model("nvidia/nemotron-3.5-super-vl:free", 500, ["text", "image"]); imagePrice["pricing"] = new { prompt = "0", completion = "0", image = "0.01" };
        route = FreeModelRouter.Resolve(Catalog(imagePrice, Model(omni, 100, ["text", "image", "audio"])), "nvidia/missing-super-vl:free", "image");
        check(route.Id == omni, "nonzero auxiliary image pricing disqualifies an otherwise free endpoint");
        var requestPrice = Model(ultra, 100); requestPrice["pricing"] = new { prompt = "0", completion = "0", request = "0.001" };
        route = FreeModelRouter.Resolve(Catalog(requestPrice, Model(inkling, 1)), ultra);
        check(route.Id == inkling, "nonzero per-request pricing cannot slip through free routing");
        var audioPrice = Model(omni, 200, ["text", "audio"]); audioPrice["pricing"] = new { prompt = "0", completion = "0", input_audio = "0.0001" };
        route = FreeModelRouter.Resolve(Catalog(audioPrice, Model(inkling, 100, ["audio", "text"])), omni, "audio");
        check(route.Id == inkling, "nonzero auxiliary audio pricing also prevents a paid route");
        route = FreeModelRouter.Resolve(Catalog(Model("qwen/very-new:free", 1000), Model("nvidia/nemotron-safety-classifier:free", 1100), Model(inkling, 1)), "qwen/very-new:free");
        check(route.Id == inkling, "Qwen endpoints and safety classifiers are excluded even when preferred or newer");
        route = FreeModelRouter.Resolve(Catalog(Model(north, 30), Model(inkling, 100)), "cohere/retired-coder:free");
        check(route.Id == north, "Cohere coding preferences favor the reviewed free North Mini family when the original endpoint disappears");
        route = FreeModelRouter.Resolve(Catalog(Model(inkling, 1)), ultra);
        check(route.MaxOutputTokens == 0 && route.Reason.Contains("equivalent quality is not assumed"), "an unknown provider output maximum remains unspecified and substitution does not promise equal power");
        bool blocked = false; try { FreeModelRouter.Resolve(Catalog(paid, Model("qwen/excluded:free", 1)), ultra); } catch (InvalidOperationException ex) { blocked = ex.Message.Contains("No paid or local-model fallback"); }
        check(blocked, "routing fails explicitly when no compatible verified free endpoint remains");
        var unknownPrice = Model(ultra, 1); unknownPrice["pricing"] = new { prompt = "0", completion = (string?)null };
        blocked = false; try { FreeModelRouter.Resolve(Catalog(unknownPrice), ultra); } catch (InvalidOperationException) { blocked = true; }
        check(blocked, "missing or nonnumeric price information never counts as proof of a free endpoint");
        route = FreeModelRouter.Resolve(Catalog(exact, Model(inkling, 1)), ultra, unavailable: new HashSet<string> { ultra });
        check(route.Id == inkling && route.Reason.Contains("could not serve"), "temporarily unavailable models are excluded before automatic free replacement");
        check(ModelOriginPolicy.IsAllowedCloudModel(ultra) && ModelOriginPolicy.IsAllowedCloudModel(north)
            && ModelOriginPolicy.TryGetCloudOrigin("nvidia/nemotron-3.5-super-vl:free", out var origin)
            && origin!.BaseDeveloper == "NVIDIA" && origin.EvidenceUrl.StartsWith("https://developer.nvidia.com/"),
            "cloud origin records retain reviewed publisher/base provenance and a primary source");
        check(new[] { "openai/gpt-oss-20b:free", "microsoft/phi-4-reasoning-plus:free", "mistralai/ministral-3-14b-reasoning-2512:free",
                "meta-llama/llama-3.3-70b-instruct:free", "google/gemma-3-27b-it:free", "allenai/olmo-3-7b-think:free", "ibm-granite/granite-4.0-h-small:free" }
            .All(ModelOriginPolicy.IsAllowedCloudModel), "reviewed US, French and Canadian developer families are explicitly eligible without guessing unknown vendors");
        check(new[] { "qwen/qwen3:free", "deepseek/deepseek-r1:free", "z-ai/glm-5:free", "moonshotai/kimi-k2:free",
                "nvidia/qwen3.5-nvfp4:free", "microsoft/phi-deepseek-distill:free", "meta-llama/llama-qwen-distill:free",
                "nvidia/nemotron-nano-12b-v2-vl:free", "new-vendor/powerful:free", "nvidia/unknown-new-release:free", "openrouter/free", "cohere/audio-test:free" }
            .All(id => !ModelOriginPolicy.IsAllowedCloudModel(id)), "Chinese bases/distills, unreviewed releases, unknown publishers and uncontrolled broker routers fail closed");
        check(!ModelOriginPolicy.IsAllowedCloudModel("openai.evil/gpt-oss-20b:free")
            && !ModelOriginPolicy.IsAllowedCloudModel("nvidia/nemotron-3-ultra-550b-a55b:free/extra")
            && !ModelOriginPolicy.IsAllowedCloudModel("openai/gpt-oss-20b:nitro")
            && !ModelOriginPolicy.IsAllowedCloudModel(" openai/gpt-oss-20b:free"),
            "origin policy rejects publisher spoofing, additional path components and unreviewed routing variants");
        route = FreeModelRouter.Resolve(Catalog(Model("openrouter/free", 1000), Model("nvidia/nemotron-5-ultra:free", 900), Model("nvidia/qwen3:free", 800), Model(north, 1)), "openrouter/free");
        check(route.Id == north, "origin checks apply to every fallback and never trust a future model solely because of its publisher");
        FreeCloudCompletionTests.Run(check);
    }
    private static Dictionary<string, object?> Model(string id, long created, string[]? modalities = null) => new()
    {
        ["id"] = id, ["name"] = id, ["created"] = created, ["context_length"] = 131072,
        ["pricing"] = new { prompt = "0", completion = "0" },
        ["architecture"] = new { input_modalities = modalities ?? ["text"], output_modalities = new[] { "text" } }
    };
}
