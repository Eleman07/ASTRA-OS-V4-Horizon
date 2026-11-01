using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;

var store = new AgentJobStore();
Directory.CreateDirectory(store.Root);
if(args.Contains("--quota-status")){await CloudProbe.QuotaAsync(store,CancellationToken.None);return;}
if(args.Contains("--configure-codex"))
{
    var runtime=store.Configuration;runtime.UseCodex=true;runtime.PreferFreeCloud=true;runtime.UseGpu=false;runtime.FallbackToGpu=false;runtime.OutputTokens=0;runtime.SemanticSearch=false;
    string? requestedCodex=Environment.GetEnvironmentVariable("ASTRA_CODEX_EXECUTABLE");_ = CodexBridge.FindExecutable(requestedCodex);runtime.CodexExecutablePath=requestedCodex??"";
    if(Environment.GetEnvironmentVariable("ASTRA_CODEX_MODEL") is{Length:>0} selected)runtime.CodexModel=selected;
    if(Environment.GetEnvironmentVariable("ASTRA_CODEX_EFFORT") is{Length:>0} effort)runtime.CodexReasoningEffort=effort;
    if(Environment.GetEnvironmentVariable("ASTRA_VAULT_DIRECTORY") is{Length:>0} vault)runtime.VaultPath=ObsidianKnowledge.ValidateVaultPath(vault);
    AgentSkillDefaults.InstallMissing(runtime);store.Settings.Put("runtime",runtime);var preferences=new PreferenceStore();var saved=preferences.Load();saved.AiProvider="codex";preferences.Save(saved);return;
}
if(args.Contains("--codex-probe"))
{
    var runtime=store.Configuration;using var session=new LocalLlmSession();session.StartCodex(runtime.CodexExecutablePath,runtime.CodexModel,runtime.CodexReasoningEffort);
    try{string answer=await session.RunTaskAsync("Preserve supplied facts exactly. Return only the code and the source ID.","Source [note-calibration]: the ASTRA calibration code is 95731.",ct:CancellationToken.None);File.WriteAllText(Path.Combine(store.Root,"codex-probe.json"),JsonSerializer.Serialize(new{passed=answer.Contains("95731")&&answer.Contains("note-calibration"),model=session.EffectiveModel,answer,provider="Codex ChatGPT plan",localInference=false}));}
    catch(Exception ex){File.WriteAllText(Path.Combine(store.Root,"codex-probe.json"),JsonSerializer.Serialize(new{passed=false,error=ex.Message}));}return;
}
if(args.Contains("--configure-local"))
{
    var runtime=store.Configuration;
    string model=Environment.GetEnvironmentVariable("ASTRA_LOCAL_MODEL")??runtime.ModelPath;
    var verified=await LocalModelCatalog.VerifyTextModelAsync(model,CancellationToken.None);
    string executable=Environment.GetEnvironmentVariable("ASTRA_LOCAL_RUNTIME")??runtime.RuntimePath;
    if(string.IsNullOrWhiteSpace(executable))executable=GpuRuntime.Executable;
    executable=Path.GetFullPath(executable);
    SafeFileOperations.NoLinks(executable);
    if(!File.Exists(executable)||!Path.GetFileName(executable).Equals("llama-server.exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Select an existing GPU llama-server.exe runtime.");
    uint context=Math.Min(32768u,verified.NativeContext);
    if(Environment.GetEnvironmentVariable("ASTRA_LOCAL_CONTEXT") is{Length:>0} requested&&(!uint.TryParse(requested,out context)||context==0||context>verified.NativeContext))throw new ArgumentException("Choose a positive context supported by the selected model.");
    string? vault=Environment.GetEnvironmentVariable("ASTRA_VAULT_DIRECTORY");
    if(!string.IsNullOrWhiteSpace(vault))runtime.VaultPath=ObsidianKnowledge.ValidateVaultPath(vault);
    runtime.UseCodex=false;runtime.PreferFreeCloud=false;runtime.UseGpu=true;runtime.FallbackToGpu=false;runtime.SemanticSearch=false;
    runtime.ModelPath=Path.GetFullPath(model);runtime.SelectedModelId=verified.Id;runtime.RuntimePath=executable;
    runtime.ContextSize=context;runtime.OutputTokens=0;runtime.ExportToVault=!string.IsNullOrWhiteSpace(runtime.VaultPath);
    store.Settings.Put("runtime",runtime);
    var preferences=new PreferenceStore();var settings=preferences.Load();settings.AiProvider="local";settings.AiExecutablePath=runtime.RuntimePath;settings.AiModelPath=runtime.ModelPath;preferences.Save(settings);
    return;
}
if(args.Contains("--configure-cloud"))
{
    var runtime=store.Configuration;runtime.UseCodex=false;runtime.PreferFreeCloud=true;runtime.FallbackToGpu=false;runtime.SemanticSearch=false;runtime.UseGpu=false;runtime.CloudModel="nvidia/nemotron-3-ultra-550b-a55b:free";runtime.ContextSize=1_000_000;runtime.OutputTokens=0;
    runtime.CloudModelAssignments["engineering"]="cohere/north-mini-code:free";
    string? vault=Environment.GetEnvironmentVariable("ASTRA_VAULT_DIRECTORY");if(!string.IsNullOrWhiteSpace(vault)){runtime.VaultPath=Path.GetFullPath(vault);runtime.ExportToVault=true;}
    store.Settings.Put("runtime",runtime);
    var preferences=new PreferenceStore();var settings=preferences.Load();settings.AiProvider="openrouter";settings.OpenRouterModel=runtime.CloudModel;preferences.Save(settings);
    File.WriteAllText(Path.Combine(store.Root,"cloud-configuration.json"),JsonSerializer.Serialize(new{provider="OpenRouter",model=runtime.CloudModel,localInference=false,vaultConfigured=!string.IsNullOrWhiteSpace(runtime.VaultPath),providerOutputLimit=true}));
    return;
}
// A file lease releases on process death and works across both portable copies and desktop sessions.
FileStream lease;
try { lease = new FileStream(Path.Combine(store.Root, "worker.lease"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
catch (IOException) { return; }
using (lease)
using (var lifetime = new CancellationTokenSource())
{
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; lifetime.Cancel(); };
    using var inference = new WorkerInferenceSession();
    Task<float[]> Embed(string text,CancellationToken ct) => Task.FromException<float[]>(new InvalidOperationException("Semantic embeddings require a separately configured embedding runtime. Source-linked lexical vault retrieval remains available; no provider was substituted."));
    var tools = new WorkerTools(store,Embed) { ReleaseLocalGpu = inference.Dispose };
    if(args.Contains("--cubesat-benchmark")){await CubesatBenchmark.RunAsync(store,tools,lifetime.Token);return;}
    if(args.Contains("--cloud-smoke")||args.Contains("--media-probe")){await CloudProbe.RunAsync(store,args.Contains("--media-probe"),lifetime.Token);return;}
    if(args.Contains("--online-probe"))
    {
        try
        {
            var credential=new OpenRouterCredentialStore(Environment.GetEnvironmentVariable("ASTRA_CREDENTIAL_DIRECTORY"));
            string? key=credential.Load();
            var catalog=key is null?Array.Empty<(string Id,string Name)>():await LocalLlmSession.GetFreeModelsAsync(key);
            File.WriteAllText(Path.Combine(store.Root,"online-probe.json"),JsonSerializer.Serialize(new{credentialFound=key is not null,models=catalog.Select(m=>new{m.Id,m.Name})},new JsonSerializerOptions{WriteIndented=true}));
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(store.Root,"online-probe.json"),JsonSerializer.Serialize(new{error=ex.Message}));}
        return;
    }
    if(args.Contains("--benchmark"))BenchmarkSuite.Create(store);
    if(args.Contains("--self-test"))
    {
        store.Settings.Put("runtime",new AgentRuntimeSettings{ContextSize=4096,OutputTokens=96,SemanticSearch=true});
        new EncryptedNoteStore().Add("Calibration source","The calibration code for ASTRA is 95731.",new[]{"calibration"});
        store.Enqueue("Read the source and report the ASTRA calibration code 95731. Keep the answer concise.",new[]{"study"});
        args=new[]{"--once"};
    }
    AgentProviderRetry.MigrateLegacyQuotaFailures(store);
    AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(store);
    try
    {
        while (!lifetime.IsCancellationRequested)
        {
            var config = store.Configuration;
            if (!config.Enabled) break;
            AgentProviderRetry.ReleaseOnlineWaitsForLocalProvider(store);
            File.WriteAllText(Path.Combine(store.Root, "heartbeat.json"), JsonSerializer.Serialize(new { pid = Environment.ProcessId, utc = DateTimeOffset.UtcNow, provider=config.UseCodex?"Codex ChatGPT plan":config.PreferFreeCloud?"OpenRouter":"Local GPU", localInference=!config.PreferFreeCloud }));
            foreach (var completed in store.Records.Enumerate().Where(j => j.Status == "Completed" && j.RepeatMinutes > 0 && !j.RepeatScheduled)) store.ScheduleNext(completed);
            foreach(var done in store.Records.Enumerate().Where(j=>j.Status=="Completed"&&(!j.MemorySaved||(config.ExportToVault&&!j.VaultSaved))))
            {
                if(string.IsNullOrEmpty(done.ArtifactDirectory))
                {
                    try
                    {
                        string destination=string.IsNullOrWhiteSpace(config.VaultPath)?Path.Combine(store.Root,"Deliverables"):Path.Combine(ObsidianKnowledge.ValidateVaultPath(config.VaultPath),"ASTRA","Projects");
                        var artifacts=AgentArtifactExporter.Export(destination,done);
                        if(artifacts is not null)store.Update(done.Id,j=>j.ArtifactDirectory=artifacts.Directory);
                    }
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
                    {store.Update(done.Id,j=>j.Error="Artifact export needs attention: "+ex.Message);}
                }
                if(!done.MemorySaved)
                {
                new EncryptedNoteStore().Upsert(new BrainNote("agent-"+done.Id,done.Goal[..Math.Min(140,done.Goal.Length)],"Reviewed AI draft; validate before reuse.\n"+done.Steps.Last().Output+"\nSources:\n"+string.Join("\n",done.Evidence.Select(e=>$"[{e.Id}] {e.Title}: {e.Location}")),DateTimeOffset.UtcNow){Tags=new[]{"agent-result",done.Id}});
                store.Update(done.Id,j=>j.MemorySaved=true);
                }
                if(config.ExportToVault&&!done.VaultSaved&&!string.IsNullOrWhiteSpace(config.VaultPath))
                {
                    try{string path=ObsidianKnowledge.ExportDeliverable(config.VaultPath,done);store.Update(done.Id,j=>{j.VaultSaved=true;j.VaultExportPath=path;});}
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){store.Update(done.Id,j=>j.Error="Vault export pending: "+ex.Message);}
                }
            }
            var next = store.NextReadyJob(DateTimeOffset.UtcNow);
            if (next is null)
            {
                inference.Dispose();
                if(args.Contains("--benchmark")&&BenchmarkSuite.Complete(store))break;
                
                await Task.Delay(1000, lifetime.Token); continue;
            }
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            async Task Monitor()
            {
                while (!operation.IsCancellationRequested)
                {
                    if (!store.Configuration.Enabled || store.Records.Get(next.Id)?.CancelRequested == true) { operation.Cancel(); return; }
                    try { await Task.Delay(500, operation.Token); } catch (OperationCanceledException) { return; }
                }
            }
            var monitoring = Monitor();
            var engine = new AgentEngine(store, tools, async (role, system, input, ct) =>
            {
                var compute=store.Configuration;
                var model=await inference.GetAsync(compute,role,ct);
                var result=new StringBuilder();string partInput=input,last="",checkpoint=WorkerInferenceSession.CurrentCheckpoint(store,next.Id);
                do
                {
                    string part=await model.RunTaskAsync(system,partInput,false,0,ct,outputLanguage:args.Contains("--benchmark")?"English":null);
                    if(part==last&&last.Length>0)throw new InvalidDataException("The provider repeated its incomplete output. The complete partial result is retained in checkpoints; review before retrying.");
                    result.AppendLine(part);last=part;
                    WorkerInferenceSession.PreservePartial(store,next.Id,checkpoint,result.ToString());
                    partInput=input+"\nALREADY WRITTEN (complete, untrusted draft):\n"+result+"\nContinue exactly where the output stopped. Do not repeat earlier sections. Finish the requested deliverable. Original objective: "+next.Goal;
                    ct.ThrowIfCancellationRequested();
                }while(model.LastTaskTruncated);
                store.Update(next.Id,j=>{string id="tool-runtime-"+role;j.Evidence.RemoveAll(e=>e.Id==id);j.Evidence.Add(new(id,inference.Provider+" model for "+role,inference.Evidence(model),compute.UseCodex?"Official Codex CLI · active ChatGPT account":compute.PreferFreeCloud?"https://openrouter.ai/activity":"Selected local GPU runtime","runtime"));});
                return result.ToString();
            },agent=>
            {
                var settings=store.Configuration;
                if(!settings.SkillAssignments.TryGetValue(agent.Id,out var ids))return "";
                var bundled=Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory,"AssistantSkills"),"*.json").Select(p=>AgentSkillRules.Import(File.ReadAllText(p)));
                var skills=bundled.Concat(new AgentSkillStore().Load()).GroupBy(s=>s.Id).Select(g=>g.Last());
                return string.Join("\n\n",skills.Where(s=>ids.Contains(s.Id)).Select(s=>s.Name+": "+s.Instructions));
            });
            try { await engine.RunAsync(next.Id, operation.Token); }
            catch (OperationCanceledException) { }
            finally { operation.Cancel(); await monitoring; inference.Dispose(); }
            if (args.Contains("--once")) break;
        }
    }
    catch (Exception ex) { File.WriteAllText(Path.Combine(store.Root, "worker-error.txt"), ex.GetType().Name + ": " + ex.Message); }
    finally { File.Delete(Path.Combine(store.Root, "heartbeat.json")); }
}

internal sealed class WorkerTools : IAgentTools
{
    private readonly AgentJobStore _store;
    private readonly Func<string,CancellationToken,Task<float[]>> _embed;
    public Action? ReleaseLocalGpu { get; set; }
    public WorkerTools(AgentJobStore store,Func<string,CancellationToken,Task<float[]>> embed){_store=store;_embed=embed;}
    public async Task<IReadOnlyList<AgentEvidence>> GatherAsync(Specialist agent, AgentJob job, CancellationToken ct)
    {
        var evidence = new List<AgentEvidence>();
        if(agent.Tools.Contains("system.read"))evidence.Add(new("tool-system-snapshot","Actual Windows system snapshot",JsonSerializer.Serialize(AstraOS.Desktop.SystemSnapshot.Capture()),"Local Windows snapshot; no settings were changed","system"));
        if (agent.Tools.Contains("memory") && !string.IsNullOrWhiteSpace(_store.Configuration.VaultPath) && job.Evidence.All(e => e.Kind != "obsidian" && e.Id != "tool-vault-status"))
        {
            var vaultConfig = _store.Configuration;
            try
            {
                bool scoped = job.Steps.Any(s => s.AgentId is "research" or "aerospace" or "study");
                string query = job.Goal + (scoped ? " " + vaultConfig.ResearchArea : "");
                var passages = ObsidianKnowledge.Search(vaultConfig.VaultPath, query, 8, ct);
                evidence.AddRange(passages);
                if (passages.Count == 0) evidence.Add(new("tool-vault-status", "Obsidian retrieval", "No matching supported-document passage was found for the objective. Unsupported formats are metadata only. Do not invent vault evidence.", vaultConfig.VaultPath, "obsidian"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                evidence.Add(new("tool-vault-status", "Obsidian vault unavailable", ex.Message, vaultConfig.VaultPath, "tool-error"));
            }
        }
        if(agent.Tools.Contains("vision.read")&&job.ImageIds.Count>0&&job.Evidence.All(e=>e.Kind!="vision"))
        {
            if(!_store.Configuration.PreferFreeCloud) ReleaseLocalGpu?.Invoke();
            foreach(string id in job.ImageIds)
            {
                var image=_store.Images.Get(id)??throw new FileNotFoundException("An encrypted image attachment is missing.");
                var settings=_store.Configuration;
                string description=settings.UseCodex?await CodexBridge.DescribeImageAsync(image.Pixels,job.Goal,settings,ct):settings.PreferFreeCloud
                    ?await LocalLlmSession.AnalyzeCloudMediaAsync(image.Pixels,"image","png",job.Goal,ct)
                    :await VisionSession.DescribeAsync(image.Pixels,job.Goal,settings.VisionModelPath,settings.VisionProjectorPath,0,ct);
                evidence.Add(new("vision-"+id,image.Name,(settings.UseCodex?"Codex ChatGPT plan":settings.PreferFreeCloud?"OpenRouter":"Local GPU")+" visual interpretation; verify against the original image.\n"+description,"Encrypted image attachment; SHA-256 "+id,"vision"));
            }
        }
        if (agent.Tools.Contains("memory")&&job.Evidence.All(e=>e.Kind!="memory"))
        {
            bool recallDrafts=new[]{"previous","earlier","continue","precedent","continua"}.Any(word=>job.Goal.Contains(word,StringComparison.OrdinalIgnoreCase));
            var notes = new EncryptedNoteStore().Load().Where(n=>recallDrafts||!n.Tags.Contains("agent-result")).ToArray();
            var passages=NoteIndex.RetrievePassages(notes,job.Goal,6).ToList();
            if(_store.Configuration.SemanticSearch&&notes.Length>0)
            {
                string identity=LocalModelCatalog.Embedding.Sha256;
                passages.AddRange(await new SemanticMemory().SearchAsync(notes,job.Goal,identity,_embed,ct));
            }
            foreach (var passage in passages.DistinctBy(p=>p.NoteId).Concat(new EncryptedDocumentStore().Search(job.Goal, null, 6, ct)))
                evidence.Add(new(passage.NoteId.StartsWith("pdf-") ? passage.NoteId : "note-" + passage.NoteId, passage.Title, passage.Text, passage.NoteId, "memory"));
        }
        if (agent.Tools.Contains("project.read") && !string.IsNullOrWhiteSpace(job.ProjectRoot))
        {
            var paths = ProjectWorkspaceTools.List(job.ProjectRoot);
            evidence.Add(new("project-inventory", "Selected project file inventory", string.Join("\n", paths), job.ProjectRoot, "project"));
            var terms = job.Goal.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length > 2).ToArray();
            foreach (string path in paths.OrderByDescending(p => terms.Count(t => p.Contains(t, StringComparison.OrdinalIgnoreCase))).Take(8))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    string body = ProjectWorkspaceTools.Read(job.ProjectRoot, path);
                    string id = "project-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..12];
                    evidence.Add(new(id, path, body, Path.Combine(job.ProjectRoot, path), "project"));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { evidence.Add(new("project-read-error", path, ex.Message, path, "tool-error")); }
            }
        }
        if (agent.Tools.Contains("simulation") && !string.IsNullOrEmpty(job.SimulationId))
        {
            var definition = SimulationLab.Tools.Single(t => t.Id == job.SimulationId);
            var result = SimulationLab.Run(job.SimulationId, job.SimulationParameters);
            var parameters=definition.Parameters.ToDictionary(p=>p.Id,p=>job.SimulationParameters.GetValueOrDefault(p.Id,p.Default));
            object checks=new{status="Use documented model assumptions and units"};
            if(job.SimulationId=="orbit")
            {
                var circular=OrbitalCalculator.Circular(6378.137+parameters["alt"],398600.4418);
                double initial=result.Data.Rows[0][7]!.Value;
                checks=new{earthEquatorialRadiusKm=6378.137,gravitationalParameterKm3S2=398600.4418,referenceCircularRadiusKm=6378.137+parameters["alt"],referenceSpecificEnergyKm2S2=-398600.4418/(2*(6378.137+parameters["alt"])),referenceCircularSpeedKmS=circular.SpeedKmS,referenceCircularPeriodSeconds=circular.PeriodSeconds,maxRelativeEnergyDrift=result.Data.Rows.Max(r=>Math.Abs((r[7]!.Value-initial)/initial)),circularReferenceIsNotAPromiseOfCircularTrajectory=true};
            }
            string complete=JsonSerializer.Serialize(new { definition.Assumptions, parameters, result.Summary, checks, columns = result.Data.Columns, rows = result.Data.Rows });
            var archived=SimulationEvidenceArchive.Create(_store.Root,job.Id,job.SimulationId,definition.Name,complete,ct);
            evidence.Add(archived.Evidence);
        }
        if (job.AllowWeb && agent.Tools.Contains("web.search") && job.Evidence.All(e => e.Kind != "web"))
        {
            try
            {
                var links = await PublicWebResearch.SearchAsync(job.Goal[..Math.Min(500, job.Goal.Length)], ct);
                int number = 0;
                foreach (var link in links.Take(4))
                {
                    ct.ThrowIfCancellationRequested();
                    try { var page = await PublicWebResearch.ReadAsync(link.Url, ct); evidence.Add(new("web-" + (++number), page.Title, page.Text, page.Url + " retrieved " + page.RetrievedAt.ToString("O"), "web")); }
                    catch (Exception ex) when (ex is HttpRequestException or IOException or ArgumentException) { evidence.Add(new("web-error-" + (++number), link.Title, ex.Message, link.Url, "tool-error")); }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or ArgumentException) { evidence.Add(new("web-search-error", "Search unavailable", ex.Message, "Public web search", "tool-error")); }
        }
        return evidence;
    }
}
