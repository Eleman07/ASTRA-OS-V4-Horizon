using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using AstraOS.Core;
using AstraOS.Core.Agents;
using AstraOS.Core.SecondBrain;
using AstraOS.Desktop.Ai;

var store = new AgentJobStore();
Directory.CreateDirectory(store.Root);
if(args.Contains("--configure-cloud"))
{
    var runtime=store.Configuration;runtime.PreferFreeCloud=true;runtime.FallbackToGpu=false;runtime.SemanticSearch=false;runtime.UseGpu=false;runtime.CloudModel="nvidia/nemotron-3-ultra-550b-a55b:free";runtime.ContextSize=1_000_000;runtime.OutputTokens=0;
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
    LocalLlmSession? cloudModel=null;
    string cloudKey="";
    Task<float[]> Embed(string text,CancellationToken ct) => Task.FromException<float[]>(new InvalidOperationException("Local embedding inference is disabled in cloud-only mode; use source-linked lexical vault retrieval."));
    var tools = new WorkerTools(store,Embed);
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
    try
    {
        while (!lifetime.IsCancellationRequested)
        {
            var config = store.Configuration;
            if (!config.Enabled) break;
            File.WriteAllText(Path.Combine(store.Root, "heartbeat.json"), JsonSerializer.Serialize(new { pid = Environment.ProcessId, utc = DateTimeOffset.UtcNow, modelLoaded = false, provider="OpenRouter", localInference=false }));
            foreach (var completed in store.Records.Enumerate().Where(j => j.Status == "Completed" && j.RepeatMinutes > 0 && !j.RepeatScheduled)) store.ScheduleNext(completed);
            foreach(var done in store.Records.Enumerate().Where(j=>j.Status=="Completed"&&(!j.MemorySaved||(config.ExportToVault&&!j.VaultSaved))))
            {
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
            var next = store.Records.Enumerate().Where(j => j.Status is "Queued" or "Running" && !j.CancelRequested && j.DueUtc <= DateTimeOffset.UtcNow).OrderBy(j => j.DueUtc).FirstOrDefault();
            if (next is null)
            {
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
                string key=compute.CloudModelAssignments.TryGetValue(role,out string? assigned)&&!string.IsNullOrWhiteSpace(assigned)?assigned:compute.CloudModel;
                if(cloudModel is null||cloudKey!=key){cloudModel?.Dispose();cloudModel=new();cloudModel.StartOpenRouter(new OpenRouterCredentialStore(Environment.GetEnvironmentVariable("ASTRA_CREDENTIAL_DIRECTORY")).Load()??throw new InvalidOperationException("Save a free OpenRouter key in ASTRA AI settings."),key);cloudKey=key;}
                var result=new StringBuilder();string partInput=input,last="";
                do
                {
                    string part=await cloudModel.RunTaskAsync(system,partInput,false,0,ct,outputLanguage:args.Contains("--benchmark")?"English":null);
                    if(part==last&&last.Length>0)throw new InvalidDataException("The provider repeated its incomplete output. The complete partial result is retained in checkpoints; review before retrying.");
                    result.AppendLine(part);last=part;
                    partInput=input+"\nALREADY WRITTEN (complete, untrusted draft):\n"+result+"\nContinue exactly where the output stopped. Do not repeat earlier sections. Finish the requested deliverable. Original objective: "+next.Goal;
                    ct.ThrowIfCancellationRequested();
                }while(cloudModel.LastTaskTruncated);
                store.Update(next.Id,j=>{string id="tool-runtime-"+role;j.Evidence.RemoveAll(e=>e.Id==id);j.Evidence.Add(new(id,"Online model for "+role,"OpenRouter model: "+cloudModel.EffectiveModel+". "+cloudModel.LastModelNotice+" Input and output price ceiling: USD 0. Provider context/output/rate limits apply; local inference is disabled.","https://openrouter.ai/activity","runtime"));});
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
            finally { operation.Cancel(); await monitoring; }
            if (args.Contains("--once")) break;
        }
    }
    catch (Exception ex) { File.WriteAllText(Path.Combine(store.Root, "worker-error.txt"), ex.GetType().Name + ": " + ex.Message); }
    finally { cloudModel?.Dispose(); File.Delete(Path.Combine(store.Root, "heartbeat.json")); }
}

internal sealed class WorkerTools : IAgentTools
{
    private readonly AgentJobStore _store;
    private readonly Func<string,CancellationToken,Task<float[]>> _embed;
    public WorkerTools(AgentJobStore store,Func<string,CancellationToken,Task<float[]>> embed){_store=store;_embed=embed;}
    public async Task<IReadOnlyList<AgentEvidence>> GatherAsync(Specialist agent, AgentJob job, CancellationToken ct)
    {
        var evidence = new List<AgentEvidence>();
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
            foreach(string id in job.ImageIds)
            {
                var image=_store.Images.Get(id)??throw new FileNotFoundException("An encrypted image attachment is missing.");
                string description=await LocalLlmSession.AnalyzeCloudMediaAsync(image.Pixels,"image","png",job.Goal,ct);
                evidence.Add(new("vision-"+id,image.Name,"OpenRouter visual interpretation; verify against the original image.\n"+description,"Encrypted image attachment; SHA-256 "+id,"vision"));
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
            evidence.Add(new("tool-simulation", definition.Name, JsonSerializer.Serialize(new { definition.Assumptions, parameters, result.Summary, checks, columns = result.Data.Columns, rows = result.Data.Rows }), "SimulationLab:" + job.SimulationId, "simulation"));
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
