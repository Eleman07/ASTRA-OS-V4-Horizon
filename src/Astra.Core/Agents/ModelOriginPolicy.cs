namespace AstraOS.Core.Agents;

public sealed record ApprovedModelOrigin(string Publisher, string Developer, string DeveloperCountry,
    string BaseModel, string BaseDeveloper, string EvidenceUrl);

/// <summary>
/// Explicitly reviewed model identities, not a country guess from a filename or
/// a blanket publisher-prefix allowlist. Unknown releases and broker routers
/// fail closed until their publisher/base provenance has been reviewed.
/// Training examples and synthetic teachers are not base-model nationality.
/// Live pricing, account access and modality checks remain separate requirements.
/// </summary>
public static class ModelOriginPolicy
{
    private static readonly IReadOnlyDictionary<string, ApprovedModelOrigin> Approved = Build();
    private static readonly string[] ExcludedLineageMarkers =
        ["qwen", "deepseek", "glm", "kimi", "moonshot", "minimax", "baichuan", "internlm", "internvl", "hunyuan", "ernie", "doubao", "stepfun", "yi-", "yi_"];

    public static bool IsAllowedCloudModel(string? id) => TryGetCloudOrigin(id, out _);

    public static bool TryGetCloudOrigin(string? id, out ApprovedModelOrigin? origin)
    {
        origin = null;
        if (string.IsNullOrWhiteSpace(id) || id.Length > 256 || id != id.Trim()
            || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('/' or '-' or '_' or '.' or ':'))
            || id.Count(c => c == '/') != 1 || id.Contains("..", StringComparison.Ordinal)) return false;
        string key = id.ToLowerInvariant();
        if (ExcludedLineageMarkers.Any(marker => key.Contains(marker, StringComparison.Ordinal))) return false;
        if (key.EndsWith(":free", StringComparison.Ordinal)) key = key[..^5];
        if (key.Contains(':')) return false; // No unreviewed broker/routing variants.
        return Approved.TryGetValue(key, out origin);
    }

    private static IReadOnlyDictionary<string, ApprovedModelOrigin> Build()
    {
        var models = new Dictionary<string, ApprovedModelOrigin>(StringComparer.Ordinal);
        void Add(string publisher, string developer, string country, string baseModel, string baseDeveloper, string source, params string[] names)
        {
            var record = new ApprovedModelOrigin(publisher, developer, country, baseModel, baseDeveloper, source);
            foreach (string name in names) models.Add(publisher + "/" + name, record);
        }
        Add("openai", "OpenAI", "United States", "gpt-oss", "OpenAI",
            "https://huggingface.co/openai/gpt-oss-20b", "gpt-oss-20b", "gpt-oss-120b");
        Add("microsoft", "Microsoft Research", "United States", "Phi-4", "Microsoft Research",
            "https://huggingface.co/microsoft/Phi-4-reasoning-plus", "phi-4", "phi-4-reasoning", "phi-4-reasoning-plus");
        Add("mistralai", "Mistral AI", "France", "Ministral 3", "Mistral AI",
            "https://huggingface.co/mistralai/Ministral-3-14B-Reasoning-2512-GGUF",
            "ministral-3b-2512", "ministral-8b-2512", "ministral-14b-2512",
            "ministral-3-8b-instruct-2512", "ministral-3-14b-instruct-2512",
            "ministral-3-8b-reasoning-2512", "ministral-3-14b-reasoning-2512");
        Add("nvidia", "NVIDIA", "United States", "Nemotron 3 Ultra", "NVIDIA",
            "https://huggingface.co/nvidia/NVIDIA-Nemotron-3-Ultra-550B-A55B-BF16", "nemotron-3-ultra-550b-a55b");
        Add("nvidia", "NVIDIA", "United States", "Llama 3.1 405B Instruct", "Meta",
            "https://huggingface.co/nvidia/Llama-3_1-Nemotron-Ultra-253B-v1", "llama-3.1-nemotron-ultra-253b-v1");
        Add("nvidia", "NVIDIA", "United States", "Nemotron 3 Ultra distilled to Nemotron 3.5 Super VL", "NVIDIA",
            "https://developer.nvidia.com/blog/nvidia-nemotron-3-5-super-vl-powers-specialized-agents-with-multimodal-reasoning/",
            "nemotron-3.5-super-vl", "nemotron-3.5-super-vl-120b-a12b");
        Add("nvidia", "NVIDIA", "United States", "Nemotron 3 Nano + C-RADIO + Parakeet", "NVIDIA",
            "https://huggingface.co/nvidia/Nemotron-3-Nano-Omni-30B-A3B-Reasoning-BF16",
            "nemotron-3-nano-omni-30b-a3b-reasoning");
        Add("nvidia", "NVIDIA", "United States", "Nemotron-Hybrid", "NVIDIA",
            "https://huggingface.co/nvidia/NVIDIA-Nemotron-Nano-9B-v2", "nemotron-nano-9b-v2");
        Add("cohere", "Cohere / Cohere Labs", "Canada", "North Mini Code (cohere2_moe)", "Cohere",
            "https://huggingface.co/CohereLabs/North-Mini-Code-1.0", "north-mini-code", "north-mini-code-1.0");
        Add("thinking-machines", "Thinking Machines Lab", "United States", "Inkling (trained from scratch)", "Thinking Machines Lab",
            "https://thinkingmachines.ai/news/introducing-inkling/", "inkling", "inkling-small");
        Add("meta-llama", "Meta", "United States", "Llama 3", "Meta",
            "https://huggingface.co/meta-llama/Llama-3.3-70B-Instruct",
            "llama-3.1-8b-instruct", "llama-3.1-70b-instruct", "llama-3.1-405b-instruct", "llama-3.3-70b-instruct");
        Add("google", "Google", "United States", "Gemma 3", "Google",
            "https://huggingface.co/google/gemma-3-27b-it", "gemma-3-4b-it", "gemma-3-12b-it", "gemma-3-27b-it");
        Add("allenai", "Allen Institute for AI", "United States", "Olmo 3", "Allen Institute for AI",
            "https://huggingface.co/allenai/Olmo-3-7B-Think", "olmo-3-7b-think");
        Add("ibm-granite", "IBM", "United States", "Granite 4", "IBM",
            "https://huggingface.co/ibm-granite/granite-4.0-h-small", "granite-4.0-h-small");
        Add("ibm", "IBM", "United States", "Granite 4", "IBM",
            "https://huggingface.co/ibm-granite/granite-4.0-h-small", "granite-4.0-h-small");
        return models;
    }
}
