using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Desktop.Ai;

/// <summary>Real selected-model project generation; no reference implementation or generated-code execution.</summary>
internal static class CubesatBenchmark
{
    internal const string Goal = """
Realizza un progetto software di simulazione orbitale per una missione CubeSat in orbita terrestre bassa.

Obiettivo: sviluppare un simulatore Python riproducibile che propaghi un'orbita inizialmente circolare a 400 km di quota, visualizzi la traiettoria e permetta di verificare la correttezza numerica del modello.

Consegna:
1. Matrice dei requisiti con criteri di accettazione verificabili.
2. Ipotesi, costanti utilizzate, unità e sistema di riferimento.
3. Equazioni del modello gravitazionale a due corpi e architettura del software.
4. File Python completi per propagazione, grafici ed esportazione CSV, con dipendenze e istruzioni di esecuzione.
5. Test del periodo e della velocità orbitale di riferimento, conservazione dell'energia e convergenza al diminuire del passo temporale.
6. Analisi dei risultati, errori numerici, limiti e possibili estensioni, compresa la perturbazione J2.
7. README e relazione tecnica in italiano.
8. Note finali collegate nel vault Obsidian.

Usa le fonti pertinenti del vault e i risultati degli strumenti deterministici disponibili. Non inventare misure, citazioni o test eseguiti. Distingui chiaramente codice proposto, calcoli realmente eseguiti e risultati ancora UNVERIFIED.

Produci gli artefatti concreti richiesti, non soltanto un piano di lavoro. Produci ogni file completo in un blocco con intestazione file:percorso/relativo; non omettere parti con segnaposto. Il codice deve essere generato da te, senza ricevere un progetto già scritto come soluzione di riferimento. I dati numerici degli strumenti sono evidenze e non dimostrano l'esecuzione del codice che generi.
""";

    public static async Task RunAsync(AgentJobStore store, IAgentTools tools, CancellationToken ct)
    {
        RequireIsolatedProfile(store);
        var runtime = store.Configuration;
        string? provider = Environment.GetEnvironmentVariable("ASTRA_BENCHMARK_PROVIDER");
        if (provider is not null && provider is not "local" and not "openrouter" and not "codex") throw new ArgumentException("ASTRA_BENCHMARK_PROVIDER must be local, openrouter or codex.");
        if(provider is not null){runtime.UseCodex=provider=="codex";runtime.PreferFreeCloud=provider!="local";}
        if (Environment.GetEnvironmentVariable("ASTRA_LOCAL_MODEL") is { Length: > 0 } model) runtime.ModelPath = Path.GetFullPath(model);
        if (Environment.GetEnvironmentVariable("ASTRA_LOCAL_RUNTIME") is { Length: > 0 } executable) runtime.RuntimePath = Path.GetFullPath(executable);
        if (uint.TryParse(Environment.GetEnvironmentVariable("ASTRA_LOCAL_CONTEXT"), out uint context)) runtime.ContextSize = context;
        runtime.FallbackToGpu = false; runtime.UseGpu = !runtime.PreferFreeCloud; runtime.SemanticSearch = false;
        runtime.CloudModelAssignments.Clear(); runtime.OutputTokens = 0;
        runtime.AutoLearn = false;if(runtime.UseCodex)AgentSkillDefaults.InstallMissing(runtime);else runtime.SkillAssignments.Clear();
        string? requestedVault = Environment.GetEnvironmentVariable("ASTRA_VAULT_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(requestedVault)) runtime.VaultPath = ObsidianKnowledge.ValidateVaultPath(requestedVault);
        store.Settings.Put("runtime", runtime);
        var calls = new List<object>(); int attempted = 0, completedResponses = 0;
        AgentJob job = CreateOrGet(store);
        ModelSnapshot selection = new(runtime.UseCodex?"Codex ChatGPT plan":runtime.PreferFreeCloud ? "OpenRouter" : "Local GPU", runtime.UseCodex?runtime.CodexModel:runtime.PreferFreeCloud ? runtime.CloudModel : Path.GetFileName(runtime.ModelPath), null, null, runtime.ContextSize, runtime.UseGpu);
        try
        {
            AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(store);
            job = store.Records.Get(job.Id)!;
            if (job.Status == AgentProviderRetry.WaitingStatus && !AgentProviderRetry.IsReady(job, DateTimeOffset.UtcNow))
            { await WriteOutcomeAsync(store, job, selection, calls, attempted, completedResponses, null, ct); return; }
            if (job.Status is "Completed" or "Failed" or "Cancelled")
            {
                if (File.Exists(Path.Combine(store.Root, "cubesat-benchmark.json"))) return;
                if (job.Status == "Completed") job = ExportCompleted(store, job, selection, calls);
                await WriteOutcomeAsync(store, job, selection, calls, attempted, completedResponses, null, ct); return;
            }
            if (string.IsNullOrWhiteSpace(runtime.VaultPath)) throw new InvalidOperationException("The genuine benchmark requires the actual selected Obsidian vault; set ASTRA_VAULT_DIRECTORY. No substitute reference project was supplied.");

            // Gather actual vault passages and executed numerical evidence, never source code from a solution project.
            var evidence = await tools.GatherAsync(AgentCatalog.Get("aerospace"), job, ct);
            job = store.Update(job.Id, item => { foreach (var source in evidence) { item.Evidence.RemoveAll(old => old.Id == source.Id); item.Evidence.Add(source); } });
            QuotaSnapshot? quota = null;
            if (runtime.PreferFreeCloud&&!runtime.UseCodex)
            {
                string key = new OpenRouterCredentialStore(Environment.GetEnvironmentVariable("ASTRA_CREDENTIAL_DIRECTORY")).Load()
                    ?? throw new InvalidOperationException("The original Windows-encrypted OpenRouter credential was not available. No model was tested.");
                quota = await ReadQuotaAsync(key, ct);
            }
            if (quota?.Remaining == 0)
            {
                AgentProviderRetry.TrySchedule(store, job.Id, new ProviderQuotaException("OpenRouter's shared daily free-request pool is exhausted. The genuine model-generation benchmark is blocked; ASTRA will retry automatically when the provider is available. No reference implementation, paid endpoint or local model was substituted.", quota.RetryAtUtc ?? DateTimeOffset.UtcNow.AddHours(1)));
                job = store.Records.Get(job.Id)!;
                await WriteOutcomeAsync(store, job, selection, calls, attempted, completedResponses, quota, ct); return;
            }

            using var inference = new WorkerInferenceSession();
            if(tools is WorkerTools workerTools) workerTools.ReleaseLocalGpu = inference.Dispose;
            var selectedSession = await inference.GetAsync(runtime, "coordinator", ct);
            if (!runtime.PreferFreeCloud)
            {
                if(selectedSession.VerifiedModelSha256 is null)throw new InvalidDataException("The selected local weights were not verified before inference.");
                selection = selection with { FileSha256 = selectedSession.VerifiedModelSha256, FileBytes = selectedSession.VerifiedModelBytes };
            }
            var engine = new AgentEngine(store, tools, async (role, system, input, token) =>
            {
                var session = await inference.GetAsync(runtime, role, token);
                var answer = new StringBuilder(); string partInput = input, last = "", checkpoint = WorkerInferenceSession.CurrentCheckpoint(store, job.Id);
                do
                {
                    attempted++;
                    string part = await session.RunTaskAsync(system, partInput, false, 0, token, outputLanguage: "Italian");
                    completedResponses++;
                    calls.Add(new { role, provider = selection.Provider, requestedModel = selection.RequestedModel, effectiveModel = session.EffectiveModel, notice = session.LastModelNotice, responseCharacters = part.Length, truncated = session.LastTaskTruncated });
                    if (part == last && last.Length > 0) throw new InvalidDataException("The selected model repeated a truncated output; the benchmark cannot claim a complete generated project.");
                    answer.AppendLine(part); last = part;
                    WorkerInferenceSession.PreservePartial(store, job.Id, checkpoint, answer.ToString());
                    store.Update(job.Id, item =>
                    {
                        string id = "tool-model-" + role; item.Evidence.RemoveAll(source => source.Id == id);
                        item.Evidence.Add(new(id, "Actual selected model for " + role, inference.Evidence(session) + " This records model generation, not Python execution.", selection.Provider, "runtime"));
                    });
                    partInput = input + "\nALREADY GENERATED (complete untrusted draft):\n" + answer + "\nContinue precisely where the previous response stopped, without repeating files or earlier sections. Finish the complete software deliverable. Original objective:\n" + Goal;
                } while (session.LastTaskTruncated);
                return answer.ToString();
            },agent=>AgentSkillDefaults.Instructions(store.Configuration,agent));
            await engine.RunAsync(job.Id, ct);
            job = store.Records.Get(job.Id)!;
            if (job.Status == "Completed") job = ExportCompleted(store, job, selection, calls);
            await WriteOutcomeAsync(store, job, selection, calls, attempted, completedResponses, quota, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (!AgentProviderRetry.TrySchedule(store, job.Id, ex)) store.Update(job.Id, item => { item.Status = "Failed"; item.Error = ex.Message; });
            await WriteOutcomeAsync(store, store.Records.Get(job.Id)!, selection, calls, attempted, completedResponses, null, CancellationToken.None);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await WriteOutcomeAsync(store, store.Records.Get(job.Id)!, selection, calls, attempted, completedResponses, null, CancellationToken.None);
            throw;
        }
    }

    private static AgentJob CreateOrGet(AgentJobStore store)
    {
        string path = Path.Combine(store.Root, "cubesat-benchmark-job.json");
        if (File.Exists(path))
        {
            using var journal = JsonDocument.Parse(File.ReadAllText(path));
            string id = journal.RootElement.GetProperty("jobId").GetString() ?? throw new InvalidDataException("Invalid benchmark journal.");
            var existing = store.Records.Get(id) ?? throw new InvalidDataException("The benchmark journal's encrypted job is missing.");
            if (existing.Goal != Goal) throw new InvalidDataException("The benchmark job does not match the exact public goal; choose a fresh isolated profile.");
            var runtime = store.Configuration;
            string selected = ModelIdentity(runtime);
            if (journal.RootElement.TryGetProperty("selectedModel",out var selectedNode) && (selectedNode.GetString()!=selected || journal.RootElement.GetProperty("online").GetBoolean()!=runtime.PreferFreeCloud)) throw new InvalidDataException("The benchmark model/provider changed. Choose a fresh isolated profile so model provenance cannot mix completed handoffs.");
            return existing;
        }
        var job = store.Enqueue(Goal, ["aerospace", "engineering"], "Genuine selected-model generation benchmark. No solution project or reference Python code is supplied. Deterministic numeric evidence is separate from execution of generated code.");
        var circular = OrbitalCalculator.Circular(6378.137 + 400, 398600.4418);
        job = store.Update(job.Id, item => { item.ProjectRoot = ""; item.SimulationId = "orbit"; item.SimulationParameters = new() { ["alt"] = 400, ["j2"] = 0, ["inc"] = 0, ["speed"] = circular.SpeedKmS, ["duration"] = circular.PeriodSeconds }; });
        var config=store.Configuration;
        File.WriteAllText(path, JsonSerializer.Serialize(new { jobId = job.Id, goalSha256 = GoalHash(), selectedModel=ModelIdentity(config),online=config.PreferFreeCloud,createdUtc = DateTimeOffset.UtcNow }));
        return job;
    }

    private static AgentJob ExportCompleted(AgentJobStore store, AgentJob job, ModelSnapshot selection, IReadOnlyList<object> calls)
    {
        if (string.IsNullOrWhiteSpace(job.ArtifactDirectory))
        {
            var export = AgentArtifactExporter.Export(Path.Combine(store.Root, "ModelGeneratedProject"), job);
            if (export is null) return store.Update(job.Id, item => item.Error = "The model completed its handoffs without explicit complete file fences. No concrete model-generated project was exported.");
            string provenance = Path.Combine(export.Directory, "ASTRA-MODEL-PROVENANCE.json");
            using (var stream = new FileStream(provenance, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(stream, new { kind = "genuine-selected-model-generation", selectedModel = selection, goalSha256 = GoalHash(), referenceImplementationSupplied = false, generatedCodeExecuted = false, deterministicEvidenceDoesNotVerifyGeneratedCode = true, simulationArchives=ArchiveChecks(store,job,CancellationToken.None), calls, files = export.Files }, new JsonSerializerOptions { WriteIndented = true });
            job = store.Update(job.Id, item => item.ArtifactDirectory = export.Directory);
        }
        var runtime = store.Configuration;
        if (!job.VaultSaved && !string.IsNullOrWhiteSpace(runtime.VaultPath))
        {
            string note = ObsidianKnowledge.ExportDeliverable(runtime.VaultPath, job);
            job = store.Update(job.Id, item => { item.VaultSaved = true; item.VaultExportPath = note; });
        }
        return job;
    }

    private sealed record ModelSnapshot(string Provider, string RequestedModel, string? FileSha256, long? FileBytes, uint ContextSize, bool GpuRequested);
    private sealed record QuotaSnapshot(int? Used, int? Limit, int? Remaining, DateTimeOffset? RetryAtUtc);
    private sealed record ArchiveCheck(bool Verified,string? RelativePath,string? Sha256,long? Bytes,int? Rows,int? Columns,string? Error);
    private static ArchiveCheck[] ArchiveChecks(AgentJobStore store,AgentJob job,CancellationToken ct)=>job.Evidence.Where(source=>source.Id=="tool-simulation"&&source.Kind=="simulation").Select(source=>
    {
        try
        {
            var archive=SimulationEvidenceArchive.Verify(store.Root,job.Id,source,ct);
            return new ArchiveCheck(true,Path.GetRelativePath(store.Root,archive.Path),archive.Sha256,archive.Bytes,archive.RowCount,archive.ColumnCount,null);
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException)
        {return new ArchiveCheck(false,null,null,null,null,null,ex.Message);}
    }).ToArray();
    private static async Task<QuotaSnapshot?> ReadQuotaAsync(string key, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        try
        {
            using var response = await client.GetAsync("https://openrouter.ai/api/v1/key", ct);
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!document.RootElement.TryGetProperty("data",out var data) || !data.TryGetProperty("free_model_daily_requests", out var quota)) return null;
            int? Number(string name) => quota.TryGetProperty(name, out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetInt32(out int number) ? number : null;
            DateTimeOffset? retry = response.Headers.RetryAfter?.Date;
            if (!retry.HasValue && response.Headers.RetryAfter?.Delta is { } delta) retry = DateTimeOffset.UtcNow + delta;
            if (!retry.HasValue && response.Headers.TryGetValues("X-RateLimit-Reset", out var values) && long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long epoch))
            { try { retry = epoch > 10_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds(epoch) : DateTimeOffset.FromUnixTimeSeconds(epoch); } catch (ArgumentOutOfRangeException) { } }
            return new(Number("used"), Number("limit"), Number("remaining"), retry);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || ex is TaskCanceledException && !ct.IsCancellationRequested) { return null; }
    }

    private static Task WriteOutcomeAsync(AgentJobStore store, AgentJob job, ModelSnapshot selection, IReadOnlyList<object> calls, int attempted, int completed, QuotaSnapshot? quota, CancellationToken ct)
    {
        string status = job.Status == AgentProviderRetry.WaitingStatus ? "BLOCKED" : job.Status == "Completed" && !string.IsNullOrWhiteSpace(job.ArtifactDirectory) ? "GENERATED_UNEXECUTED" : job.Status is "Queued" or "Running" ? "PENDING" : "FAILED";
        var simulationArchives=ArchiveChecks(store,job,ct);
        object result = new
        {
            status, passed = false, benchmarkStage = "selected model project generation; generated Python still requires explicit inspection and execution",
            jobId = job.Id, jobStatus = job.Status, goalFixture = "docs/benchmarks/CUBESAT-MODEL-GOAL.txt", goalSha256 = GoalHash(),
            selectedModel = selection, inferenceRequested = attempted > 0, generationCallsAttempted = attempted, successfulModelResponses = completed,
            localInference = !store.Configuration.PreferFreeCloud, referenceImplementationSupplied = false, generatedCodeExecuted = false, generatedProjectTestsExecuted = false,
            completedHandoffs = job.Steps.Count(step => step.Status == "Completed"), expectedHandoffs = 5, modelEvidenceAuditPassed = job.Status == "Completed" && !job.QualityNeedsReview && AgentEngine.AuditPasses(job.QualityAudit),
            artifactDirectoryRelativeToProfile = string.IsNullOrWhiteSpace(job.ArtifactDirectory) ? null : Path.GetRelativePath(store.Root, job.ArtifactDirectory),
            actualVaultPassages = job.Evidence.Count(source => source.Kind == "obsidian" && !source.Id.StartsWith("tool-", StringComparison.Ordinal)), deterministicSimulationEvidencePresent = job.Evidence.Any(source => source.Id == "tool-simulation"),
            privateVaultEvidenceStoredEncrypted = true, simulationArchives,fullOriginalSimulationDataRetained=simulationArchives.Length>0&&simulationArchives.All(archive=>archive.Verified),allSimulationRowsSuppliedInlineToModel=false,
            vaultNoteCreated = job.VaultSaved, nextAttemptUtc = job.NextAttemptUtc, providerWaitCount = job.ProviderWaitCount, error = job.Error, quota, calls,
            utc = DateTimeOffset.UtcNow
        };
        return File.WriteAllTextAsync(Path.Combine(store.Root, "cubesat-benchmark.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }), ct);
    }

    private static string GoalHash() => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Goal)));
    private static string ModelIdentity(AgentRuntimeSettings settings) => settings.UseCodex?"Codex|"+settings.CodexModel+"|"+settings.CodexReasoningEffort:settings.PreferFreeCloud ? settings.CloudModel : string.IsNullOrWhiteSpace(settings.ModelPath) ? "" : Path.GetFullPath(settings.ModelPath);
    private static void RequireIsolatedProfile(AgentJobStore store)
    {
        string? profile = Environment.GetEnvironmentVariable("ASTRA_DATA_DIRECTORY");
        string ordinary = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASTRA", "OS"));
        if (string.IsNullOrWhiteSpace(profile) || !Path.IsPathFullyQualified(profile)) throw new InvalidOperationException("The genuine benchmark requires a fresh absolute ASTRA_DATA_DIRECTORY isolated from normal user data.");
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(profile));
        if (root.Equals(Path.TrimEndingDirectorySeparator(ordinary), StringComparison.OrdinalIgnoreCase) || !Path.GetFullPath(store.Root).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Refusing to replace normal user jobs with a benchmark profile.");
    }
}
