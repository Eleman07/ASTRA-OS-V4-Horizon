using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;

internal static class PreferenceProviderTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        var store = new PreferenceStore(Path.Combine(root, "provider-persistence"));
        var fresh = store.Load();
        check(fresh.AiProvider == "local" && fresh.AiModelPath == "" && fresh.AiExecutablePath == "",
            "new preferences default to local with no implicit bundled model or executable");
        var runtimeDefaults = new AgentRuntimeSettings();
        check(runtimeDefaults.UseGpu && !runtimeDefaults.PreferFreeCloud && fresh.AiProvider == "local",
            "global local preference agrees with existing GPU-first agent runtime defaults");

        string selectedModel = Path.Combine(root, "user-selected models", "Ministral-3-14B-Reasoning-2512-Q4_K_M.gguf");
        string selectedRuntime = Path.Combine(root, "user-selected runtime", "llama-server.exe");
        store.Save(new AstraPreferences { AiProvider = "local", AiModelPath = selectedModel, AiExecutablePath = selectedRuntime });
        var loaded = new PreferenceStore(store.DirectoryPath).Load();
        check(loaded.AiProvider == "local" && loaded.AiModelPath == selectedModel && loaded.AiExecutablePath == selectedRuntime,
            "save and process-like reload retain explicit local provider and exact user-selected paths without model substitution");
        loaded.SelectedProfiles = ["core", "stem"];
        loaded.FocusMode = true;
        store.Save(loaded);
        loaded = store.Load();
        check(loaded.AiProvider == "local" && loaded.AiModelPath == selectedModel && loaded.AiExecutablePath == selectedRuntime,
            "saving unrelated preferences cannot switch local inference online or replace its selected model");
        using (var persisted = JsonDocument.Parse(File.ReadAllText(store.FilePath)))
            check(persisted.RootElement.GetProperty("aiProvider").GetString() == "local",
                "the persisted provider itself remains local rather than only the in-memory UI value");

        const string selectedCloud = "cohere/north-mini-code:free";
        store.Save(new AstraPreferences { AiProvider = "openrouter", OpenRouterModel = selectedCloud,
            AiModelPath = selectedModel, AiExecutablePath = selectedRuntime });
        loaded = store.Load();
        check(loaded.AiProvider == "openrouter" && loaded.OpenRouterModel == selectedCloud
            && loaded.AiModelPath == selectedModel && loaded.AiExecutablePath == selectedRuntime,
            "explicit optional online mode and its model survive reload while retaining the user's separate local selection");

        foreach (string? provider in new[] { "unknown", "", "cloud", "openrouter/invalid", null })
        {
            store.Save(new AstraPreferences { AiProvider = provider!, AiModelPath = selectedModel });
            loaded = store.Load();
            check(loaded.AiProvider == "local" && loaded.AiModelPath == selectedModel,
                "invalid provider falls back to local without replacing the selected weights: " + (provider ?? "null"));
        }
        store.Save(new AstraPreferences { AiProvider = " LOCAL " });
        check(store.Load().AiProvider == "local" && store.Load().AiModelPath == "",
            "provider canonicalization preserves local and never fills an unselected model path");
        store.Save(new AstraPreferences { AiProvider = "OPENROUTER", OpenRouterModel = selectedCloud });
        check(store.Load().AiProvider == "openrouter" && store.Load().OpenRouterModel == selectedCloud,
            "explicit online provider casing normalizes without changing the selected endpoint");
        store.Save(new AstraPreferences{AiProvider="codex"});
        check(new PreferenceStore(store.DirectoryPath).Load().AiProvider=="codex","Codex ChatGPT provider persists without reverting to a GPU or OpenRouter provider");

        File.WriteAllText(store.FilePath, "{}");
        loaded = store.Load();
        check(loaded.AiProvider == "local" && loaded.AiModelPath == "" && loaded.AiExecutablePath == "",
            "legacy preferences missing provider and paths default to local without a bundled-model autofill");
        File.WriteAllText(store.FilePath, JsonSerializer.Serialize(new Dictionary<string, object?>
            { ["aiProvider"] = null, ["AiModelPath"] = selectedModel, ["AiExecutablePath"] = selectedRuntime }));
        loaded = store.Load();
        check(loaded.AiProvider == "local" && loaded.AiModelPath == selectedModel && loaded.AiExecutablePath == selectedRuntime,
            "null provider in persisted JSON recovers locally while preserving valid paths");
        File.WriteAllText(store.FilePath, "{broken");
        check(store.Load().AiProvider == "local", "malformed persisted preferences recover with local default");
    }
}
