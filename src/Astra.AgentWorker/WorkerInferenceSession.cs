using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Desktop.Ai;

/// <summary>One explicitly selected provider; local inference never sends a request to a hosted model.</summary>
internal sealed class WorkerInferenceSession : IDisposable
{
    private LocalLlmSession? _session;
    private string _identity = "";
    public string RequestedModel { get; private set; } = "";
    public string Provider { get; private set; } = "";

    public async Task<LocalLlmSession> GetAsync(AgentRuntimeSettings settings, string role, CancellationToken ct)
    {
        string model = settings.UseCodex?settings.CodexModel:settings.PreferFreeCloud
            ? settings.CloudModelAssignments.TryGetValue(role, out string? assigned) && !string.IsNullOrWhiteSpace(assigned) ? assigned : settings.CloudModel
            : settings.ModelPath;
        string executable = string.IsNullOrWhiteSpace(settings.RuntimePath) ? GpuRuntime.Executable : settings.RuntimePath;
        string identity = settings.UseCodex?$"Codex|{settings.CodexExecutablePath}|{model}|{settings.CodexReasoningEffort}":settings.PreferFreeCloud ? "OpenRouter|" + model : $"LocalGpu|{Path.GetFullPath(executable)}|{model}|{settings.ContextSize}|{settings.Threads}|{settings.UseGpu}";
        if (_session is { IsRunning: true } && _identity == identity) return _session;
        Dispose();
        var session = new LocalLlmSession();
        try
        {
            if(settings.UseCodex)
            {
                session.StartCodex(settings.CodexExecutablePath,model,settings.CodexReasoningEffort);
                Provider="Codex · ChatGPT plan";RequestedModel=model;
            }
            else if (settings.PreferFreeCloud)
            {
                string key = new OpenRouterCredentialStore(Environment.GetEnvironmentVariable("ASTRA_CREDENTIAL_DIRECTORY")).Load()
                    ?? throw new InvalidOperationException("Save a free OpenRouter key before explicitly selecting the online provider.");
                session.StartOpenRouter(key, model);
                Provider = "OpenRouter"; RequestedModel = model;
            }
            else
            {
                if (!settings.UseGpu) throw new InvalidOperationException("Local ASTRA agents require GPU inference. Enable Use GPU in model settings; no CPU or cloud fallback was started.");
                if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException("Select a verified local GGUF model in ASTRA settings before starting agents. No model was automatically downloaded or substituted.");
                if (settings.ContextSize == 0) throw new InvalidOperationException("Choose a positive local model context size appropriate for your GPU memory.");
                if (Path.GetFullPath(executable).Equals(Path.GetFullPath(BundledAi.RuntimePath), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Select the GPU llama-server runtime; the embedded CPU runtime is not permitted for local agents.");
                if (Path.GetFullPath(executable).Equals(Path.GetFullPath(GpuRuntime.Executable), StringComparison.OrdinalIgnoreCase)) await GpuRuntime.VerifyAsync(ct);
                await session.StartAsync(executable, model, true, ct, settings.ContextSize, settings.Threads);
                Provider = "Local GPU"; RequestedModel = Path.GetFileName(model);
            }
            _session = session; _identity = identity;
            return session;
        }
        catch { session.Dispose(); throw; }
    }

    public string Evidence(LocalLlmSession session) => session.IsCodex?"Official Codex CLI; ChatGPT plan sign-in. Requested model "+RequestedModel+". "+session.LastModelNotice+" Generated text is not proof of executing the proposed project.":session.IsOpenRouter
        ? "Explicit online provider. Requested " + RequestedModel + "; effective " + session.EffectiveModel + ". " + session.LastModelNotice + " Zero-priced routes only; provider quotas and context limits apply. No local fallback."
        : "Explicit local GPU provider. Selected GGUF " + RequestedModel + ". Hosted inference requests: none. GPU memory and model context are finite; complete source material is passed without ASTRA clipping. Runtime diagnostics:\n" + session.RuntimeDiagnostics;

    public static string CurrentCheckpoint(AgentJobStore store, string jobId) => store.Records.Get(jobId)?.Steps.FirstOrDefault(step => step.Status == "Working")?.Output ?? "";
    public static void PreservePartial(AgentJobStore store, string jobId, string prefix, string output)
    {
        store.Update(jobId, job =>
        {
            var step = job.Steps.FirstOrDefault(item => item.Status == "Working");
            if (step is not null) step.Output = (prefix.Length == 0 ? "" : prefix + "\n\n--- Additional uncompleted inference ---\n") + output;
        });
    }

    public void Dispose() { _session?.Dispose(); _session = null; _identity = ""; }
}
